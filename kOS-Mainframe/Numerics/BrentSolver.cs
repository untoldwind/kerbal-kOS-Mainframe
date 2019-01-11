using System;
namespace kOSMainframe.Numerics {
    public static class BrentSolver {
        private const double EPS = 1e-15;

        public static double Solve(Function f, double x1, double x2, double tolerance, int maxIterations) {
            double a = x1;
            double b = x2;
            double fa = f.Evaluate(a);
            double fb = f.Evaluate(b);
            double c = b;
            double fc = fb;
            double d = b-a;
            double e = d;

            if ((fa < 0 && fb < 0) || (fa > 0 && fb > 0)) {
                // Root is not bracketed
                return SecantSolver.Solve(f, x1, x2, tolerance, maxIterations);
            }

            for (int i = 0; i < maxIterations; i++) {
                if((fb > 0.0 && fc > 0.0) || (fb < 0.0 && fc < 0.0)) {
                    c = a;
                    fc = fa;
                    d = b - a;
                    e = d;
                }
                if(Math.Abs(fc) < Math.Abs(fb)) {
                    a = b;
                    b = c;
                    c = a;
                    fa = fb;
                    fb = fc;
                    fc = fa;
                }
                double tol1 = 2.0 * EPS * Math.Abs(b) + 0.5 * tolerance;
                double xm = 0.5 * (c - b);
                if (Math.Abs(xm) <= tol1 || fb == 0.0) return b;
                if(Math.Abs(e) >= tol1 && Math.Abs(fa) > Math.Abs(fb)) {
                    double p, q, r;
                    double s = fb / fa;
                    if ( a == c) {
                        p = 2.0 * xm * s;
                        q = 1.0 - s;
                    } else {
                        q = fa / fc;
                        r = fb / fc;
                        p = s * (2.0 * xm * q * (r - q) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0) q = -q;
                    p = Math.Abs(p);
                    double min1 = 3.0 * xm * q - Math.Abs(tol1 * q);
                    double min2 = Math.Abs(e * q);
                    if(2.0*p < (min1 < min2 ? min1 : min2)) {
                        e = d;
                        d = p / q;
                    } else {
                        d = xm;
                        e = d;
                    }
                } else {
                    d = xm;
                    e = d;
                }
                a = b;
                fa = fb;
                if(Math.Abs(d) > tol1) {
                    b += d;
                } else {
                    b += (xm < 0.0 ? -tol1 : tol1);
                    fb = f.Evaluate(b);
                }
            }
            throw new Exception("BrentSolver reached iteration limit of " + maxIterations + " on " + f.ToString());
        }
    }
}
