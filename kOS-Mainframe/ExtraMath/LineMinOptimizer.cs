using System;
using UnityEngine;
using kOSMainframe.UnityToolbag;

namespace kOSMainframe.ExtraMath {
    public static class LineMinOptimizer {
        public static Vector2d Optimize(Function2 func, Vector2d p, Vector2d xi, double tolerance, int maxIterations, out double fmin) {
            var lineFunc = LineFunction2.pool.Borrow();
            lineFunc.Init(func, p, xi);
            double xmin = BrentOptimizer.Optimize(lineFunc, 0.0, 1.0, tolerance, maxIterations, out fmin);
            LineFunction2.pool.Release(lineFunc);

            return p + xmin * xi;
        }

        public static Vector3d Optimize(Function3 func, Vector3d p, Vector3d xi, double tolerance, int maxIterations, out double fmin) {
            var lineFunc = LineFunction3.pool.Borrow();
            lineFunc.Init(func, p, xi);
            double xmin = BrentOptimizer.Optimize(lineFunc, 0.0, 1.0, tolerance, maxIterations, out fmin);
            LineFunction3.pool.Release(lineFunc);

            return p + xmin * xi;
        }
    }

    class LineFunction2 : Function {
        public static Pool<LineFunction2> pool = new Pool<LineFunction2>(() => new LineFunction2(), f => {});
        private Function2 func;
        private Vector2d p;
        private Vector2d xi;

        public void Init(Function2 func, Vector2d p, Vector2d xi) {
            this.func = func;
            this.p = p;
            this.xi = xi;
        }

        public double Evaluate(double x) {
            return func.Evaluate(p + x * xi);
        }
    }

    class LineFunction3 : Function {
        public static Pool<LineFunction3> pool = new Pool<LineFunction3>(() => new LineFunction3(), f => { });
        private Function3 func;
        private Vector3d p;
        private Vector3d xi;

        public void Init(Function3 func, Vector3d p, Vector3d xi) {
            this.func = func;
            this.p = p;
            this.xi = xi;
        }

        public double Evaluate(double x) {
            return func.Evaluate(p + x * xi);
        }
    }
}
