using System;
using NUnit.Framework;
using kOSMainframe.Numerics;
using UnityEngine;

namespace kOSMainframeTest {
    [TestFixture]
    public class PowelOptimizerTest {
        class TestFunc3 : Function3 {
            public double Evaluate(Vector3d p) {
                return 0.5 - Bessel.J0((p.x - 1.0) * (p.x - 1.0) + (p.y - 2.0) * (p.y - 2.0) + (p.z - 3.0) * (p.z - 3.0));
            }
        }

        [Test]
        public void TestMinimize3() {
            TestFunc3 func = new TestFunc3();
            double fmin;
            Vector3d min = PowelOpimizer.Optimize(func, new Vector3d(1.5, 1.5, 2.5), 1e-6, 1000, out fmin);

            Assert.AreEqual(1.0, min.x, 1e-4);
            Assert.AreEqual(2.0, min.y, 1e-4);
            Assert.AreEqual(3.0, min.z, 1e-4);
        }
    }
}
