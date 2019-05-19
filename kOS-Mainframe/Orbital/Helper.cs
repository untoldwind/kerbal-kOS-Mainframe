using System;
using UnityEngine;
using kOS.Suffixed;

namespace kOSMainframe.Orbital {
    public static class Helper {
        /// <summary>
        /// Create a celestrial frame from two vectors.
        /// First will become X, second will become Z (ensuring its perpendicular to X.
        /// </summary>
        /// <returns>The frame.</returns>
        /// <param name="x">The desired X direction</param>
        /// <param name="z">The desired Z direction</param>
        public static Planetarium.CelestialFrame CreateFrame(Vector3d x, Vector3d z) {
            var frame = new Planetarium.CelestialFrame();

            frame.X = x.normalized;
            frame.Z = Vector3d.Exclude(frame.X, z).normalized;
            frame.Y = Vector3d.Cross(frame.Z, frame.X).normalized;

            return frame;
        }

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
