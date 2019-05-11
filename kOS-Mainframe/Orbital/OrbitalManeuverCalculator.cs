using System;
using kOSMainframe.Numerics;
using UnityEngine;

namespace kOSMainframe.Orbital {
    public static class OrbitalManeuverCalculator {
        /// <summary>
        /// Compute the delta-V of the burn at the givent time required to enter an orbit with a period of (resonanceDivider-1)/resonanceDivider of the starting orbit period.
        /// </summary>
        public static Vector3d DeltaVToResonantOrbit(IOrbit o, double UT, double f) {
            double a = o.ApR;
            double p = o.PeR;

            // Thanks wolframAlpha for the Math
            // x = (a^3 f^2 + 3 a^2 f^2 p + 3 a f^2 p^2 + f^2 p^3)^(1/3)-a
            double x = Math.Pow(Math.Pow(a, 3) * Math.Pow(f, 2) + 3 * Math.Pow(a, 2) * Math.Pow(f, 2) * p + 3 * a * Math.Pow(f, 2) * Math.Pow(p, 2) + Math.Pow(f, 2) * Math.Pow(p, 3), 1d / 3) - a;

            if (x < 0)
                return Vector3d.zero;

            if (f > 1)
                return OrbitChange.ChangeApoapsis(o, UT, x).deltaV;
            else
                return OrbitChange.ChangePeriapsis(o, UT, x).deltaV;
        }

        // Compute the angular distance between two points on a unit sphere
        public static double Distance(double lat_a, double long_a, double lat_b, double long_b) {
            // Using Great-Circle Distance 2nd computational formula from http://en.wikipedia.org/wiki/Great-circle_distance
            // Note the switch from degrees to radians and back
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return UtilMath.Rad2Deg * Math.Atan2(Math.Sqrt(Math.Pow(Math.Cos(lat_b_rad) * Math.Sin(long_diff_rad), 2) +
                                                 Math.Pow(Math.Cos(lat_a_rad) * Math.Sin(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad), 2)),
                                                 Math.Sin(lat_a_rad) * Math.Sin(lat_b_rad) + Math.Cos(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad));
        }

        // Compute an angular heading from point a to point b on a unit sphere
        public static double Heading(double lat_a, double long_a, double lat_b, double long_b) {
            // Using Great-Circle Navigation formula for initial heading from http://en.wikipedia.org/wiki/Great-circle_navigation
            // Note the switch from degrees to radians and back
            // Original equation returns 0 for due south, increasing clockwise. We add 180 and clamp to 0-360 degrees to map to compass-type headings
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return ExtraMath.ClampDegrees360(180.0 / Math.PI * Math.Atan2(
                                                 Math.Sin(long_diff_rad),
                                                 Math.Cos(lat_a_rad) * Math.Tan(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(long_diff_rad)));
        }

        //Computes the deltaV of the burn needed to set a given LAN at a given UT.
        public static Vector3d DeltaVToShiftLAN(Orbit o, double UT, double newLAN) {
            Vector3d pos = o.SwappedAbsolutePositionAtUT(UT);
            // Burn position in the same reference frame as LAN
            double burn_latitude = o.referenceBody.GetLatitude(pos);
            double burn_longitude = o.referenceBody.GetLongitude(pos) + o.referenceBody.rotationAngle;

            const double target_latitude = 0; // Equator
            double target_longitude = 0; // Prime Meridian

            // Select the location of either the descending or ascending node.
            // If the descending node is closer than the ascending node, or there is no ascending node, target the reverse of the newLAN
            // Otherwise target the newLAN
            if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists()) {
                if (o.TimeOfDescendingNodeEquatorial(UT) < o.TimeOfAscendingNodeEquatorial(UT)) {
                    // DN is closer than AN
                    // Burning for the AN would entail flipping the orbit around, and would be very expensive
                    // therefore, burn for the corresponding Longitude of the Descending Node
                    target_longitude = ExtraMath.ClampDegrees360(newLAN + 180.0);
                } else {
                    // DN is closer than AN
                    target_longitude = ExtraMath.ClampDegrees360(newLAN);
                }
            } else if (o.AscendingNodeEquatorialExists() && !o.DescendingNodeEquatorialExists()) {
                // No DN
                target_longitude = ExtraMath.ClampDegrees360(newLAN);
            } else if (!o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists()) {
                // No AN
                target_longitude = ExtraMath.ClampDegrees360(newLAN + 180.0);
            } else {
                throw new ArgumentException("OrbitalManeuverCalculator.DeltaVToShiftLAN: No Equatorial Nodes");
            }
            double desiredHeading = ExtraMath.ClampDegrees360(Heading(burn_latitude, burn_longitude, target_latitude, target_longitude));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(UtilMath.Deg2Rad * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(UtilMath.Deg2Rad * desiredHeading) * o.North(UT);
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }


        public static Vector3d DeltaVForSemiMajorAxis(Orbit o, double UT, double newSMA) {
            bool raising = o.semiMajorAxis < newSMA;
            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);
            double minDeltaV = 0;
            double maxDeltaV;
            if (raising) {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;
                while (o.PerturbedOrbit(UT, maxDeltaV * burnDirection).semiMajorAxis < newSMA) {
                    maxDeltaV *= 2;
                    if (maxDeltaV > 100000)
                        break; //a safety precaution
                }
            } else {
                //when lowering the SMA, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                maxDeltaV = Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }
            // Debug.Log (String.Format ("We are {0} SMA to {1}", raising ? "raising" : "lowering", newSMA));
            // Debug.Log (String.Format ("Starting SMA iteration with maxDeltaV of {0}", maxDeltaV));
            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01) {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testSMA = o.PerturbedOrbit(UT, testDeltaV * burnDirection).semiMajorAxis;
                // Debug.Log (String.Format ("Testing dV of {0} gave an SMA of {1}", testDeltaV, testSMA));

                if ((testSMA < 0) || (testSMA > newSMA && raising) || (testSMA < newSMA && !raising)) {
                    maxDeltaV = testDeltaV;
                } else {
                    minDeltaV = testDeltaV;
                }
            }

            return ((maxDeltaV + minDeltaV) / 2) * burnDirection;
        }

        public static Vector3d DeltaVToShiftNodeLongitude(Orbit o, double UT, double newNodeLong) {
            // Get the location underneath the burn location at the current moment.
            // Note that this does NOT account for the rotation of the body that will happen between now
            // and when the vessel reaches the apoapsis.
            Vector3d pos = o.SwappedAbsolutePositionAtUT(UT);
            double burnRadius = o.Radius(UT);
            double oppositeRadius = 0;

            // Back out the rotation of the body to calculate the longitude of the apoapsis when the vessel reaches the node
            double degreeRotationToNode = (UT - Planetarium.GetUniversalTime()) * 360 / o.referenceBody.rotationPeriod;
            double NodeLongitude = o.referenceBody.GetLongitude(pos) - degreeRotationToNode;

            double LongitudeOffset = NodeLongitude - newNodeLong; // Amount we need to shift the Ap's longitude

            // Calculate a semi-major axis that gives us an orbital period that will rotate the body to place
            // the burn location directly over the newNodeLong longitude, over the course of one full orbit.
            // N tracks the number of full body rotations desired in a vessal orbit.
            // If N=0, we calculate the SMA required to let the body rotate less than a full local day.
            // If the resulting SMA would drop us under the 5x time warp limit, we deem it to be too low, and try again with N+1.
            // In other words, we allow the body to rotate more than 1 day, but less then 2 days.
            // As long as the resulting SMA is below the 5x limit, we keep increasing N until we find a viable solution.
            // This may place the apside out the sphere of influence, however.
            // TODO: find the cheapest SMA, instead of the smallest
            int N = -1;
            double target_sma = 0;

            while (oppositeRadius - o.referenceBody.Radius < o.referenceBody.timeWarpAltitudeLimits[4] && N < 20) {
                N++;
                double target_period = o.referenceBody.rotationPeriod * (LongitudeOffset / 360 + N);
                target_sma = Math.Pow((o.referenceBody.gravParameter * target_period * target_period) / (4 * Math.PI * Math.PI), 1.0 / 3.0); // cube roo
                oppositeRadius = 2 * (target_sma) - burnRadius;
            }
            return DeltaVForSemiMajorAxis(o, UT, target_sma);
        }
    }
}
