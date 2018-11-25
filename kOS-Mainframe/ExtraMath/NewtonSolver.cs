using System;
namespace kOSMainframe.ExtraMath {
    //A simple root-finder. We really ought to use some existing root-finder that someone else has put a
    //lot of time and effort into, but this one seems to work
    public static class NewtonSolver {
        public static double Solve(DerivativeFunction p, double guess, double tolerance, int maxIterations) {
            double x = guess;
            int iter;
            for (iter = 0; iter < maxIterations; iter++) {
                double delta = p.Evaluate(x) / p.Derivative(x);
                if (Math.Abs(delta) < tolerance) break;
                x = x - delta;
            }
            if (iter == maxIterations) {
                throw new Exception("NewtonSolver reached iteration limit of " + maxIterations + " on polynomial " + p.ToString());
            }
            return x;
        }

        // Combination of bisection with Newton-Raphson
        public static double SolveSafe(DerivativeFunction p, double x1, double x2, double tolerance, int maxIterations) {
            double xh;
            double f1 = p.Evaluate(x1);
            double fh = p.Evaluate(x2);

            if((f1 < 0 && fh < 0) || (f1 > 0 && fh > 0)) {
                // Root is not bracketed
                return Solve(p, (x1 + x2) / 2, tolerance, maxIterations);
            }

            if (f1 == 0.0) return x1;
            if (fh == 0.0) return x2;

            if (f1 < 0) {
                xh = x2;
            } else {
                xh = x1;
                x1 = x2;
            }

            double rts = 0.5 * (x1 + x2);
            double dxold = Math.Abs(x2 - x1);
            double dx = dxold;
            double f = p.Evaluate(rts);
            double df = p.Derivative(rts);
            for (int i = 0; i < maxIterations; i++) {
                if(((rts-xh)*df-f)*((rts-x1)*df-f) > 0.0 ||
                        Math.Abs(2.0*f) > Math.Abs(dxold + df)) {
                    dxold = dx;
                    dx = 0.5 * (xh - x1);
                    rts = x1 + dx;
                    if (x1 == rts) return rts;
                } else {
                    dxold = dx;
                    dx = f / df;
                    double temp = rts;
                    rts -= dx;
                    if (temp == rts) return rts;
                }
                if (Math.Abs(dx) < tolerance) return rts;
                f = p.Evaluate(rts);
                df = p.Derivative(rts);
                if (f < 0.0) {
                    x1 = rts;
                } else {
                    xh = rts;
                }
            }
            throw new Exception("NewtonSolver reached iteration limit of " + maxIterations + " on polynomial " + p.ToString());
        }
    }
}
