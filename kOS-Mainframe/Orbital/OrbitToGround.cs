using System;
using kOSMainframe.Numerics;
using kOSMainframe.VesselExtra;

namespace kOSMainframe.Orbital {
    public static class OrbitToGround {
        //Computes the heading of the ground track of an orbit with a given inclination at a given latitude.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east)
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForInclination(double inclinationDegrees, double latitudeDegrees) {
            double cosDesiredSurfaceAngle = Math.Cos(inclinationDegrees * UtilMath.Deg2Rad) / Math.Cos(latitudeDegrees * UtilMath.Deg2Rad);
            if (Math.Abs(cosDesiredSurfaceAngle) > 1.0) {
                //If inclination < latitude, we get this case: the desired inclination is impossible
                if (Math.Abs(ExtraMath.ClampDegrees180(inclinationDegrees)) < 90) return 90;
                else return 270;
            } else {
                double angleFromEast = (UtilMath.Rad2Deg) * Math.Acos(cosDesiredSurfaceAngle); //an angle between 0 and 180
                if (inclinationDegrees < 0) angleFromEast *= -1;
                //now angleFromEast is between -180 and 180

                return ExtraMath.ClampDegrees360(90 - angleFromEast);
            }
        }

        //See #676
        //Computes the heading for a ground launch at the specified latitude accounting for the body rotation.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east)
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForLaunchInclination(Vessel vessel, double inclinationDegrees) {
            CelestialBody body = vessel.mainBody;
            double latitudeDegrees = vessel.latitude;
            double orbVel = OrbitChange.CircularOrbitSpeed(body.wrap(), vessel.GetAltitudeASL() + body.Radius);
            double headingOne = HeadingForInclination(inclinationDegrees, latitudeDegrees) * UtilMath.Deg2Rad;
            double headingTwo = HeadingForInclination(-inclinationDegrees, latitudeDegrees) * UtilMath.Deg2Rad;
            double now = Planetarium.GetUniversalTime();
            Orbit o = vessel.orbit;

            Vector3d north = vessel.north;
            Vector3d east = vessel.east;

            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(now), o.SwappedOrbitalVelocityAtUT(now));
            Vector3d desiredHorizontalVelocityOne = orbVel * (Math.Sin(headingOne) * east + Math.Cos(headingOne) * north);
            Vector3d desiredHorizontalVelocityTwo = orbVel * (Math.Sin(headingTwo) * east + Math.Cos(headingTwo) * north);

            Vector3d deltaHorizontalVelocityOne = desiredHorizontalVelocityOne - actualHorizontalVelocity;
            Vector3d deltaHorizontalVelocityTwo = desiredHorizontalVelocityTwo - actualHorizontalVelocity;

            Vector3d desiredHorizontalVelocity;
            Vector3d deltaHorizontalVelocity;

            if (vessel.GetSpeedSurfaceHorizontal() < 200) {
                // at initial launch we have to head the direction the user specifies (90 north instead of -90 south).
                // 200 m/s of surface velocity also defines a 'grace period' where someone can catch a rocket that they meant
                // to launch at -90 and typed 90 into the inclination box fast after it started to initiate the turn.
                // if the rocket gets outside of the 200 m/s surface velocity envelope, then there is no way to tell MJ to
                // take a south travelling rocket and turn north or vice versa.
                desiredHorizontalVelocity = desiredHorizontalVelocityOne;
                deltaHorizontalVelocity = deltaHorizontalVelocityOne;
            } else {
                // now in order to get great circle tracks correct we pick the side which gives the lowest delta-V, which will get
                // ground tracks that cross the maximum (or minimum) latitude of a great circle correct.
                if (deltaHorizontalVelocityOne.magnitude < deltaHorizontalVelocityTwo.magnitude) {
                    desiredHorizontalVelocity = desiredHorizontalVelocityOne;
                    deltaHorizontalVelocity = deltaHorizontalVelocityOne;
                } else {
                    desiredHorizontalVelocity = desiredHorizontalVelocityTwo;
                    deltaHorizontalVelocity = deltaHorizontalVelocityTwo;
                }
            }

            // if you circularize in one burn, towards the end deltaHorizontalVelocity will whip around, but we want to
            // fall back to tracking desiredHorizontalVelocity
            if (Vector3d.Dot(desiredHorizontalVelocity.normalized, deltaHorizontalVelocity.normalized) < 0.90) {
                // it is important that we do NOT do the fracReserveDV math here, we want to ignore the deltaHV entirely at ths point
                return ExtraMath.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredHorizontalVelocity, east), Vector3d.Dot(desiredHorizontalVelocity, north)));
            }

            return ExtraMath.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(deltaHorizontalVelocity, east), Vector3d.Dot(deltaHorizontalVelocity, north)));
        }
    }
}
