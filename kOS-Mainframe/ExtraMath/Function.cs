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

    public class DelegateFunction : Function {
        public delegate double delegate_func(double x);
        private delegate_func func;

        public DelegateFunction(delegate_func func) {
            this.func = func;
        }

        public double Evaluate(double x) {
            return func(x);
        }
    }

    public class DelegateFunction2 : Function2 {
        public delegate double delegate_func(double x, double y);
        private delegate_func func;

        public DelegateFunction2(delegate_func func) {
            this.func = func;
        }

        public double Evaluate(Vector2d x) {
            return func(x.x, x.y);
        }
    }
}
