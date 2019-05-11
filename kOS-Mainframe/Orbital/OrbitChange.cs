using System;
using kOSMainframe.Numerics;

namespace kOSMainframe.Orbital {
    public static class OrbitChange {
        /// <summary>
        /// Computes the speed of a circular orbit of a given radius for a given body.
        /// </summary>
        public static double CircularOrbitSpeed(IBody body, double radius) {
            //v = sqrt(GM/r)
            return Math.Sqrt(body.gravParameter / radius);
        }

        /// <summary>
        /// Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        /// </summary>
        public static NodeParameters Circularize(IOrbit o, double UT) {
            Vector3d desiredVelocity = CircularOrbitSpeed(o.ReferenceBody, o.Radius(UT)) * o.Horizontal(UT);
            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            return o.DeltaVToNode(UT,  desiredVelocity - actualVelocity);
        }

        /// <summary>
        /// Computes the deltaV of the burn needed to set a given PeR and ApR at at a given UT.
        /// </summary>
        /// <returns>The ellipticize.</returns>
        public static NodeParameters Ellipticize(IOrbit o, double UT, double newPeR, double newApR) {
            double radius = o.Radius(UT);

            //sanitize inputs
            newPeR = ExtraMath.Clamp(newPeR, 0 + 1, radius - 1);
            newApR = Math.Max(newApR, radius + 1);

            double GM = o.ReferenceBody.gravParameter;
            double E = -GM / (newPeR + newApR); //total energy per unit mass of new orbit
            double L = Math.Sqrt(Math.Abs((Math.Pow(E * (newApR - newPeR), 2) - GM * GM) / (2 * E))); //angular momentum per unit mass of new orbit
            double kineticE = E + GM / radius; //kinetic energy (per unit mass) of new orbit at UT
            double horizontalV = L / radius;   //horizontal velocity of new orbit at UT
            double verticalV = Math.Sqrt(Math.Abs(2 * kineticE - horizontalV * horizontalV)); //vertical velocity of new orbit at UT

            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);

            //untested:
            verticalV *= Math.Sign(Vector3d.Dot(o.Up(UT), actualVelocity));

            Vector3d desiredVelocity = horizontalV * o.Horizontal(UT) + verticalV * o.Up(UT);
            return o.DeltaVToNode(UT, desiredVelocity - actualVelocity);
        }

        //Computes the delta-V of the burn required to attain a given periapsis, starting from
        //a given orbit and burning at a given UT. Throws an ArgumentException if given an impossible periapsis.
        //The computed burn is always horizontal, though this may not be strictly optimal.
        public static NodeParameters ChangePeriapsis(Orbit o, double UT, double newPeR) {
            double radius = o.Radius(UT);

            //sanitize input
            newPeR = ExtraMath.Clamp(newPeR, 0 + 1, radius - 1);

            //are we raising or lowering the periapsis?
            bool raising = (newPeR > o.PeR);
            Vector3d burnDirection = (raising ? 1 : -1) * o.Horizontal(UT);

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising) {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;
                while (o.PerturbedOrbit(UT, maxDeltaV * burnDirection).PeR < newPeR) {
                    minDeltaV = maxDeltaV; //narrow the range
                    maxDeltaV *= 2;
                    if (maxDeltaV > 100000) break; //a safety precaution
                }
            } else {
                //when lowering periapsis, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                maxDeltaV = Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }

            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01) {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testPeriapsis = o.PerturbedOrbit(UT, testDeltaV * burnDirection).PeR;

                if ((testPeriapsis > newPeR && raising) || (testPeriapsis < newPeR && !raising)) {
                    maxDeltaV = testDeltaV;
                } else {
                    minDeltaV = testDeltaV;
                }
            }

            return o.DeltaVToNode(UT, ((maxDeltaV + minDeltaV) / 2) * burnDirection);
        }

        public static bool ApoapsisIsHigher(double ApR, double than) {
            if (than > 0 && ApR < 0) return true;
            if (than < 0 && ApR > 0) return false;
            return ApR > than;
        }

        //Computes the delta-V of the burn at a given UT required to change an orbits apoapsis to a given value.
        //The computed burn is always prograde or retrograde, though this may not be strictly optimal.
        //Note that you can pass in a negative apoapsis if the desired final orbit is hyperbolic
        public static NodeParameters ChangeApoapsis(Orbit o, double UT, double newApR) {
            double radius = o.Radius(UT);

            //sanitize input
            if (newApR > 0) newApR = Math.Max(newApR, radius + 1);

            //are we raising or lowering the periapsis?
            bool raising = ApoapsisIsHigher(newApR, o.ApR);

            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising) {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;

                double ap = o.PerturbedOrbit(UT, maxDeltaV * burnDirection).ApR;
                while (ApoapsisIsHigher(newApR, ap)) {
                    minDeltaV = maxDeltaV; //narrow the range
                    maxDeltaV *= 2;
                    ap = o.PerturbedOrbit(UT, maxDeltaV * burnDirection).ApR;
                    if (maxDeltaV > 100000) break; //a safety precaution
                }
            } else {
                //when lowering apoapsis, we burn retrograde, and max possible deltaV is total velocity
                maxDeltaV = o.SwappedOrbitalVelocityAtUT(UT).magnitude;
            }

            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01) {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testApoapsis = o.PerturbedOrbit(UT, testDeltaV * burnDirection).ApR;

                bool above = ApoapsisIsHigher(testApoapsis, newApR);

                if ((raising && above) || (!raising && !above)) {
                    maxDeltaV = testDeltaV;
                } else {
                    minDeltaV = testDeltaV;
                }
            }

            return o.DeltaVToNode(UT, ((maxDeltaV + minDeltaV) / 2) * burnDirection);
        }

        //Computes the delta-V of the burn required to change an orbit's inclination to a given value
        //at a given UT. If the latitude at that time is too high, so that the desired inclination
        //cannot be attained, the burn returned will achieve as low an inclination as possible (namely, inclination = latitude).
        //The input inclination is in degrees.
        //Note that there are two orbits through each point with a given inclination. The convention used is:
        //   - first, clamp newInclination to the range -180, 180
        //   - if newInclination > 0, do the cheaper burn to set that inclination
        //   - if newInclination < 0, do the more expensive burn to set that inclination
        public static NodeParameters ChangeInclination(Orbit o, double UT, double newInclination) {
            double latitude = o.referenceBody.GetLatitude(o.SwappedAbsolutePositionAtUT(UT));
            double desiredHeading = OrbitToGround.HeadingForInclination(newInclination, latitude);
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(UtilMath.Deg2Rad * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(UtilMath.Deg2Rad * desiredHeading) * o.North(UT);
            if (Vector3d.Dot(actualHorizontalVelocity, northComponent) < 0) northComponent *= -1;
            if (ExtraMath.ClampDegrees180(newInclination) < 0) northComponent *= -1;
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return o.DeltaVToNode(UT, desiredHorizontalVelocity - actualHorizontalVelocity);
        }
    }
}
