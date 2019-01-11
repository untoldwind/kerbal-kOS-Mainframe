using System;
namespace kOSMainframe.Numerics {
    public static class SecantSolver {
        public static double Solve(Func1 p, double x1, double x2, double tolerance, int maxIterations) {
            double rts, t;
            double f1 = p(x1);
            double f = p(x2);

            if (Math.Abs(f1) < Math.Abs(f)) {
                rts = x1;
                x1 = x2;
                t = f;
                f = f1;
                f1 = t;
            } else {
                rts = x2;
            }
            for (int j = 0; j < maxIterations; j++) {
                double dx = (x1 - rts) * f / (f - f1);
                x1 = rts;
                f1 = f;
                rts += dx;
                f = p(rts);
                if (Math.Abs(dx) < tolerance || Math.Abs(f) < tolerance) return rts;
            }
            throw new Exception("SecantSolver reached iteration limit of " + maxIterations + " on " + p.ToString());
        }
    }
}
