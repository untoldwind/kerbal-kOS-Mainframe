using System;
using UnityEngine;

namespace kOSMainframe.Numerics {
    public static class MathExtensions {
        // +/- infinity is not a finite number (not finite)
        // NaN is also not a finite number (not a number)
        public static bool IsFinite(this double v) {
            return !Double.IsNaN(v) && !Double.IsInfinity(v);
        }

        public static double NextGaussian(this System.Random r, double mu = 0, double sigma = 1) {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();

            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                  Math.Sin(2.0 * Math.PI * u2);

            var rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

        public static Vector3 ProjectIntoPlane(this Vector3 vector, Vector3 planeNormal) {
            return vector - Vector3.Project(vector, planeNormal);
        }

        public static Vector3d ProjectOnPlane(this Vector3d vector, Vector3d planeNormal) {
            return vector - Vector3d.Project(vector, planeNormal);
        }
    }
}
