using NUnit.Framework;
using kOSMainframe.Orbital;

namespace kOSMainframeTest {
    [TestFixture]
    public class OrbitMatchTest {
        [Test]
        public void TestMatchAtAscending() {
            var a = new OrbitTestRef(BodyTestRef.Kerbin, 4, 0.5, 600000, 30, 40, 0, 10);
            var b = new OrbitTestRef(BodyTestRef.Kerbin, 34, 0.8, 800000, 35, 46, 0, 30);
            var node = OrbitMatch.MatchPlanesAscending(a, b, 20000);
            var result = a.PerturbedOrbit(node.time, node.deltaV);

            Assert.True(node.time > 20000, "Node in future");
            Assert.AreEqual(b.inclination, result.Inclination, 1e-5);
            Assert.AreEqual(Vector3d.Angle(b.SwappedOrbitNormal, result.SwappedOrbitNormal), 0, 1e-5);
        }

        [Test]
        public void TestMatchAtDecending() {
            var a = new OrbitTestRef(BodyTestRef.Kerbin, 4, 0.5, 600000, 30, 40, 0, 10);
            var b = new OrbitTestRef(BodyTestRef.Kerbin, 34, 0.8, 800000, 35, 46, 0, 30);
            var node = OrbitMatch.MatchPlanesDescending(a, b, 20000);
            var result = a.PerturbedOrbit(node.time, node.deltaV);

            Assert.True(node.time > 20000, "Node in future");
            Assert.AreEqual(b.inclination, result.Inclination, 1e-5);
            Assert.AreEqual(Vector3d.Angle(b.SwappedOrbitNormal, result.SwappedOrbitNormal), 0, 1e-5);
        }
    }
}
