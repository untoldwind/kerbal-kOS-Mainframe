using System;

namespace kOSMainframe.ExtraMath {
    public static class Hyperbolic {
        //asinh(x) = log(x + sqrt(x^2 + 1))
        public static double Asinh(double x) {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }

        //acosh(x) = log(x + sqrt(x^2 - 1))
        public static double Acosh(double x) {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        //atanh(x) = (log(1+x) - log(1-x))/2
        public static double Atanh(double x) {
            return 0.5 * (Math.Log(1 + x) - Math.Log(1 - x));
        }
    }
}
