using System;
using UnityEngine;

namespace kOSMainframe.ExtraMath {
    public class AmoebaOptimizer {
        private const double TINY = 1.0e-20;

        public static Vector2d Optimize(Function2 func, Vector2d guess, Vector2d perturbation, double tolerance, int maxIterations) {
            Vector2d[] p = { guess, guess + new Vector2d(perturbation.x,0.0), guess + new Vector2d(0.0, perturbation.y) };

            return Optimize(func, p, tolerance, maxIterations);
        }

        public static Vector2d Optimize(Function2 func, Vector2d[] p, double tolerance, int maxIterations) {
            int npts = p.Length;
            int nfunc = 0;
            double temp;
            Vector2d tempP;
            Vector2d psum = new Vector2d(0.0, 0.0);
            double[] y = new double[npts];

            for (int i = 0; i < npts; i++) {
                y[i] = func.Evaluate(p[i]);
                psum += p[i];
            }

            while (true) {
                int ilo = 0, ihi, inhi;
                if (y[0] > y[1]) {
                    inhi = 1;
                    ihi = 0;
                } else {
                    inhi = 0;
                    ihi = 1;
                }
                for (int i = 0; i < npts; i++) {
                    if (y[i] <= y[ilo]) ilo = i;
                    if (y[i] > y[ihi]) {
                        inhi = ihi;
                        ihi = i;
                    } else if (y[i] > y[inhi] && i != ihi) inhi = i;
                }
                double rtol = 2.0 * Math.Abs(y[ihi] - y[ilo]) / (Math.Abs(y[ihi]) + Math.Abs(y[ilo]) + TINY);
                if (rtol < tolerance) {
                    return psum / npts;
                }
                if (nfunc >= maxIterations) {
                    throw new Exception("AmoebaOptimizer reached iteration limit of " + maxIterations + " on " + func.ToString());
                }
                nfunc += 2;
                double ytry = Amotry(p, y, ref psum, func, ihi, -1.0);
                if(ytry < y[ilo]) {
                    ytry = Amotry(p, y, ref psum, func, ihi, 2.0);
                } else if (ytry > y[inhi]) {
                    double ysave = y[ihi];
                    ytry = Amotry(p, y, ref psum, func, ihi, 0.5);
                    if ( ytry >= ysave) {
                        for (int i = 0; i < npts; i++) {
                            psum = 0.5 * (p[i] + p[ilo]);
                            p[i] = psum;
                            y[i] = func.Evaluate(psum);
                        }
                        nfunc += 2;
                        psum.x = 0;
                        psum.y = 0;
                        for (int i = 0; i < npts; i++) {
                            psum += p[i];
                        }
                    }
                } else {
                    nfunc -= 1;
                }
            }
        }

        public static Vector3d Optimize(Function3 func, Vector3d guess, Vector3d perturbation, double tolerance, int maxIterations) {
            Vector3d[] p = { guess, guess + new Vector3d(perturbation.x, 0.0, 0.0), guess + new Vector3d(0.0, perturbation.y, 0.0), guess + new Vector3d(0.0, 0.0, perturbation.z) };

            return Optimize(func, p, tolerance, maxIterations);
        }

        public static Vector3d Optimize(Function3 func, Vector3d[] p, double tolerance, int maxIterations) {
            int npts = p.Length;
            int nfunc = 0;
            double temp;
            Vector3d tempP;
            Vector3d psum = new Vector3d(0.0, 0.0, 0.0);
            double[] y = new double[npts];

            for (int i = 0; i < npts; i++) {
                y[i] = func.Evaluate(p[i]);
                psum += p[i];
            }

            while (true) {
                int ilo = 0, ihi, inhi;
                if (y[0] > y[1]) {
                    inhi = 1;
                    ihi = 0;
                } else {
                    inhi = 0;
                    ihi = 1;
                }
                for (int i = 0; i < npts; i++) {
                    if (y[i] <= y[ilo]) ilo = i;
                    if (y[i] > y[ihi]) {
                        inhi = ihi;
                        ihi = i;
                    } else if (y[i] > y[inhi] && i != ihi) inhi = i;
                }
                double rtol = 2.0 * Math.Abs(y[ihi] - y[ilo]) / (Math.Abs(y[ihi]) + Math.Abs(y[ilo]) + TINY);
                if (rtol < tolerance) {
                    return psum / npts;
                }
                if (nfunc >= maxIterations) {
                    throw new Exception("AmoebaOptimizer reached iteration limit of " + maxIterations + " on " + func.ToString());
                }
                nfunc += 2;
                double ytry = Amotry(p, y, ref psum, func, ihi, -1.0);
                if (ytry < y[ilo]) {
                    ytry = Amotry(p, y, ref psum, func, ihi, 2.0);
                } else if (ytry > y[inhi]) {
                    double ysave = y[ihi];
                    ytry = Amotry(p, y, ref psum, func, ihi, 0.5);
                    if (ytry >= ysave) {
                        for (int i = 0; i < npts; i++) {
                            psum = 0.5 * (p[i] + p[ilo]);
                            p[i] = psum;
                            y[i] = func.Evaluate(psum);
                        }
                        nfunc += 3;
                        psum.x = 0;
                        psum.y = 0;
                        psum.z = 0;
                        for (int i = 0; i < npts; i++) {
                            psum += p[i];
                        }
                    }
                } else {
                    nfunc -= 1;
                }
            }
        }

        private static double Amotry(Vector2d[] p, double[] y, ref Vector2d psum, Function2 func, int ihi, double fac) {
            double fac1 = (1.0 - fac) / 2;
            double fac2 = fac1 - fac;
            Vector2d ptry = psum * fac1 - p[ihi] * fac2;
            double ytry = func.Evaluate(ptry);
            if (ytry < y[ihi]) {
                y[ihi] = ytry;
                psum += ptry - p[ihi];
                p[ihi] = ptry;
            }
            return ytry;
        }

        private static double Amotry(Vector3d[] p, double[] y, ref Vector3d psum, Function3 func, int ihi, double fac) {
            double fac1 = (1.0 - fac) / 3;
            double fac2 = fac1 - fac;
            Vector3d ptry = psum * fac1 - p[ihi] * fac2;
            double ytry = func.Evaluate(ptry);
            if (ytry < y[ihi]) {
                y[ihi] = ytry;
                psum += ptry - p[ihi];
                p[ihi] = ptry;
            }
            return ytry;
        }

    }
}
