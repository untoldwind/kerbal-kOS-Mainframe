using System;
namespace kOSMainframe.ExtraMath {
    public static class SecantSolver {
        public static double Solve(Function p, double x1, double x2, double tolerance, int maxIterations) {
            double rts, t;
            double f1 = p.Evaluate(x1);
            double f = p.Evaluate(x2);

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
                f = p.Evaluate(rts);
                if (Math.Abs(dx) < tolerance || f == 0.0) return rts;
            }
            throw new Exception("SecantSolver reached iteration limit of " + maxIterations + " on " + p.ToString());
        }
    }
}
