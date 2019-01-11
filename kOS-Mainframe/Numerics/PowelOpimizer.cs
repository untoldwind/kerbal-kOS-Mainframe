using System;
using UnityEngine;

namespace kOSMainframe.Numerics {
    public static class PowelOpimizer {
        private const double TINY = 1.0e-25;

        public static Vector2d Optimize(Function2 func, Vector2d p, double tolerance, int maxIterations, out double fmin) {
            Vector2d[] ximat = { new Vector2d(1.0, 0.0), new Vector2d(0.0, 1.0) };
            double fret = func.Evaluate(p);
            double fptt;
            Vector2d pt = p, ptt;

            for (int iter = 0; iter < maxIterations; iter++) {
                double fp = fret;
                int idel = -1;
                double del = 0.0;

                for (int i = 0; i < 2; i++ ) {
                    fptt = fret;
                    p = LineMinOptimizer.Optimize(func, p, ximat[i], tolerance, maxIterations, out fret);
                    if(fptt - fret > del) {
                        del = fptt - fret;
                        idel = i;
                    }
                }
                if(2.0 *(fp-fret) <= tolerance * (Math.Abs(fp) + Math.Abs(fret)) + TINY) {
                    fmin = fret;
                    return p;
                }
                ptt = 2.0 * p - pt;
                Vector2d xi = p - pt;
                pt = p;
                fptt = func.Evaluate(ptt);
                if(fptt < fp) {
                    double t = 2.0 * (fp - 2.0 * fret + fptt) * (fp - fret - del) * (fp - fret - del) - del * (fp - fptt) * (fp - fptt);
                    if (t < 0.0) {
                        p = LineMinOptimizer.Optimize(func, p, xi, tolerance, maxIterations, out fret);
                        if(idel >= 0)                        {
                            ximat[idel] = ximat[1];
                            ximat[1] = xi;
                        }
                    }
                }
            }
            throw new Exception("PowelOpimizer reached iteration limit of " + maxIterations + " on " + func.ToString());
        }

        public static Vector3d Optimize(Function3 func, Vector3d p, double tolerance, int maxIterations, out double fmin) {
            Vector3d[] ximat = { new Vector3d(1.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0), new Vector3d(0.0, 0.0, 1.0) };
            double fret = func.Evaluate(p);
            double fptt;
            Vector3d pt = p, ptt;

            for (int iter = 0; iter < maxIterations; iter++) {
                double fp = fret;
                int idel = 0;
                double del = 0.0;

                for (int i = 0; i < 3; i++) {
                    fptt = fret;
                    p = LineMinOptimizer.Optimize(func, p, ximat[i], tolerance, maxIterations, out fret);
                    if (fptt - fret > del) {
                        del = fptt - fret;
                        idel = i;
                    }
                }
                if (2.0 * (fp - fret) <= tolerance * (Math.Abs(fp) + Math.Abs(fret)) + TINY) {
                    fmin = fret;
                    return p;
                }
                ptt = 2.0 * p - pt;
                Vector3d xi = p - pt;
                pt = p;
                fptt = func.Evaluate(ptt);
                if (fptt < fp) {
                    double t = 2.0 * (fp - 2.0 * fret + fptt) * (fp - fret - del) * (fp - fret - del) - del * (fp - fptt) * (fp - fptt);
                    if (t < 0.0 ) {
                        p = LineMinOptimizer.Optimize(func, p, xi, tolerance, maxIterations, out fret);
                        if(idel >= 0) {
                            ximat[idel] = ximat[2];
                            ximat[2] = xi;
                        }
                    }
                }
            }
            throw new Exception("PowelOpimizer reached iteration limit of " + maxIterations + " on " + func.ToString());
        }

    }
}
