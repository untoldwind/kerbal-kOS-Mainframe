using System;
namespace kOSMainframe.ExtraMath {
    public class BrentOptimizer {
        private static double GOLD = 1.618034;
        private static double GLIMIT = 100.0;
        private static double TINY = 1.0e-20;
        private static double CGOLD = 0.3819660;

        public static double Optimize(Function func, double a, double b, double tollerance, int maxIterations) {
            double ax, bx, cx, fa, fb, fc;

            Bracket(func, a, b, out ax, out bx, out cx, out fa, out fb, out fc);

            a = (ax < cx ? ax : cx);
            b = (ax > cx ? ax : cx);
            double x = bx, w = bx, v = bx, u;
            double fx = fb, fw = fb, fv = fb, fu;
            double d = 0.0, e = 0.0;

            for (int i = 0; i < maxIterations; i++ ) {
                double xm = 0.5 * (a + b);
                double tol1 = tollerance * Math.Abs(x) + double.Epsilon;
                double tol2 = 2.0 * tol1;
                if (Math.Abs(x-xm) <= (tol2 - 0.5 *(b-a))) {
                    return x;
                }
                if(Math.Abs(e) > tol1) {
                    double r = (x - w) * (fx - fv);
                    double q = (x - v) * (fx - fw);
                    double p = (x - v) * q - (x - w) * r;
                    q = 2.0 * (q - r);
                    if (q > 0.0) p = -p;
                    q = Math.Abs(q);
                    double etemp = e;
                    e = d;
                    if(Math.Abs(p) >= Math.Abs(0.5 *q * etemp) || p <= q*(a - x) || p >= q*(b-x)) {
                        e = (x >= xm ? a - x : b - x);
                        d = CGOLD * e;
                    } else {
                        d = p / q;
                        u = x + d;
                        if(u - a < tol2 || b-u < tol2) {
                            d = xm - x < 0.0 ? -tol1 : tol1;
                        }
                    }
                } else {
                    e = (x >= xm ? a - x : b - x);
                    d = CGOLD * e;
                }
                u = (Math.Abs(d) >= tol1 ? x + d : (d < 0.0 ? x - tol1 : x + tol1));
                fu = func.Evaluate(u);
                if(fu <= fx) {
                    if (u >= x) {
                        a = x;
                    } else {
                        b = x;
                    }
                    v = w;
                    w = x;
                    x = u;
                    fv = fw;
                    fw = fx;
                    fx = fu;
                } else {
                    if ( u < x) {
                        a = u;
                    } else {
                        b = u;
                    }
                    if(fu <= fw || w == x) {
                        v = w;
                        w = u;
                        fv = fw;
                        fw = fu;
                    } else if(fu <= fv || v == x || v == w) {
                        v = u;
                        fv = fu;
                    }
                }
            }
            throw new Exception("BrentOptimizer reached iteration limit of " + maxIterations + " on " + func.ToString());
        }

        static void Bracket(Function func, double a, double b, out double ax, out double bx, out double cx, out double fa, out double fb, out double fc) {
            ax = a;
            bx = b;
            fa = func.Evaluate(ax);
            fb = func.Evaluate(bx);
            if ( fb > fa ) {
                double temp;

                temp = ax;
                ax = bx;
                bx = temp;
                temp = fa;
                fa = fb;
                fb = temp;
            }
            cx = bx + GOLD * (bx - ax);
            fc = func.Evaluate(cx);
            while(fb > fc) {
                double r = (bx - ax) * (fb - fc);
                double q = (bx - cx) * (fb - fa);
                double t = q - r < 0 ? -2.0 * Math.Max(Math.Abs(q - r), TINY) : 2.0 * Math.Max(Math.Abs(q - r), TINY);
                double u = bx - ((bx - cx) * q - (bx - ax) * r) / t;
                double ulim = bx * GLIMIT * (cx - bx);
                double fu;

                if((bx -u)*(u-cx) >0.0) {
                    fu = func.Evaluate(u);
                    if(fu < fc) {
                        ax = bx;
                        bx = u;
                        fa = fb;
                        fb = fu;
                        return;
                    } else if(fu > fb) {
                        cx = u;
                        fc = fu;
                        return;
                    }
                    u = cx + GOLD * (cx - bx);
                    fu = func.Evaluate(u);
                } else if ((cx-u)*(u-ulim) > 0.0) {
                    fu = func.Evaluate(u);
                    if(fu < fc) {
                        bx = cx;
                        cx = u;
                        u = u + GOLD * (u - cx);
                        fb = fc;
                        fc = fu;
                        fu = func.Evaluate(u);
                    }
                } else if((u-ulim)*(ulim-cx) >= 0.0) {
                    u = ulim;
                    fu = func.Evaluate(u);
                } else {
                    u = cx + GOLD * (cx - bx);
                    fu = func.Evaluate(u);
                }
                ax = bx;
                bx = cx;
                cx = u;
                fa = fb;
                fb = fc;
                fc = fu;
            }
        }
    }
}
