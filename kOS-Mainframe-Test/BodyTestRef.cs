using System;
using kOSMainframe.Orbital;

namespace kOSMainframeTest {
    public class BodyTestRef : IBody {
        public static BodyTestRef Kerbol = new BodyTestRef("Kerbol", 261600000, 1.17233279483249E+18);
        public static BodyTestRef Kerbin = new BodyTestRef("Kerbin", 600000, Kerbol, 0, 0, 13599840256, 0, 0, 0, 3.14000010490417, 3531600000000, 84159286.4796305);
        public static BodyTestRef Mun = new BodyTestRef("Mun", 200000, Kerbin, 0, 0, 12000000, 0, 0, 0, 1.70000004768372, 65138397520.7807, 2429559.11656475);
        public static BodyTestRef Minmus = new BodyTestRef("Minmus", 60000, Kerbin, 6, 0, 47000000, 78, 38, 0, 0.899999976158142, 1765800026.31247, 2247428.3879023);
        public static BodyTestRef Duna = new BodyTestRef("Duna", 320000, Kerbol, 0.06, 0.051, 20726155264, 135.5, 0, 0, 3.14000010490417, 301363211975.098, 47921949.369738);
        public static BodyTestRef Ike = new BodyTestRef("Ike", 130000, Duna, 0.2, 0.03, 3200000, 0, 0, 0, 1.7, 18568368573.144, 1049598.93931162);
        public static BodyTestRef Eve = new BodyTestRef("Eve", 700000, Kerbol, 2.09999990463257, 0.00999999977648258, 9832684544, 15, 0, 0, 3.14000010490417, 8171730229210.87, 85109364.7382441);
        public static BodyTestRef Gilly = new BodyTestRef("Gilly", 13000, Eve, 12, 0.550000011920929, 31500000, 80, 10, 0, 0.899999976158142, 8289449.81471635, 126123.271704568);
        public static BodyTestRef Jool = new BodyTestRef("Jool", 6000000, Kerbol, 1.30400002002716, 0.0500000007450581, 68773560320, 52, 0, 0, 0.100000001490116, 282528004209995, 2455985185.42347);
        public static BodyTestRef Tylo = new BodyTestRef("Tylo", 600000, Jool, 0.025000000372529, 0, 68500000, 0, 0, 0, 3.14000010490417, 2825280042099.95, 10856518.3683586);
        public static BodyTestRef Laythe = new BodyTestRef("Laythe", 500000, Jool, 0, 0, 27184000, 0, 0, 0, 3.14000010490417, 1962000029236.08, 3723645.81113302);
        public static BodyTestRef Bop = new BodyTestRef("Bop", 65000, Jool, 15, 0.234999999403954, 128500000, 10, 25, 0, 0.899999976158142, 2486834944.41491, 1221060.86284253);
        public static BodyTestRef Pol = new BodyTestRef("Pol", 44000, Jool, 4.25, 0.17085, 179890000, 2, 15, 0, 0.899999976158142, 721702080, 1042138.89230178);
        public static BodyTestRef Vall = new BodyTestRef("Vall", 300000, Jool, 0, 0, 43152000, 0, 0, 0, 0.899999976158142, 207481499473.751, 2406401.44479404);

        public readonly BodyTestRef parent;
        public readonly string name;
        public readonly OrbitTestRef orbit;
        public readonly double mu;
        public readonly double soiRadius;
        public readonly double radius;

        public BodyTestRef(string name, double radius, double mu) {
            this.name = name;
            this.mu = mu;
            this.parent = null;
            this.orbit = null;
            this.soiRadius = double.PositiveInfinity;
            this.radius = radius;
        }

        public BodyTestRef(string name,
            double radius,
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
            this.radius = radius;
        }

        public string Name => name;

        public double GravParameter => mu;

        public double SOIRadius => soiRadius;

        public IOrbit Orbit => orbit;

        public Vector3d GetPositionAtUT(double UT) {
            return orbit?.GetRelativePositionAtUT(UT) ?? Vector3d.zero;
        }

        public Vector3d GetOrbitalVelocityAtUT(double UT) {
            return orbit?.GetOrbitalVelocityAtUT(UT) ?? Vector3d.zero;
        }

        public IOrbit CreateOrbit(Vector3d relPos, Vector3d vel, double UT) {
            return new OrbitTestRef(this, relPos.SwapYZ(), vel.SwapYZ(), UT);
        }
    }
}
