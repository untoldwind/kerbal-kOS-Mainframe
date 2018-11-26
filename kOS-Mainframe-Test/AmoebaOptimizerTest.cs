using System;
using kOSMainframe.ExtraMath;
using UnityEngine;
using NUnit.Framework;

namespace kOSMainframeTest {
    [TestFixture]
    public class AmoebaOptimizerTest {
        class TestFunc3 : Function3 {
            public double Evaluate(Vector3d p) {
                return 0.6 - Bessel.J0((p.x - 0.5) * (p.x - 0.5) + (p.y - 0.6) * (p.y - 0.6) + (p.z - 0.7) * (p.z - 0.7));
            }
        }

        [Test]
        public void TestMinimize3() {
            TestFunc3 func = new TestFunc3();
            Vector3d xmin = AmoebaOptimizer.Optimize(func, Vector3d.zero, Vector3d.one, 1e-10, 1000);

            Assert.AreEqual(0.5, xmin.x, 1e-3);
            Assert.AreEqual(0.6, xmin.y, 1e-3);
            Assert.AreEqual(0.7, xmin.z, 1e-3);
        }
    }
}