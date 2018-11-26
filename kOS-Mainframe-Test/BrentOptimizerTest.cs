using NUnit.Framework;
using kOSMainframe.ExtraMath;
using System;
using System.Collections.Generic;

namespace kOSMainframeTest {
    [TestFixture]
    public class BrentOptimizerTest {
        [Test]
        public void TestBesselMinima() {
            const double EQL = 1e-4;
            List<double> mins = new List<double>();

            for (int i = 0; i < 100; i++) {
                double ax = i;
                double bx = i + 1;
                double fmin;
                double xmin = BrentOptimizer.Optimize(new DelegateFunction(Bessel.J0), ax, bx, 1e-6, 1000, out fmin);

                if (mins.FindIndex(min => Math.Abs(xmin - min) < EQL) < 0) {
                    mins.Add(xmin);
                }
            }

            Assert.That(mins, Has.Count.EqualTo(16));
            foreach(var min in mins) {
                Assert.AreEqual(0.0, Bessel.J1(min), 1e-5);
            }
            Assert.AreEqual(mins[ 0],  3.831705, 1e-5);
            Assert.AreEqual(mins[ 1], 10.173468, 1e-5);
            Assert.AreEqual(mins[ 2], 16.470634, 1e-5);
            Assert.AreEqual(mins[ 3], 22.760083, 1e-5);
            Assert.AreEqual(mins[ 4], 29.046824, 1e-5);
            Assert.AreEqual(mins[ 5], 35.332323, 1e-5);
            Assert.AreEqual(mins[ 6], 41.617094, 1e-5);
            Assert.AreEqual(mins[ 7], 47.901455, 1e-5);
            Assert.AreEqual(mins[ 8], 54.185563, 1e-5);
            Assert.AreEqual(mins[ 9], 60.469465, 1e-5);
            Assert.AreEqual(mins[10], 66.753226, 1e-5);
            Assert.AreEqual(mins[11], 73.036905, 1e-5);
            Assert.AreEqual(mins[12], 79.320476, 1e-5);
            Assert.AreEqual(mins[13], 85.604020, 1e-5);
            Assert.AreEqual(mins[14], 91.887500, 1e-5);
            Assert.AreEqual(mins[15], 98.170918, 1e-5);
        }
    }
}
