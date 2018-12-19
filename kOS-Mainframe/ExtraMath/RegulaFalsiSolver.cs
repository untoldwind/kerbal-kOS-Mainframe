using System;
namespace kOSMainframe.ExtraMath {
    public class RegulaFalsiSolver {
        /// <summary>
        /// Regula-Falsi root finding method.
        /// </summary>
        /// <returns>The solve.</returns>
        /// <param name="F">Function to solve.</param>
        /// <param name="a">The first x value.</param>
        /// <param name="b">The second x value.</param>
        /// <param name="tolerance">Tolerance.</param>
        /// <param name="maxIterations">Max iterations.</param>
        public static double Solve(Function F, double a, double b, double tolerance, int maxIterations) {
            double c = a;
            double Fa = F.Evaluate(a);
            double Fb = F.Evaluate(b);
            double Fc = Fa;

            for (int j = 0; j < maxIterations; j++) {
                if (Math.Abs(Fc) < tolerance) break;
                c = (a * Fb - b * Fa) / (Fb - Fa);
                Fc = F.Evaluate(c);
                a = b;
                Fa = Fb;
                b = c;
                Fb = Fc;
            }
            return c;
        }
    }
}
