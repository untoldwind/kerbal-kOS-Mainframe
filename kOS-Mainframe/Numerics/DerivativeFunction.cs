using System;
namespace kOSMainframe.Numerics {
    public interface DerivativeFunction  {
        double Evaluate(double x);

        double Derivative(double x);
    }
}
