using System;
using kOSMainframe.ExtraMath;

namespace kOSMainframe.Landing {
    //A ReferenceFrame is a scheme for converting Vector3d positions and velocities into AbsoluteVectors, and vice versa
    public class ReferenceFrame {
        private double epoch;
        private Vector3d lat0lon0AtStart;
        private Vector3d lat0lon90AtStart;
        private Vector3d lat90AtStart;
        private CelestialBody referenceBody;

        public void UpdateAtCurrentTime(CelestialBody body) {
            lat0lon0AtStart = body.GetSurfaceNVector(0, 0);
            lat0lon90AtStart = body.GetSurfaceNVector(0, 90);
            lat90AtStart = body.GetSurfaceNVector(90, 0);
            epoch = Planetarium.GetUniversalTime();
            referenceBody = body;
        }

        //Vector3d must be either a position RELATIVE to referenceBody, or a velocity
        public AbsoluteVector ToAbsolute(Vector3d vector3d, double UT) {
            AbsoluteVector absolute = new AbsoluteVector();

            absolute.latitude = Latitude(vector3d);

            double longitude = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(vector3d.normalized, lat0lon90AtStart), Vector3d.Dot(vector3d.normalized, lat0lon0AtStart));
            longitude -= 360 * (UT - epoch) / referenceBody.rotationPeriod;
            absolute.longitude = Functions.ClampDegrees180(longitude);

            absolute.radius = vector3d.magnitude;

            absolute.UT = UT;

            return absolute;
        }

        //Interprets a given AbsoluteVector as a position, and returns the corresponding Vector3d position
        //in world coordinates.
        public Vector3d WorldPositionAtCurrentTime(AbsoluteVector absolute) {
            return referenceBody.position + WorldVelocityAtCurrentTime(absolute);
        }


        public Vector3d BodyPositionAtCurrentTime(AbsoluteVector absolute) {
            return referenceBody.position + absolute.radius * referenceBody.GetSurfaceNVector(absolute.latitude, absolute.longitude);
        }


        //Interprets a given AbsoluteVector as a velocity, and returns the corresponding Vector3d velocity
        //in world coordinates.
        public Vector3d WorldVelocityAtCurrentTime(AbsoluteVector absolute) {
            double now = Planetarium.GetUniversalTime();
            double unrotatedLongitude = Functions.ClampDegrees360(absolute.longitude - 360 * (now - absolute.UT) / referenceBody.rotationPeriod);
            return absolute.radius * referenceBody.GetSurfaceNVector(absolute.latitude, unrotatedLongitude);
        }

        public double Latitude(Vector3d vector3d) {
            return UtilMath.Rad2Deg * Math.Asin(Vector3d.Dot(vector3d.normalized, lat90AtStart));
        }

    }
}
