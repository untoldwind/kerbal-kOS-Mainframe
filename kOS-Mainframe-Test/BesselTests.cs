using System;
using NUnit.Framework;

namespace kOSMainframeTest {
    [TestFixture]
    public class BesselTests {
        [Test]
        public void TestJ0() {
            Assert.AreEqual(-1.775968e-01, Bessel.J0(-5), 1e-7);
            Assert.AreEqual(-3.971498e-01, Bessel.J0(-4), 1e-7);
            Assert.AreEqual(-2.600520e-01, Bessel.J0(-3), 1e-7);
            Assert.AreEqual( 2.238908e-01, Bessel.J0(-2), 1e-7);
            Assert.AreEqual( 7.651977e-01, Bessel.J0(-1), 1e-7);
            Assert.AreEqual( 1.000000e+00, Bessel.J0( 0), 1e-7);
            Assert.AreEqual( 7.651977e-01, Bessel.J0( 1), 1e-7);
            Assert.AreEqual( 2.238908e-01, Bessel.J0( 2), 1e-7);
            Assert.AreEqual(-2.600520e-01, Bessel.J0( 3), 1e-7);
            Assert.AreEqual(-3.971498e-01, Bessel.J0( 4), 1e-7);
            Assert.AreEqual(-1.775968e-01, Bessel.J0( 5), 1e-7);
            Assert.AreEqual( 1.506453e-01, Bessel.J0( 6), 1e-7);
            Assert.AreEqual( 3.000793e-01, Bessel.J0( 7), 1e-7);
            Assert.AreEqual( 1.716508e-01, Bessel.J0( 8), 1e-7);
            Assert.AreEqual(-9.033361e-02, Bessel.J0( 9), 1e-7);
            Assert.AreEqual(-2.459358e-01, Bessel.J0(10), 1e-7);
            Assert.AreEqual(-1.711903e-01, Bessel.J0(11), 1e-7);
            Assert.AreEqual( 4.768931e-02, Bessel.J0(12), 1e-7);
            Assert.AreEqual( 2.069261e-01, Bessel.J0(13), 1e-7);
            Assert.AreEqual( 1.710735e-01, Bessel.J0(14), 1e-7);
        }

        [Test]
        public void TestJ1() {
            Assert.AreEqual( 3.275791e-01, Bessel.J1(-5), 1e-7);
            Assert.AreEqual( 6.604333e-02, Bessel.J1(-4), 1e-7);
            Assert.AreEqual(-3.390590e-01, Bessel.J1(-3), 1e-7);
            Assert.AreEqual(-5.767248e-01, Bessel.J1(-2), 1e-7);
            Assert.AreEqual(-4.400506e-01, Bessel.J1(-1), 1e-7);
            Assert.AreEqual( 0.000000e+00, Bessel.J1( 0), 1e-7);
            Assert.AreEqual( 4.400506e-01, Bessel.J1( 1), 1e-7);
            Assert.AreEqual( 5.767248e-01, Bessel.J1( 2), 1e-7);
            Assert.AreEqual( 3.390590e-01, Bessel.J1( 3), 1e-7);
            Assert.AreEqual(-6.604333e-02, Bessel.J1( 4), 1e-7);
            Assert.AreEqual(-3.275791e-01, Bessel.J1( 5), 1e-7);
            Assert.AreEqual(-2.766839e-01, Bessel.J1( 6), 1e-7);
            Assert.AreEqual(-4.682826e-03, Bessel.J1( 7), 1e-7);
            Assert.AreEqual( 2.346363e-01, Bessel.J1( 8), 1e-7);
            Assert.AreEqual( 2.453118e-01, Bessel.J1( 9), 1e-7);
            Assert.AreEqual( 4.347275e-02, Bessel.J1(10), 1e-7);
            Assert.AreEqual(-1.767853e-01, Bessel.J1(11), 1e-7);
            Assert.AreEqual(-2.234471e-01, Bessel.J1(12), 1e-7);
            Assert.AreEqual(-7.031805e-02, Bessel.J1(13), 1e-7);
            Assert.AreEqual( 1.333752e-01, Bessel.J1(14), 1e-7);
        }
    }
}