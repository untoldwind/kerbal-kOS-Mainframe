using System;
namespace kOSMainframe.Numerics {
    public interface DerivativeFunction : Function {
        double Derivative(double x);
    }
}
