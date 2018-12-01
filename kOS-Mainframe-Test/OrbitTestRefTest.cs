using System;
using NUnit.Framework;

namespace kOSMainframeTest {
    [TestFixture]
    public class OrbitTestRefTest {
        [Test]
        public void TestShipOrbit() {
            OrbitTestRef orbit = new OrbitTestRef(BodyTestRef.Kerbin, 23.0000000004889, 0.499999999787874, 1999999.99888609, 12.0000000035085, 45.000000043628, 2248030.00958346, -1.76866346012346);

            AssertEqual(orbit.FrameX, new Vector3d(0.556326074237772, 0.78368736854604, 0.276288630791373), "Orbit FrameX");
            AssertEqual(orbit.FrameY, new Vector3d(-0.826983528498581, 0.489655834561346, 0.276288630370612), "Orbit FrameY");
            AssertEqual(orbit.FrameZ, new Vector3d(0.0812375696043732, -0.382192715866505, 0.920504853449106), "Orbit FrameZ");
            Assert.AreEqual(0.000664417038265578, orbit.meanMotion, 1e-7, "Orbit mean motion");
            Assert.AreEqual(-2.55587262120358, orbit.GetTrueAnomalyAtUT(2248030.02958346), 1e-7, "Orbit true anomaly");
            TestContext.Out.WriteLine(orbit.GetPositionAtUT(2248030.02958346));
            TestContext.Out.WriteLine(orbit.GetOrbitalVelocityAtUT(2248030.02958346));
            AssertEqual(orbit.GetPositionAtUT(2248030.02958346), new Vector3d(-16555.4669097162, -2375291.03194007, -984757.441721877), "Orbit Pos");
            AssertEqual(orbit.GetOrbitalVelocityAtUT(2248030.02958346), new Vector3d(894.837941056022, 414.309011750899, 93.0483164395469), "Orbit Vel");
        }

        [Test]
        public void TestKerbinOrbit() {
            AssertEqual(BodyTestRef.Kerbin.orbit.GetPositionAtUT(2248727.10958411), new Vector3d(-505812409.43674, -13590430780.3387, 0), "Orbit Pos");
            AssertEqual(BodyTestRef.Kerbin.orbit.GetOrbitalVelocityAtUT(2248727.10958411), new Vector3d(9278.07693080402, -345.314031847963, 0), "Orbit Vel");
        }

        private void AssertEqual(Vector3d expected, Vector3d actual, String message) {
            Assert.AreEqual(expected.x, actual.x, 1e-4, message + ".x");
            Assert.AreEqual(expected.y, actual.y, 1e-4, message + ".y");
            Assert.AreEqual(expected.z, actual.z, 1e-4, message + ".z");
        }
    }
}

