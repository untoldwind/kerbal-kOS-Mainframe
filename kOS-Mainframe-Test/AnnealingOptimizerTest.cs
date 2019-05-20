using kOSMainframe.Numerics;
using NUnit.Framework;
using System;
using UnityEngine;

namespace kOSMainframeTest {
    [TestFixture]
    public class AnnealingOptimizerTest {
        public double TestFunc2(double x, double y) {
            return  20 *(Math.Sin(x) + Math.Sin(y)) + ((x - 3) * (x - 3) + (y - 3) * (y - 3));
        }

        [Test]
        public void TestTwoDimension() {
            Vector2d expected = new Vector2d(4.55, 4.55);
            Vector2d[] points = AnnealingOptimizer.Optimize(TestFunc2, new Vector2d(-10, -10), new Vector2d(10, 10), 50);

            foreach (var point in points) {
                if((point - expected).magnitude < 0.1) {
                    return;
                }
            }
            Assert.Fail("Nothing near 4.55,4.55");
        }
    }
}
