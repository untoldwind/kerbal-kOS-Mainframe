using System;

namespace kOSMainframe.ExtraMath {
    //Represents a polynomial function of one variable, and can evaluate its own
    //derivative exactly.
    public class PolynomialFunction : DerivativeFunction {
        double[] coeffs; //coeffs[i] is the coefficient of x^i

        public PolynomialFunction(double[] coeffs) {
            this.coeffs = coeffs;
        }

        //Evaluate the polynomial at x
        public double Evaluate(double x) {
            double ret = 0;
            double pow = 1;
            for (int i = 0; i < coeffs.Length; i++) {
                ret += coeffs[i] * pow;
                pow *= x;
            }
            return ret;
        }

        //Evaluate the derivative of the polynomial at x
        public double Derivative(double x) {
            double ret = 0;
            double pow = 1;
            for (int i = 1; i < coeffs.Length; i++) {
                ret += i * coeffs[i] * pow;
                pow *= x;
            }
            return ret;
        }

        public override string ToString() {
            string ret = "";
            for (int i = 0; i < coeffs.Length; i++) {
                ret += coeffs[i].ToString() + " * x^" + i + " + ";
            }
            return ret;
        }
    }
}
