using System;
using NUnit.Framework;
using kOSMainframe.Orbital;

namespace kOSMainframeTest {
    [TestFixture]
    public class OrbitChangeTest {
        [Test]
        public void TestCircularize() {
            IOrbit orbit = new OrbitTestRef(BodyTestRef.Kerbin, 20.0, 0.4, 1200000, 34, 0.0, 0.0, 0.0);
            double UT = 200;
            NodeParameters node = OrbitChange.Circularize(orbit, UT);
            IOrbit newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);
            double expectedRadius = orbit.Radius(UT);

            Assert.AreEqual(0.0, node.normal, 1e-7);
            Assert.AreEqual(0.0, newOrbit.Eccentricity, 1e-7);
            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(expectedRadius, newOrbit.ApR, 1e-7);
            Assert.AreEqual(expectedRadius, newOrbit.PeR, 1e-7);

            UT = orbit.NextApoapsisTime(UT);
            node = OrbitChange.Circularize(orbit, UT);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);
            expectedRadius = orbit.Radius(UT);

            Assert.AreEqual(0.0, node.normal, 1e-7);
            Assert.AreEqual(0.0, node.radialOut, 1e-7);
            Assert.AreEqual(0.0, newOrbit.Eccentricity, 1e-7);
            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(expectedRadius, newOrbit.ApR, 1e-7);
            Assert.AreEqual(expectedRadius, newOrbit.PeR, 1e-7);
        }
    }
}
