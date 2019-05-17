using NUnit.Framework;
using kOSMainframe.Orbital;

namespace kOSMainframeTest {
    [TestFixture]
    public class OrbitEjectionTest {
        [Test]
        public void TestInterplanetaryEjection() {
            var exitNode = OrbitIntercept.BiImpulsiveAnnealed(BodyTestRef.Kerbin.Orbit, BodyTestRef.Duna.Orbit, 0);
            OrbitEjection.IdealEjection(BodyTestRef.Kerbin, exitNode.time, 80000 + BodyTestRef.Kerbin.radius, exitNode.deltaV);
        }
    }
}
