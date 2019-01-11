using System;
using NUnit.Framework;
using kOSMainframe.Numerics;
using UnityEngine;

namespace kOSMainframeTest {
    [TestFixture]
    public class PowelOptimizerTest {
        public double TestFunc3(double x, double y, double z) {
            return 0.5 - Bessel.J0((x - 1.0) * (x - 1.0) + (y - 2.0) * (y - 2.0) + (z - 3.0) * (z - 3.0));
        }

        [Test]
        public void TestMinimize3() {
            double fmin;
            Vector3d min = PowelOpimizer.Optimize(TestFunc3, new Vector3d(1.5, 1.5, 2.5), 1e-6, 1000, out fmin);

            Assert.AreEqual(1.0, min.x, 1e-4);
            Assert.AreEqual(2.0, min.y, 1e-4);
            Assert.AreEqual(3.0, min.z, 1e-4);
        }
    }
}
