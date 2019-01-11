using System;
using UnityEngine;
using kOSMainframe.UnityToolbag;

namespace kOSMainframe.Numerics {
    public static class LineMinOptimizer {
        public static Vector2d Optimize(Func2 func, Vector2d p, Vector2d xi, double tolerance, int maxIterations, out double fmin) {
            var lineFunc = LineFunction2.pool.Borrow();
            lineFunc.Init(func, p, xi);
            double xmin = BrentOptimizer.Optimize(lineFunc.Evaluate, 0.0, 1.0, tolerance, maxIterations, out fmin);
            LineFunction2.pool.Release(lineFunc);

            return p + xmin * xi;
        }

        public static Vector3d Optimize(Func3 func, Vector3d p, Vector3d xi, double tolerance, int maxIterations, out double fmin) {
            var lineFunc = LineFunction3.pool.Borrow();
            lineFunc.Init(func, p, xi);
            double xmin = BrentOptimizer.Optimize(lineFunc.Evaluate, 0.0, 1.0, tolerance, maxIterations, out fmin);
            LineFunction3.pool.Release(lineFunc);

            return p + xmin * xi;
        }
    }

    class LineFunction2  {
        public static Pool<LineFunction2> pool = new Pool<LineFunction2>(() => new LineFunction2(), f => {});
        private Func2 func;
        private Vector2d p;
        private Vector2d xi;

        public void Init(Func2 func, Vector2d p, Vector2d xi) {
            this.func = func;
            this.p = p;
            this.xi = xi;
        }

        public double Evaluate(double x) {
            Vector2d pt = p + x * xi;
            return func(pt.x, pt.y);
        }
    }

    class LineFunction3  {
        public static Pool<LineFunction3> pool = new Pool<LineFunction3>(() => new LineFunction3(), f => { });
        private Func3 func;
        private Vector3d p;
        private Vector3d xi;

        public void Init(Func3 func, Vector3d p, Vector3d xi) {
            this.func = func;
            this.p = p;
            this.xi = xi;
        }

        public double Evaluate(double x) {
            Vector3d pt = p + x * xi;
            return func(pt.x, pt.y, pt.z);
        }
    }
}
