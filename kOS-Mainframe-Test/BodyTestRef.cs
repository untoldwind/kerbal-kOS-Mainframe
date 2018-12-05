using System;
namespace kOSMainframeTest {
    public class BodyTestRef {
        public static BodyTestRef Kerbol = new BodyTestRef("Kerbol", 1.17233279483249E+18);
        public static BodyTestRef Kerbin = new BodyTestRef("Kerbin", Kerbol, 0, 0, 13599840256, 0, 0, 0, 3.14000010490417, 3531600000000, 84159286.4796305);
        public static BodyTestRef Mun = new BodyTestRef("Mun", Kerbin, 0, 0, 12000000, 0, 0, 0, 1.70000004768372, 65138397520.7807, 2429559.11656475);
        public static BodyTestRef Minmus = new BodyTestRef("Minmus", Kerbin, 6, 0, 47000000, 78, 38, 0, 0.899999976158142, 1765800026.31247, 2247428.3879023);
        public static BodyTestRef Duna = new BodyTestRef("Duna", Kerbol, 0.06, 0.051, 20726155264, 135.5, 0, 0, 3.14000010490417, 301363211975.098, 47921949.369738);

        public readonly BodyTestRef parent;
        public readonly String name;
        public readonly OrbitTestRef orbit;
        public readonly double mu;
        public readonly double soiRadius;

        public BodyTestRef(String name, double mu) {
            this.name = name;
            this.mu = mu;
            this.parent = null;
            this.orbit = null;
            this.soiRadius = double.PositiveInfinity;
        }

        public BodyTestRef(String name,
                           BodyTestRef parent,
                           double inclination,
                           double eccentricity,
                           double semiMajorAxis,
                           double LAN,
                           double argumentOfPeriapsis,
                           double epoch,
                           double meanAnomalyAtEpoch,
                           double mu,
                           double soiRadius) {
            this.name = name;
            this.parent = parent;
            this.mu = mu;
            this.soiRadius = soiRadius;
            this.orbit = new OrbitTestRef(parent, inclination, eccentricity, semiMajorAxis, LAN, argumentOfPeriapsis, epoch, meanAnomalyAtEpoch);
        }

        public Vector3d GetPositionAtUT(double UT) {
            return orbit?.GetPositionAtUT(UT) ?? Vector3d.zero;
        }

        public Vector3d GetOrbitalVelocityAtUT(double UT) {
            return orbit?.GetOrbitalVelocityAtUT(UT) ?? Vector3d.zero;
        }
    }
}
