using System;
using System.Collections.Generic;
using System.Threading;
using KramaxReloadExtensions;
using kOS.Utilities;
using kOSMainframe.Utils;
using kOSMainframe.Orbital;
using kOSMainframe.VesselExtra;
using kOSMainframe.Simulation;
using kOSMainframe.UnityToolbag;
using kOSMainframe.Numerics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace kOSMainframe.Landing {
    public class LandingSimulation : ReloadableMonoBehaviour {
        private Vessel vessel;
        private CelestialBody targetBody;
        private double targetLatitude;
        private double targetLongitude;
        public bool makeAerobrakeNodes = false;
        public ManeuverNode aerobrakeNode = null;
        public Result result;
        public Result errorResult;
        private readonly Queue<Result> readyResults = new Queue<Result>();
        private bool simulationRunning = false;
        protected bool errorSimulationRunning = false;
        public double parachuteSemiDeployMultiplier = 3;
        public int limitChutesStage = -1;
        private Random random = new Random();
        public IDescentSpeedPolicy descentSpeedPolicy = null;
        public double maxOrbits = 1;
        public bool noSkipToFreefall = false;
        protected double dt = 0.2;
        protected bool variabledt = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        protected System.Diagnostics.Stopwatch errorStopwatch = new System.Diagnostics.Stopwatch();
        private double lastSimTime;
        private double lastSimSteps;
        private double lastErrorSimTime;
        private double lastErrorSimSteps;
        protected long millisecondsBetweenSimulations;
        protected long millisecondsBetweenErrorSimulations;
        public bool runErrorSimulations = false;
        protected const int interationsPerSecond = 5;

        public static LandingSimulation Current {
            get {
                return kOSMainFramePlugin.Instance.GetComponent<LandingSimulation>();
            }
        }

        public CelestialBody Body {
            get {
                return targetBody;
            }
        }

        public double Latitude {
            get {
                return targetLatitude;
            }
        }

        public double Longitude {
            get {
                return targetLongitude;
            }
        }

        public double LandingAltitude {
            // The altitude above sea level of the terrain at the landing site
            get {
                if (PredictionReady) {
                    // Although we know the landingASL as it is in the prediction, we suspect that
                    // it might sometimes be incorrect. So to check we will calculate it again here,
                    // and if the two differ log an error. It seems that this terrain ASL calls when
                    // made from the simulatiuon thread are regularly incorrect, but are OK when made
                    // from this thread. At the time of writting (KSP0.23) there seem to be several
                    // other things going wrong with he terrain system, such as visual glitches as
                    // we as the occasional exceptions being thrown when calls to the CelestialBody
                    // object are made. I suspect a bug or some sort - for now this hack improves
                    // the landing results.
                    {
                        double checkASL = result.body.TerrainAltitude(result.endPosition.latitude, result.endPosition.longitude);
                        if (checkASL != result.endASL) {
                            // I know that this check is not required as we might as well always make
                            // the asignment. However this allows for some debug monitoring of how often this is occuring.
                            result.endASL = checkASL;
                        }
                    }

                    return result.endASL;
                } else {
                    return targetBody.TerrainAltitude(targetLatitude, targetLongitude);
                }
            }
        }

        public bool PredictionReady {
            //We shouldn't do any autopilot stuff until this is true
            get {
                // Check that there is a prediction and that it is a landing prediction.
                if (result == null) {
                    return false;
                } else if (result.outcome != Outcome.LANDED) {
                    return false;
                } else {
                    return true;
                }
            }
        }

        public static void Start(Vessel vessel, double latitude, double longitude) {
            LandingSimulation simulation = Current ?? (kOSMainFramePlugin.Instance.AddComponent(typeof(LandingSimulation)) as LandingSimulation);

            simulation.SetTarget(vessel, latitude, longitude);
        }

        public static void Stop() {
            LandingSimulation simulation = Current;

            if(simulation != null) {
                Object.Destroy(simulation);
            }
        }

        public void SetTarget(Vessel vessel, double latitude, double longitude) {
            this.vessel = vessel;
            targetBody = vessel.mainBody;
            targetLatitude = latitude;
            targetLongitude = longitude;
            UpdateDescentSpeedPolicy();
        }

        // Estimate the delta-V of the correction burn that would be required to put us on
        // course for the target
        public Vector3d ComputeCourseCorrection(double UT, bool allowPrograde) {
            if (!PredictionReady) return Vector3d.zero;

            // actualLandingPosition is the predicted actual landing position
            Vector3d actualLandingPosition = result.RelativeEndPosition();

            // orbitLandingPosition is the point where our current orbit intersects the planet
            double endRadius = targetBody.Radius + DecelerationEndAltitude() - 100;
            Orbit orbit = vessel.orbit;

            // Seems we are already landed ?
            if (endRadius > orbit.ApR || vessel.LandedOrSplashed)
                return Vector3d.zero;

            Vector3d orbitLandingPosition;
            if (orbit.PeR < endRadius)
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextTimeOfRadius(UT, endRadius));
            else
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextPeriapsisTime(UT));

            // convertOrbitToActual is a rotation that rotates orbitLandingPosition on actualLandingPosition
            Quaternion convertOrbitToActual = Quaternion.FromToRotation(orbitLandingPosition, actualLandingPosition);

            // Consider the effect small changes in the velocity in each of these three directions
            Vector3d[] perturbationDirections = { vessel.GetSurfaceVelocity().normalized, vessel.GetRadialPlusSurface(), vessel.GetNormalPlusSurface() };

            // Compute the effect burns in these directions would
            // have on the landing position, where we approximate the landing position as the place
            // the perturbed orbit would intersect the planet.
            Vector3d[] deltas = new Vector3d[3];
            for (int i = 0; i < 3; i++) {
                const double perturbationDeltaV = 1; //warning: hard experience shows that setting this too low leads to bewildering bugs due to finite precision of Orbit functions
                Orbit perturbedOrbit = orbit.PerturbedOrbit(UT, perturbationDeltaV * perturbationDirections[i]); //compute the perturbed orbit
                double perturbedLandingTime;
                if (perturbedOrbit.PeR < endRadius) perturbedLandingTime = perturbedOrbit.NextTimeOfRadius(UT, endRadius);
                else perturbedLandingTime = perturbedOrbit.NextPeriapsisTime(UT);
                Vector3d perturbedLandingPosition = perturbedOrbit.SwappedRelativePositionAtUT(perturbedLandingTime); //find where it hits the planet
                Vector3d landingDelta = perturbedLandingPosition - orbitLandingPosition; //find the difference between that and the original orbit's intersection point
                landingDelta = convertOrbitToActual * landingDelta; //rotate that difference vector so that we can now think of it as starting at the actual landing position
                landingDelta = Vector3d.Exclude(actualLandingPosition, landingDelta); //project the difference vector onto the plane tangent to the actual landing position
                deltas[i] = landingDelta / perturbationDeltaV; //normalize by the delta-V considered, so that deltas now has units of meters per (meter/second) [i.e., seconds]
            }

            // Now deltas stores the predicted offsets in landing position produced by each of the three perturbations.
            // We now figure out the offset we actually want

            // First we compute the target landing position. We have to convert the latitude and longitude of the target
            // into a position. We can't just get the current position of those coordinates, because the planet will
            // rotate during the descent, so we have to account for that.
            Vector3d desiredLandingPosition = targetBody.GetWorldSurfacePosition(targetLatitude, targetLongitude, 0) - targetBody.position;
            float bodyRotationAngleDuringDescent = (float)(360 * (result.endUT - UT) / targetBody.rotationPeriod);
            Quaternion bodyRotationDuringFall = Quaternion.AngleAxis(bodyRotationAngleDuringDescent, targetBody.angularVelocity.normalized);
            desiredLandingPosition = bodyRotationDuringFall * desiredLandingPosition;

            Vector3d desiredDelta = desiredLandingPosition - actualLandingPosition;
            desiredDelta = Vector3d.Exclude(actualLandingPosition, desiredDelta);

            // Now desiredDelta gives the desired change in our actual landing position (projected onto a plane
            // tangent to the actual landing position).

            Vector3d downrangeDirection;
            Vector3d downrangeDelta;
            if (allowPrograde) {
                // Construct the linear combination of the prograde and radial+ perturbations
                // that produces the largest effect on the landing position. The Math.Sign is to
                // detect and handle the case where radial+ burns actually bring the landing sign closer
                // (e.g. when we are traveling close to straight up)
                downrangeDirection = (deltas[0].magnitude * perturbationDirections[0]
                                      + Math.Sign(Vector3d.Dot(deltas[0], deltas[1])) * deltas[1].magnitude * perturbationDirections[1]).normalized;

                downrangeDelta = Vector3d.Dot(downrangeDirection, perturbationDirections[0]) * deltas[0]
                                 + Vector3d.Dot(downrangeDirection, perturbationDirections[1]) * deltas[1];
            } else {
                // If we aren't allowed to burn prograde, downrange component of the landing
                // position has to be controlled by radial+/- burns:
                downrangeDirection = perturbationDirections[1];
                downrangeDelta = deltas[1];
            }

            // Now solve a 2x2 system of linear equations to determine the linear combination
            // of perturbationDirection01 and normal+ that will give the desired offset in the
            // predicted landing position.
            Matrix2x2 A = new Matrix2x2(
                downrangeDelta.sqrMagnitude, Vector3d.Dot(downrangeDelta, deltas[2]),
                Vector3d.Dot(downrangeDelta, deltas[2]), deltas[2].sqrMagnitude
            );

            Vector2d b = new Vector2d(Vector3d.Dot(desiredDelta, downrangeDelta), Vector3d.Dot(desiredDelta, deltas[2]));

            Vector2d coeffs = A.inverse() * b;

            Vector3d courseCorrection = coeffs.x * downrangeDirection + coeffs.y * perturbationDirections[2];

            return courseCorrection;
        }


        public void OnGUI() {
            if (targetBody == null) {
                return;
            }
            GLUtils.DrawGroundMarker(targetBody, targetLatitude, targetLongitude, Color.blue, MapView.MapIsEnabled, 60);

            Result drawResult = result;

            if(drawResult != null && drawResult.outcome == Outcome.LANDED) {
                GLUtils.DrawGroundMarker(targetBody, drawResult.endPosition.latitude, drawResult.endPosition.longitude, Color.red, MapView.MapIsEnabled);
            }
            if(MapView.MapIsEnabled && drawResult != null && drawResult.outcome != Outcome.ERROR) {
                double interval = Math.Max(Math.Min((drawResult.endUT - drawResult.input_UT) / 1000, 10), 0.1);
                using (var list = drawResult.WorldTrajectory(interval)) {
                    if (!MapView.MapIsEnabled && (noSkipToFreefall || vessel.staticPressurekPa > 0))
                        list.value[0] = vessel.CoM;
                    GLUtils.DrawPath(drawResult.body, list.value, Color.red, MapView.MapIsEnabled);
                }
            }
        }

        public void FixedUpdate() {
            if (targetBody == null) {
                return;
            }

            CheckForResult();

            TryStartSimulation(true);
        }

        private void TryStartSimulation(bool doErrorSim) {
            try {
                if (vessel.isActiveVessel && !vessel.LandedOrSplashed) {
                    // We should be running simulations periodically. If one is not running right now,
                    // check if enough time has passed since the last one to start a new one:
                    if (!simulationRunning && (stopwatch.ElapsedMilliseconds > millisecondsBetweenSimulations || !stopwatch.IsRunning)) {
                        // variabledt generate too much instability of the landing site with atmo.
                        // variabledt = !(mainBody.atmosphere && core.landing.enabled);
                        // the altitude may induce some instability but allow for greater precision of the display in manual flight

                        //variabledt = !mainBody.atmosphere || vessel.terrainAltitude < 1000 ;
                        //if (!variabledt)
                        //    dt = 0.5;

                        StartSimulation(false);
                    }

                    // We also periodically run simulations containing deliberate errors if we have been asked to do so by the landing autop
                    if (doErrorSim && this.runErrorSimulations && !errorSimulationRunning && (errorStopwatch.ElapsedMilliseconds >= millisecondsBetweenErrorSimulations || !errorStopwatch.IsRunning)) {
                        StartSimulation(true);
                    }
                }
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        protected void StartSimulation(bool addParachuteError) {
            double altitudeOfPreviousPrediction = 0;
            double parachuteMultiplierForThisSimulation = this.parachuteSemiDeployMultiplier;
            if (addParachuteError) {
                errorSimulationRunning = true;
            } else {
                simulationRunning = true;
            }
            Orbit patch = GetReenteringPatch() ?? vessel.orbit;
            // Work out what the landing altitude was of the last prediction, and use that to pass into the next simulation
            if (result != null) {
                if (result.outcome == Outcome.LANDED && result.body != null) {
                    altitudeOfPreviousPrediction = result.endASL; // Note that we are caling GetResult here to force the it to calculate the endASL, if it has not already done this. It is not allowed to do this previously as we are only allowed to do it from this thread, not the reentry simulation thread.
                }
            }
            // Is this a simulation run with errors added? If so then add some error to the parachute multiple
            if (addParachuteError) {
                parachuteMultiplierForThisSimulation *= 1d + (random.Next(1000000) - 500000d) / 10000000d;
            }

            // The curves used for the sim are not thread safe so we need a copy used only by the thread
            SimCurves simCurves = SimCurves.Borrow(patch.referenceBody);

            SimulatedVessel simVessel = SimulatedVessel.Borrow(vessel, simCurves, patch.StartUT, limitChutesStage);
            ReentrySimulation sim = ReentrySimulation.Borrow(patch, patch.StartUT, simVessel, simCurves, descentSpeedPolicy, DecelerationEndAltitude(), VesselUtils.GetAvailableThrust(vessel) / vessel.totalMass, parachuteMultiplierForThisSimulation, altitudeOfPreviousPrediction, addParachuteError, dt, Time.fixedDeltaTime, maxOrbits, noSkipToFreefall);
            //MechJebCore.print("Sim ran with dt=" + dt.ToString("F3"));

            //Run the simulation in a separate thread
            ThreadPool.QueueUserWorkItem(RunSimulation, sim);
            //RunSimulation(sim);
        }

        private void RunSimulation(object o) {
            ReentrySimulation sim = (ReentrySimulation)o;
            try {
                Result newResult = sim.RunSimulation();

                lock (readyResults) {
                    readyResults.Enqueue(newResult);
                }

                if (newResult.multiplierHasError) {
                    //see how long the simulation took
                    errorStopwatch.Stop();
                    long millisecondsToCompletion = errorStopwatch.ElapsedMilliseconds;
                    lastErrorSimTime = millisecondsToCompletion * 0.001;
                    lastErrorSimSteps = newResult.steps;

                    errorStopwatch.Reset();

                    //set the delay before the next simulation
                    millisecondsBetweenErrorSimulations = Math.Min(Math.Max(4 * millisecondsToCompletion, 400), 5);
                    // Note that we are going to run the simulations with error in less often that the real simulations

                    //start the stopwatch that will count off this delay
                    errorStopwatch.Start();
                    errorSimulationRunning = false;
                } else {
                    //see how long the simulation took
                    stopwatch.Stop();
                    long millisecondsToCompletion = stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();

                    //set the delay before the next simulation
                    millisecondsBetweenSimulations = Math.Min(Math.Max(2 * millisecondsToCompletion, 200), 5);
                    lastSimTime = millisecondsToCompletion * 0.001;
                    lastSimSteps = newResult.steps;
                    // Do not wait for too long before running another simulation, but also give the processor a rest.

                    // How long should we set the max_dt to be in the future? Calculate for interationsPerSecond runs per second. If we do not enter the atmosphere, however do not do so as we will complete so quickly, it is not a good guide to how long the reentry simulation takes.
                    if (newResult.outcome == Outcome.AEROBRAKED ||
                            newResult.outcome == Outcome.LANDED) {
                        if (this.variabledt) {
                            dt = newResult.maxdt * (millisecondsToCompletion / 1000d) / (1d / (3d * interationsPerSecond));
                            // There is no point in having a dt that is smaller than the physics frame rate as we would be trying to be more precise than the game.
                            dt = Math.Max(dt, sim.min_dt);
                            // Set a sensible upper limit to dt as well. - in this case 10 seconds
                            dt = Math.Min(dt, 10);
                        }
                    }

                    //Debug.Log("Result:" + this.result.outcome + " Time to run: " + millisecondsToCompletion + " millisecondsBetweenSimulations: " + millisecondsBetweenSimulations + " new dt: " + dt + " Time.fixedDeltaTime " + Time.fixedDeltaTime + "\n" + this.result.ToString()); // Note the endASL will be zero as it has not yet been calculated, and we are not allowed to calculate it from this thread :(

                    //start the stopwatch that will count off this delay
                    stopwatch.Start();
                    simulationRunning = false;
                }
            } catch (Exception ex) {
                //Debug.Log(string.Format("Exception in MechJebModuleLandingPredictions.RunSimulation\n{0}", ex.StackTrace));
                //Debug.LogException(ex);
                Dispatcher.InvokeAsync(() => Debug.LogException(ex));
            } finally {
                sim.Release();
            }
        }

        private void CheckForResult() {
            lock (readyResults) {
                while (readyResults.Count > 0) {
                    Result newResult = readyResults.Dequeue();

                    // If running the simulation resulted in an error then just ignore it.
                    if (newResult.outcome != Outcome.ERROR) {
                        if (newResult.body != null)
                            newResult.endASL = newResult.body.TerrainAltitude(newResult.endPosition.latitude, newResult.endPosition.longitude);

                        if (newResult.multiplierHasError) {
                            if (errorResult != null)
                                errorResult.Release();
                            errorResult = newResult;
                        } else {
                            if (result != null)
                                result.Release();
                            result = newResult;
                        }
                        UpdateDescentSpeedPolicy();
                    } else {
                        if (newResult.exception != null)
                            Logging.Debug("Exception in the last simulation\n" + newResult.exception.Message + "\n" + newResult.exception.StackTrace);
                        newResult.Release();
                    }
                }
            }
        }


        private Orbit GetReenteringPatch() {
            Orbit patch = vessel.orbit;

            int i = 0;

            do {
                i++;
                double reentryRadius = patch.referenceBody.Radius + patch.referenceBody.RealMaxAtmosphereAltitude();
                Orbit nextPatch = vessel.GetNextPatch(patch, aerobrakeNode);
                if (patch.PeR < reentryRadius) {
                    if (patch.Radius(patch.StartUT) < reentryRadius) return patch;

                    double reentryTime = patch.NextTimeOfRadius(patch.StartUT, reentryRadius);
                    if (patch.StartUT < reentryTime && (nextPatch == null || reentryTime < nextPatch.StartUT)) {
                        return patch;
                    }
                }

                patch = nextPatch;
            } while (patch != null);

            return null;
        }

        private void MaintainAerobrakeNode() {
            if (makeAerobrakeNodes) {
                //Remove node after finishing aerobraking:
                if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode)) {
                    if (aerobrakeNode.UT < Planetarium.GetUniversalTime() && vessel.GetAltitudeASL() > targetBody.RealMaxAtmosphereAltitude()) {
                        aerobrakeNode.RemoveSelf();
                        aerobrakeNode = null;
                    }
                }

                //Update or create node if necessary:
                Result r = result;
                if (r != null && r.outcome == Outcome.AEROBRAKED) {
                    //Compute the node dV:
                    Orbit preAerobrakeOrbit = GetReenteringPatch();

                    //Put the node at periapsis, unless we're past periapsis. In that case put the node at the current time.
                    double UT;
                    if (preAerobrakeOrbit == vessel.orbit &&
                            vessel.GetAltitudeASL() < targetBody.RealMaxAtmosphereAltitude() && vessel.GetSpeedVertical() > 0) {
                        UT = Planetarium.GetUniversalTime();
                    } else {
                        UT = preAerobrakeOrbit.NextPeriapsisTime(preAerobrakeOrbit.StartUT);
                    }

                    Orbit postAerobrakeOrbit = Helper.OrbitFromStateVectors(r.WorldAeroBrakePosition(), r.WorldAeroBrakeVelocity(), r.body, r.aeroBrakeUT);

                    Vector3d dV = OrbitChange.ChangeApoapsis(preAerobrakeOrbit, UT, postAerobrakeOrbit.ApR).deltaV;

                    if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode)) {
                        //update the existing node
                        NodeParameters nodeParams = preAerobrakeOrbit.DeltaVToNode(UT, dV);
                        aerobrakeNode.UpdateNode(nodeParams);
                    } else {
                        //place a new node
                        aerobrakeNode = vessel.PlaceManeuverNode(preAerobrakeOrbit, dV, UT);
                    }
                } else {
                    //no aerobraking, remove the node:
                    if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode)) {
                        aerobrakeNode.RemoveSelf();
                    }
                }
            } else {
                //Remove aerobrake node when it is turned off:
                if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode)) {
                    aerobrakeNode.RemoveSelf();
                }
            }
        }

        public double DecelerationEndAltitude() {
            if (UseAtmosphereToBrake()) {
                // if the atmosphere is thick, deceleration (meaning freefall through the atmosphere)
                // should end a safe height above the landing site in order to allow braking from terminal velocity warning Drag Length is quite large now without parachutes, check this better
                double landingSiteDragLength = targetBody.DragLength(LandingAltitude, VesselAverageDrag() + ParachuteAddedDragCoef(), vessel.totalMass);

                //MechJebCore.print("DecelerationEndAltitude Atmo " + (2 * landingSiteDragLength + LandingAltitude).ToString("F2"));
                return 1.1 * landingSiteDragLength + LandingAltitude;
            } else {
                //if the atmosphere is thin, the deceleration burn should end
                //500 meters above the landing site to allow for a controlled final descent
                //MechJebCore.print("DecelerationEndAltitude Vacum " + (500 + LandingAltitude).ToString("F2"));
                return 500 + LandingAltitude;
            }
        }

        //On planets with thick enough atmospheres, we shouldn't do a deceleration burn. Rather,
        //we should let the atmosphere decelerate us and only burn during the final descent to
        //ensure a safe touchdown speed. How do we tell if the atmosphere is thick enough? We check
        //to see if there is an altitude within the atmosphere for which the characteristic distance
        //over which drag slows the ship is smaller than the altitude above the terrain. If so, we can
        //expect to get slowed to near terminal velocity before impacting the ground.
        public bool UseAtmosphereToBrake() {
            double landingSiteDragLength = targetBody.DragLength(LandingAltitude, VesselAverageDrag() + ParachuteAddedDragCoef(), vessel.totalMass);

            //if (mainBody.RealMaxAtmosphereAltitude() > 0 && (ParachutesDeployable() || ParachutesDeployed()))
            if (targetBody.RealMaxAtmosphereAltitude() > 0 && landingSiteDragLength < 0.7 * targetBody.RealMaxAtmosphereAltitude()) { // the ratio is totally arbitrary until I get something better
                return true;
            }
            return false;
        }

        public double ParachuteAddedDragCoef() {
            double addedDragCoef = 0;
            if (targetBody.atmosphere) {
                List<ModuleParachute> parachutes = vessel.GetModules<ModuleParachute>();
                for (int i = 0; i < parachutes.Count; i++) {
                    ModuleParachute p = parachutes[i];
                    if (p.part.inverseStage >= limitChutesStage) {
                        //addedDragMass += p.part.DragCubes.Cubes.Where(c => c.Name == "DEPLOYED").m

                        float maxCoef = 0;
                        for (int c = 0; c < p.part.DragCubes.Cubes.Count; c++) {
                            DragCube dragCube = p.part.DragCubes.Cubes[c];
                            if (dragCube.Name != "DEPLOYED")
                                continue;

                            for (int f = 0; f < 6; f++) {
                                // we only want the additional coef from going fully deployed
                                maxCoef = Mathf.Max(maxCoef, p.part.DragCubes.WeightedDrag[f] - dragCube.Weight * dragCube.Drag[f]);
                            }
                        }
                        addedDragCoef += maxCoef;
                    }
                }
            }
            return addedDragCoef * PhysicsGlobals.DragCubeMultiplier;
        }

        // Get an average drag for the whole vessel. Far from precise but fast.
        public double VesselAverageDrag() {
            float dragCoef = 0;
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part part = vessel.parts[i];
                if (part.DragCubes.None || part.ShieldedFromAirstream) {
                    continue;
                }
                //DragCubeList.CubeData data = part.DragCubes.AddSurfaceDragDirection(Vector3.back, 1);
                //
                //dragCoef += data.areaDrag;

                float partAreaDrag = 0;
                for (int f = 0; f < 6; f++) {
                    partAreaDrag = part.DragCubes.WeightedDrag[f] * part.DragCubes.AreaOccluded[f]; // * PhysicsGlobals.DragCurveValue(0.5, machNumber) but I ll assume it is 1 for now
                }
                dragCoef += partAreaDrag / 6;
            }
            return dragCoef * PhysicsGlobals.DragCubeMultiplier;
        }

        private void UpdateDescentSpeedPolicy() {
            descentSpeedPolicy = new SafeDescentSpeedPolicy(targetBody.Radius + DecelerationEndAltitude(), targetBody.GeeASL * 9.81, VesselUtils.GetAvailableThrust(vessel) / vessel.totalMass);
        }
    }
}
