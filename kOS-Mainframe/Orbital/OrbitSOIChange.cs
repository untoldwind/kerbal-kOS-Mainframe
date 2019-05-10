using System;
using UnityEngine;

namespace kOSMainframe.Orbital {
    public static class OrbitSOIChange {
        public static NodeParameters MoonReturnEjection(Orbit o, double UT, double targetPrimaryPeriapsis) {
            CelestialBody moon = o.referenceBody;
            CelestialBody primary = moon.referenceBody;

            //construct an orbit at the target radius around the primary, in the same plane as the moon. This is a fake target
            Orbit primaryOrbit = new Orbit(moon.orbit.inclination, moon.orbit.eccentricity, primary.Radius + targetPrimaryPeriapsis, moon.orbit.LAN, moon.orbit.argumentOfPeriapsis, moon.orbit.meanAnomalyAtEpoch, moon.orbit.epoch, primary);

            return InterplanetaryTransferEjection(o, UT, primaryOrbit, false);
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another.
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection.
        public static NodeParameters InterplanetaryTransferEjection(Orbit o, double UT, Orbit target, bool syncPhaseAngle) {
            Orbit planetOrbit = o.referenceBody.orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV;

            if (syncPhaseAngle) {
                //time the ejection burn to intercept the target
                NodeParameters idealParams = OrbitIntercept.HohmannTransfer(planetOrbit, target, UT);
                idealBurnUT = idealParams.time;
                idealDeltaV = idealParams.deltaV;
            } else {
                //don't time the ejection burn to intercept the target; we just care about the final peri/apoapsis
                idealBurnUT = UT;
                if (target.semiMajorAxis < planetOrbit.semiMajorAxis) idealDeltaV = OrbitChange.ChangePeriapsis(planetOrbit, idealBurnUT, target.semiMajorAxis).deltaV;
                else idealDeltaV = OrbitChange.ChangeApoapsis(planetOrbit, idealBurnUT, target.semiMajorAxis).deltaV;
            }

            //Compute the actual transfer orbit this ideal burn would lead to.
            Orbit transferOrbit = planetOrbit.PerturbedOrbit(idealBurnUT, idealDeltaV);

            //Now figure out how to approximately eject from our current orbit into the Hohmann orbit we just computed.

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealDeltaV;
            //project the desired exit direction into the current orbit plane to get the feasible exit direction
            Vector3d inPlaneSoiExitDirection = Vector3d.Exclude(o.SwappedOrbitNormal(), soiExitVelocity).normalized;

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up;
            Orbit sampleEjectionOrbit = Helper.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Math.Abs(Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity));

            //rotate the exit direction by 90 + the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn. Then convert this to a true anomaly and compute the time closest
            //to planetUT at which we will pass through that true anomaly.
            Vector3d ejectionPointDirection = Quaternion.AngleAxis(-(float)(90 + turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            double burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            if ((idealBurnUT - burnUT > o.period / 2) || (burnUT < UT)) {
                burnUT += o.period;
            }

            //rotate the exit direction by the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return o.DeltaVToNode(burnUT, ejectionVelocity - preEjectionVelocity);
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another.
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection.
        public static NodeParameters InterplanetaryLambertTransferEjection(Orbit o, double UT, Orbit target) {
            Orbit planetOrbit = o.referenceBody.orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            //time the ejection burn to intercept the target
            //idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);
            double vesselOrbitVelocity = OrbitChange.CircularOrbitSpeed(o.referenceBody.wrap(), o.semiMajorAxis);
            NodeParameters idealParams = OrbitIntercept.HohmannLambertTransfer(planetOrbit, target, UT, vesselOrbitVelocity);

            Logging.Debug("idealBurnUT = " + idealParams.time + ", idealDeltaV = " + idealParams.deltaV);

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealParams.deltaV;
            Logging.Debug("soiExitVelocity = " + (Vector3)soiExitVelocity);

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits
            Logging.Debug("soiExitEnergy = " + soiExitEnergy);
            Logging.Debug("ejectionRadius = " + ejectionRadius);

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);
            Logging.Debug("ejectionSpeed = " + ejectionSpeed);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up;
            Orbit sampleEjectionOrbit = Helper.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity);
            Logging.Debug("turningAngle = " + turningAngle);

            //sine of the angle between the vessel orbit and the desired SOI exit velocity
            double outOfPlaneAngle = (UtilMath.Deg2Rad) * (90 - Vector3d.Angle(soiExitVelocity, o.SwappedOrbitNormal()));
            Logging.Debug("outOfPlaneAngle (rad) = " + outOfPlaneAngle);

            double coneAngle = Math.PI / 2 - (UtilMath.Deg2Rad) * turningAngle;
            Logging.Debug("coneAngle (rad) = " + coneAngle);

            Vector3d exitNormal = Vector3d.Cross(-soiExitVelocity, o.SwappedOrbitNormal()).normalized;
            Vector3d normal2 = Vector3d.Cross(exitNormal, -soiExitVelocity).normalized;

            //unit vector pointing to the spot on our orbit where we will burn.
            //fails if outOfPlaneAngle > coneAngle.
            Vector3d ejectionPointDirection = Math.Cos(coneAngle) * (-soiExitVelocity.normalized)
                                              + Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle) * normal2
                                              - Math.Sqrt(Math.Pow(Math.Sin(coneAngle), 2) - Math.Pow(Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle), 2)) * exitNormal;

            Logging.Debug("soiExitVelocity = " + (Vector3)soiExitVelocity);
            Logging.Debug("vessel orbit normal = " + (Vector3)(1000 * o.SwappedOrbitNormal()));
            Logging.Debug("exitNormal = " + (Vector3)(1000 * exitNormal));
            Logging.Debug("normal2 = " + (Vector3)(1000 * normal2));
            Logging.Debug("ejectionPointDirection = " + ejectionPointDirection);

            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            double burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealParams.time - o.period);

            if ((idealParams.time - burnUT > o.period / 2) || (burnUT < UT)) {
                burnUT += o.period;
            }

            Vector3d ejectionOrbitNormal = Vector3d.Cross(ejectionPointDirection, soiExitVelocity).normalized;
            Logging.Debug("ejectionOrbitNormal = " + ejectionOrbitNormal);
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), ejectionOrbitNormal) * soiExitVelocity.normalized;
            Logging.Debug("ejectionBurnDirection = " + ejectionBurnDirection);
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return o.DeltaVToNode(burnUT, ejectionVelocity - preEjectionVelocity);
        }

        public static NodeParameters InterplanetaryBiImpulsiveEjection(Orbit o, double UT, Orbit target, double maxUT = double.PositiveInfinity) {
            Orbit planetOrbit = o.referenceBody.orbit;

            NodeParameters idealParams = OrbitIntercept.BiImpulsiveAnnealed(planetOrbit, target, UT, maxUT: maxUT);

            Logging.Debug("idealBurnUT = " + idealParams.time + ", idealDeltaV = " + idealParams.deltaV);

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealParams.deltaV;
            Logging.Debug("soiExitVelocity = " + (Vector3)soiExitVelocity);

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits
            Logging.Debug("soiExitEnergy = " + soiExitEnergy);
            Logging.Debug("ejectionRadius = " + ejectionRadius);

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);
            Logging.Debug("ejectionSpeed = " + ejectionSpeed);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up;
            Orbit sampleEjectionOrbit = Helper.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity);
            Logging.Debug("turningAngle = " + turningAngle);

            //sine of the angle between the vessel orbit and the desired SOI exit velocity
            double outOfPlaneAngle = (UtilMath.Deg2Rad) * (90 - Vector3d.Angle(soiExitVelocity, o.SwappedOrbitNormal()));
            Logging.Debug("outOfPlaneAngle (rad) = " + outOfPlaneAngle);

            double coneAngle = Math.PI / 2 - (UtilMath.Deg2Rad) * turningAngle;
            Logging.Debug("coneAngle (rad) = " + coneAngle);

            Vector3d exitNormal = Vector3d.Cross(-soiExitVelocity, o.SwappedOrbitNormal()).normalized;
            Vector3d normal2 = Vector3d.Cross(exitNormal, -soiExitVelocity).normalized;

            //unit vector pointing to the spot on our orbit where we will burn.
            //fails if outOfPlaneAngle > coneAngle.
            Vector3d ejectionPointDirection = Math.Cos(coneAngle) * (-soiExitVelocity.normalized)
                                              + Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle) * normal2
                                              - Math.Sqrt(Math.Pow(Math.Sin(coneAngle), 2) - Math.Pow(Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle), 2)) * exitNormal;

            Logging.Debug("soiExitVelocity = " + (Vector3)soiExitVelocity);
            Logging.Debug("vessel orbit normal = " + (Vector3)(1000 * o.SwappedOrbitNormal()));
            Logging.Debug("exitNormal = " + (Vector3)(1000 * exitNormal));
            Logging.Debug("normal2 = " + (Vector3)(1000 * normal2));
            Logging.Debug("ejectionPointDirection = " + ejectionPointDirection);

            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            double burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealParams.time - o.period);

            if ((idealParams.time - burnUT > o.period / 2) || (burnUT < UT)) {
                burnUT += o.period;
            }

            Vector3d ejectionOrbitNormal = Vector3d.Cross(ejectionPointDirection, soiExitVelocity).normalized;
            Logging.Debug("ejectionOrbitNormal = " + ejectionOrbitNormal);
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), ejectionOrbitNormal) * soiExitVelocity.normalized;
            Logging.Debug("ejectionBurnDirection = " + ejectionBurnDirection);
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return o.DeltaVToNode(burnUT, ejectionVelocity - preEjectionVelocity);
        }
    }
}
