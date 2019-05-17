using NUnit.Framework;
using kOSMainframe.Orbital;
using System;

namespace kOSMainframeTest {
    [TestFixture]
    public class OrbitEjectionTest {
        [Test]
        public void TestInterplanetaryEjection() {
            var exitNode = OrbitIntercept.BiImpulsiveAnnealed(BodyTestRef.Kerbin.Orbit, BodyTestRef.Duna.Orbit, 0);
            var ejectionOrbit = OrbitEjection.IdealEjection(BodyTestRef.Kerbin, exitNode.time, 80000 + BodyTestRef.Kerbin.radius, BodyTestRef.Kerbin.orbit.SwappedOrbitNormal, exitNode.deltaV);
            var ejectionP = ejectionOrbit.SwappedRelativePositionAtUT(exitNode.time);
            var ejectionV = ejectionOrbit.SwappedOrbitalVelocityAtUT(exitNode.time);

            Assert.True(Math.Abs(ejectionOrbit.Inclination) < 5, "Inclination");
            Assert.AreEqual(exitNode.deltaV.x, ejectionV.x, 1e-5);
            Assert.AreEqual(exitNode.deltaV.y, ejectionV.y, 1e-5);
            Assert.AreEqual(exitNode.deltaV.z, ejectionV.z, 1e-5);
            Assert.AreEqual(ejectionP.magnitude, BodyTestRef.Kerbin.SOIRadius, 1e-5);
        }
    }
}
