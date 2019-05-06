namespace kOSMainframe.Orbital {
    public interface IOrbit {
        Vector3d getOrbitalVelocityAtUT(double UT);

        Vector3d getRelativePositionAtUT(double UT);

        Vector3d GetOrbitNormal();

        IBody referenceBody { get; }

        double ApR { get; }
        double PeR { get; }
        double eccentricity { get; }
        double semiMajorAxis { get; }
        double semiMinorAxis { get; }
        double period { get; }
        double epoch { get; }
        double ObTAtEpoch { get; }
        double LAN { get; }
        double argumentOfPeriapsis { get; }
        double inclination { get; }
        double meanAnomalyAtEpoch { get; }

        double GetOrbitalStateVectorsAtUT(double UT, out Vector3d pos, out Vector3d vel);
        double TrueAnomalyAtRadius(double R);
        double RadiusAtTrueAnomaly(double tA);

        void UpdateFromStateVectors(Vector3d pos, Vector3d vel, double UT);
    }

    public class OrbitWrapper : IOrbit {
        private readonly Orbit orbit;

        public double ApR => orbit.ApR;

        public double PeR => orbit.PeR;

        public IBody referenceBody => new BodyWrapper(orbit.referenceBody);

        public double eccentricity => orbit.eccentricity;

        public double semiMajorAxis => orbit.semiMajorAxis;
        public double semiMinorAxis => orbit.semiMinorAxis;

        public double period => orbit.period;

        public double epoch => orbit.epoch;

        public double ObTAtEpoch => orbit.ObTAtEpoch;

        public double LAN => orbit.LAN;

        public double argumentOfPeriapsis => orbit.argumentOfPeriapsis;

        public double inclination => orbit.inclination;

        public double meanAnomalyAtEpoch => orbit.meanAnomalyAtEpoch;

        public OrbitWrapper(Orbit orbit) {
            this.orbit = orbit;
        }

        public Vector3d getOrbitalVelocityAtUT(double UT) {
            return orbit.getOrbitalVelocityAtUT(UT);
        }

        public Vector3d getRelativePositionAtUT(double UT) {
            return orbit.getRelativePositionAtT(UT);
        }

        public Vector3d GetOrbitNormal() {
            return orbit.GetOrbitNormal();
        }

        public double GetOrbitalStateVectorsAtUT(double UT, out Vector3d pos, out Vector3d vel) {
            return orbit.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
        }

        public void UpdateFromStateVectors(Vector3d pos, Vector3d vel, double UT) {
            orbit.UpdateFromStateVectors(pos, vel, orbit.referenceBody, UT);
        }

        public double TrueAnomalyAtRadius(double R) {
            return orbit.TrueAnomalyAtRadius(R);
        }

        public double RadiusAtTrueAnomaly(double tA) {
            return orbit.RadiusAtTrueAnomaly(tA);
        }
    }
}
