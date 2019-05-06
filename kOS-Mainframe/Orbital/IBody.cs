using UnityEngine;
using System;

namespace kOSMainframe.Orbital {
    public interface IBody {
        Vector3d position { get; }

        double Radius { get; }

        Vector3d transformUp { get; }
        Vector3d transformRight { get; }

        double GetLatitude(Vector3d position);

        double GetLongitude(Vector3d position);

        double rotationAngle { get; }

        double rotationPeriod { get; }

        double gravParameter { get; }

        double sphereOfInfluence { get; }

        float[] timeWarpAltitudeLimits { get; }

        IOrbit orbit { get; }

        IBody referenceBody { get; }

        IOrbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, double UT);

        IOrbit newOrbit(double inclination, double eccentricity, double semiMajorAxis, double lan, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch);
    }

    public class BodyWrapper : IBody {
        private CelestialBody body;

        public BodyWrapper(CelestialBody body) {
            this.body = body;
        }

        public Vector3d position => body.position;

        public double Radius => body.Radius;

        public Vector3d transformUp => body.transform.up;
        public Vector3d transformRight => body.transform.right;

        public double rotationAngle => body.rotationAngle;

        public double rotationPeriod => body.rotationPeriod;

        public double gravParameter => body.gravParameter;

        public float[] timeWarpAltitudeLimits => body.timeWarpAltitudeLimits;

        public IOrbit orbit => new OrbitWrapper(body.orbit);

        public double sphereOfInfluence => body.sphereOfInfluence;

        public IBody referenceBody => new BodyWrapper(body.referenceBody);

        public double GetLatitude(Vector3d position) {
            return body.GetLatitude(position);
        }

        public double GetLongitude(Vector3d position) {
            return body.GetLongitude(position);
        }

        public IOrbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, double UT) {
            return Helper.OrbitFromStateVectors(pos, vel, body, UT).wrap();
        }

        public IOrbit newOrbit(double inclination, double eccentricity, double semiMajorAxis, double lan, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch) {
            return new Orbit(inclination, eccentricity, semiMajorAxis, lan, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, body).wrap();
        }
    }
}
