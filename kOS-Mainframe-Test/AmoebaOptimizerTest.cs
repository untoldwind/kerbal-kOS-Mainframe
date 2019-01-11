using System;
using kOSMainframe.Numerics;
using UnityEngine;
using NUnit.Framework;

namespace kOSMainframeTest {
    [TestFixture]
    public class AmoebaOptimizerTest {
        public double TestFunc3(double x, double y, double z) {
            return 0.6 - Bessel.J0((x - 0.5) * (x - 0.5) + (y - 0.6) * (y - 0.6) + (z - 0.7) * (z - 0.7));
        }

        [Test]
        public void TestMinimize3() {
            Vector3d xmin;
            int iter = AmoebaOptimizer.Optimize(TestFunc3, Vector3d.zero, Vector3d.one, 1e-10, 1000, out xmin);

            Assert.AreEqual(0.5, xmin.x, 1e-3);
            Assert.AreEqual(0.6, xmin.y, 1e-3);
            Assert.AreEqual(0.7, xmin.z, 1e-3);
        }
    }
}