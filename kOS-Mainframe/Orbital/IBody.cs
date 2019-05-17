using System;
using UnityEngine;

namespace kOSMainframe.Orbital {
    public interface IBody {
        string Name {
            get;
        }

        double GravParameter {
            get;
        }

        double SOIRadius {
            get;
        }

        IOrbit Orbit {
            get;
        }

        IOrbit CreateOrbit(Vector3d pos, Vector3d vel, double UT);
    }

    public class BodyWrapper : IBody {
        private readonly CelestialBody body;

        public BodyWrapper(CelestialBody body) {
            this.body = body;
        }

        public string Name => body.name;

        public double GravParameter => body.gravParameter;

        public double SOIRadius => body.sphereOfInfluence;

        public IOrbit Orbit => body.orbit.wrap();

        public IOrbit CreateOrbit(Vector3d relPos, Vector3d vel, double UT) {
            Orbit ret = new Orbit();
            ret.UpdateFromStateVectors(relPos.SwapYZ(), vel.SwapYZ(), body, UT);
            if (double.IsNaN(ret.argumentOfPeriapsis)) {
                Vector3d vectorToAN = Quaternion.AngleAxis(-(float)ret.LAN, Planetarium.up) * Planetarium.right;
                Vector3d vectorToPe = ret.eccVec.SwapYZ();
                double cosArgumentOfPeriapsis = Vector3d.Dot(vectorToAN, vectorToPe) / (vectorToAN.magnitude * vectorToPe.magnitude);
                //Squad's UpdateFromStateVectors is missing these checks, which are needed due to finite precision arithmetic:
                if (cosArgumentOfPeriapsis > 1) {
                    ret.argumentOfPeriapsis = 0;
                } else if (cosArgumentOfPeriapsis < -1) {
                    ret.argumentOfPeriapsis = 180;
                } else {
                    ret.argumentOfPeriapsis = Math.Acos(cosArgumentOfPeriapsis);
                }
            }
            return ret.wrap();
        }
    }
}
