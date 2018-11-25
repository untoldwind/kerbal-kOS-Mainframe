using System;
using UnityEngine;

namespace kOSMainframe.ExtraMath {
    public interface Function {
        double Evaluate(double x);
    }

    public interface Function2 {
        double Evaluate(Vector2d p);
    }

    public interface Function3 {
        double Evaluate(Vector3d p);
    }

    public interface Function4 {
        double Evaluate(Vector4d p);
    }
}
