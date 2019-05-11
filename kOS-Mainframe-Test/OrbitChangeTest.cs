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

        [Test]
        public void TestEllipticize() {
            IOrbit orbit = new OrbitTestRef(BodyTestRef.Kerbin, 20.0, 0.4, 1200000, 34, 0.0, 0.0, 0.0);
            double UT = 200;
            NodeParameters node = OrbitChange.Ellipticize(orbit, UT, 1500000, 1500000);
            IOrbit newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);
            double expectedPeR = orbit.Radius(UT);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(expectedPeR, newOrbit.PeR, 2);
            Assert.AreEqual(1500000, newOrbit.ApR, 1e-7);

            UT = orbit.NextApoapsisTime(UT);
            node = OrbitChange.Ellipticize(orbit, UT, 900000, 1200000);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);
            double expectedApR = orbit.Radius(UT);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(900000, newOrbit.PeR, 1e-7);
            Assert.AreEqual(expectedApR, newOrbit.ApR, 2);

        }

        [Test]
        public void TestChangePeriapsis() {
            IOrbit orbit = new OrbitTestRef(BodyTestRef.Kerbin, 20.0, 0.4, 1200000, 34, 0.0, 0.0, 0.0);
            double UT = orbit.NextApoapsisTime(200);
            NodeParameters node = OrbitChange.ChangePeriapsis(orbit, UT, 500000);
            IOrbit newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(500000, newOrbit.PeR, 3);
            Assert.AreEqual(1680000, newOrbit.ApR, 1e-7);

            node = OrbitChange.ChangePeriapsis(orbit, UT, 900000);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(900000, newOrbit.PeR, 8);
            Assert.AreEqual(1680000, newOrbit.ApR, 1e-7);

            node = OrbitChange.ChangePeriapsis(orbit, UT, 1900000);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(1680000, newOrbit.PeR, 2);
            Assert.AreEqual(1680000, newOrbit.ApR, 8);
        }

        [Test]
        public void TestChangeApoapsis() {
            IOrbit orbit = new OrbitTestRef(BodyTestRef.Kerbin, 20.0, 0.4, 1200000, 34, 0.0, 0.0, 0.0);
            double UT = orbit.NextPeriapsisTime(200);
            NodeParameters node = OrbitChange.ChangeApoapsis(orbit, UT, 500000);
            IOrbit newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(720000, newOrbit.PeR, 1e-7);
            Assert.AreEqual(720000, newOrbit.ApR, 5);

            node = OrbitChange.ChangeApoapsis(orbit, UT, 900000);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(720000, newOrbit.PeR, 1e-7);
            Assert.AreEqual(900000, newOrbit.ApR, 2);

            node = OrbitChange.ChangeApoapsis(orbit, UT, 1900000);
            newOrbit = orbit.PerturbedOrbit(UT, node.deltaV);

            Assert.AreEqual(20.0, newOrbit.Inclination, 1e-7);
            Assert.AreEqual(34.0, newOrbit.LAN, 1e-7);
            Assert.AreEqual(720000, newOrbit.PeR, 1e-7);
            Assert.AreEqual(1900000, newOrbit.ApR, 20);
        }
    }
}
