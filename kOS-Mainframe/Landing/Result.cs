using System;
using System.Collections.Generic;
using kOSMainframe.Orbital;
using kOSMainframe.ExtraMath;
using kOSMainframe.Simulation;
using Smooth.Pools;
using Smooth.Dispose;
using UnityEngine;

namespace kOSMainframe.Landing {
    public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY, ERROR }

    public struct prediction {
        // debuging
        public double firstDrag;
        public double firstLift;
        public double speedOfSound;
        public double mach;
        public double dynamicPressurekPa;
    }

    public class Result {
        public double maxdt;
        public int steps;

        public double timeToComplete;

        public ulong id; // give each set of results a new id so we can check to see if the result has changed.
        public Outcome outcome;
        public Exception exception;

        public CelestialBody body;
        public ReferenceFrame referenceFrame;
        public double endUT;

        public AbsoluteVector startPosition;
        public AbsoluteVector endPosition;
        public AbsoluteVector endVelocity;

        public bool aeroBrake;
        public double aeroBrakeUT;
        public AbsoluteVector aeroBrakePosition;
        public AbsoluteVector aeroBrakeVelocity;

        public double endASL;
        public List<AbsoluteVector> trajectory;

        public double maxDragGees;
        public double deltaVExpended;

        public bool multiplierHasError;
        public double parachuteMultiplier;

        // Provide all the input paramaters to the simulation in the result to aid debugging
        public Orbit input_initialOrbit;
        public double input_UT;
        //public double input_dragMassExcludingUsedParachutes;
        public List<SimulatedParachute> input_parachuteList;
        public double input_mass;
        public IDescentSpeedPolicy input_descentSpeedPolicy;
        public double input_decelEndAltitudeASL;
        public double input_maxThrustAccel;
        public double input_parachuteSemiDeployMultiplier;
        public double input_probableLandingSiteASL;
        public bool input_multiplierHasError;
        public double input_dt;

        public string debugLog;

        private static readonly Pool<Result> pool = new Pool<Result>(Create, Reset);

        public static int PoolSize {
            get {
                return pool.Size;
            }
        }

        private static Result Create() {
            return new Result();
        }

        public void Release() {
            if (trajectory != null)
                ListPool<AbsoluteVector>.Instance.Release(trajectory);
            exception = null;
            pool.Release(this);
        }

        private static void Reset(Result obj) {
            obj.aeroBrake = false;
        }

        public static Result Borrow() {
            Result result = pool.Borrow();
            return result;
        }

        // debuging
        public prediction prediction;

        public Vector3d RelativeEndPosition() {
            return WorldEndPosition() - body.position;
        }

        public Vector3d WorldEndPosition() {
            return referenceFrame.WorldPositionAtCurrentTime(endPosition);
        }

        public Vector3d WorldEndVelocity() {
            return referenceFrame.WorldVelocityAtCurrentTime(endVelocity);
        }

        public Orbit EndOrbit() {
            return Helper.OrbitFromStateVectors(WorldEndPosition(), WorldEndVelocity(), body, endUT);
        }

        public Vector3d WorldAeroBrakePosition() {
            return referenceFrame.WorldPositionAtCurrentTime(aeroBrakePosition);
        }

        public Vector3d WorldAeroBrakeVelocity() {
            return referenceFrame.WorldVelocityAtCurrentTime(aeroBrakeVelocity);
        }

        public Orbit AeroBrakeOrbit() {
            return Helper.OrbitFromStateVectors(WorldAeroBrakePosition(), WorldAeroBrakeVelocity(), body, endUT);
        }

        public Disposable<List<Vector3d>> WorldTrajectory(double timeStep, bool world = true) {
            Disposable<List<Vector3d>> ret = ListPool<Vector3d>.Instance.BorrowDisposable();

            if (trajectory.Count == 0) return ret;

            if (world)
                ret.value.Add(referenceFrame.WorldPositionAtCurrentTime(trajectory[0]));
            else
                ret.value.Add(referenceFrame.BodyPositionAtCurrentTime(trajectory[0]));
            double lastTime = trajectory[0].UT;
            for (int i = 0; i < trajectory.Count; i++) {
                AbsoluteVector absolute = trajectory[i];
                if (absolute.UT > lastTime + timeStep) {
                    if (world)
                        ret.value.Add(referenceFrame.WorldPositionAtCurrentTime(absolute));
                    else
                        ret.value.Add(referenceFrame.BodyPositionAtCurrentTime(absolute));
                    lastTime = absolute.UT;
                }
            }
            return ret;
        }

        // A method to calculate the overshoot (length of the component of the vector from the target to the actual landing position that is parallel to the vector from the start position to the target site.)
        public double GetOvershoot(double targetLatitude, double targetLongitude) {
            // Get the start, end and target positions as a set of 3d vectors that we can work with
            Vector3 end = this.body.GetWorldSurfacePosition(endPosition.latitude, endPosition.longitude, 0) - body.position;
            Vector3 target = this.body.GetWorldSurfacePosition(targetLatitude, targetLongitude, 0) - body.position;
            Vector3 start = this.body.GetWorldSurfacePosition(startPosition.latitude, startPosition.longitude, 0) - body.position;

            // First we need to get two vectors that are non orthogonal to each other and to the vector from the start to the target. TODO can we simplify this code by using Vector3.Exclude?
            Vector3 start2Target = target - start;
            Vector3 orthog1 = Vector3.Cross(start2Target, Vector3.up);
            // check for the spaecial case where start2target is parrallel to up. If it is then the result will be zero,and we need to try again
            if (orthog1 == Vector3.up) {
                orthog1 = Vector3.Cross(start2Target, Vector3.forward);
            }
            Vector3 orthog2 = Vector3.Cross(start2Target, orthog1);

            // Now that we have those two orthogonal vectors, we can project any vector onto the two planes defined by them to give us the vector component that is parallel to start2Target.
            Vector3 target2end = end - target;

            Vector3 overshoot = target2end.ProjectIntoPlane(orthog1).ProjectIntoPlane(orthog2);

            // finally how long is it? We know it is parrallel to start2target, so if we add it to start2target, and then get the difference of the lengths, that should give us a positive or negative
            double overshootLength = (start2Target + overshoot).magnitude - start2Target.magnitude;

            return overshootLength;
        }

        public override string ToString() {
            string resultText = "Simulation result\n{";

            resultText += "Inputs:\n{";
            if (null != input_initialOrbit) {
                resultText += "\n input_initialOrbit: " + input_initialOrbit.ToString();
            }
            resultText += "\n input_UT: " + input_UT;
            //resultText += "\n input_dragMassExcludingUsedParachutes: " + input_dragMassExcludingUsedParachutes;
            resultText += "\n input_mass: " + input_mass;
            if (null != input_descentSpeedPolicy) {
                resultText += "\n input_descentSpeedPolicy: " + input_descentSpeedPolicy.ToString();
            }
            resultText += "\n input_decelEndAltitudeASL: " + input_decelEndAltitudeASL;
            resultText += "\n input_maxThrustAccel: " + input_maxThrustAccel;
            resultText += "\n input_parachuteSemiDeployMultiplier: " + input_parachuteSemiDeployMultiplier;
            resultText += "\n input_probableLandingSiteASL: " + input_probableLandingSiteASL;
            resultText += "\n input_multiplierHasError: " + input_multiplierHasError;
            resultText += "\n input_dt: " + input_dt;
            resultText += "\n}";
            resultText += "\nid: " + id;
            resultText += "\noutcome: " + outcome;
            resultText += "\nmaxdt: " + maxdt;
            resultText += "\ntimeToComplete: " + timeToComplete;
            resultText += "\nendUT: " + endUT;
            if (null != referenceFrame) {
                resultText += "\nstartPosition: " + referenceFrame.WorldPositionAtCurrentTime(startPosition);
            }
            if (null != referenceFrame) {
                resultText += "\nendPosition: " + referenceFrame.WorldPositionAtCurrentTime(endPosition);
            }
            resultText += "\nendASL: " + endASL;
            resultText += "\nendVelocity: " + endVelocity.longitude + "," + endVelocity.latitude + "," + endVelocity.radius;
            resultText += "\nmaxDragGees: " + maxDragGees;
            resultText += "\ndeltaVExpended: " + deltaVExpended;
            resultText += "\nmultiplierHasError: " + multiplierHasError;
            resultText += "\nparachuteMultiplier: " + parachuteMultiplier;
            resultText += "\n}";

            return (resultText);

        }
    }
}
