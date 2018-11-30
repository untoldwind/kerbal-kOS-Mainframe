using System;
using UnityEngine;
using kOS.Suffixed;

namespace kOSMainframe.Orbital {
    public static class Helper {
        public static Orbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, CelestialBody body, double UT) {
            Orbit ret = new Orbit();
            ret.UpdateFromStateVectors(Orbit.Swizzle(pos - body.position), Orbit.Swizzle(vel), body, UT);
            if (double.IsNaN(ret.argumentOfPeriapsis)) {
                Vector3d vectorToAN = Quaternion.AngleAxis(-(float)ret.LAN, Planetarium.up) * Planetarium.right;
                Vector3d vectorToPe = Orbit.Swizzle(ret.eccVec);
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
            return ret;
        }
    }
}
