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
    }
}
