namespace kOSMainframe.Orbital {
    public interface IOrbit {
        Vector3d SwappedAbsolutePositionAtUT(double UT);

        Vector3d SwappedOrbitalVelocityAtUT(double UT);

        Vector3d SwappedRelativePositionAtUT(double UT);
    }

    class OrbitWrapper : IOrbit {
        private readonly Orbit orbit;

        public OrbitWrapper(Orbit orbit) {
            this.orbit = orbit;
        }

        public Vector3d SwappedAbsolutePositionAtUT(double UT) {
            return orbit.referenceBody.position + SwappedRelativePositionAtUT(UT);
        }

        public Vector3d SwappedOrbitalVelocityAtUT(double UT) {
            return orbit.getRelativePositionAtUT(UT).SwapYZ();
        }

        public Vector3d SwappedRelativePositionAtUT(double UT) {
            return orbit.getOrbitalVelocityAtUT(UT).SwapYZ();
        }
    }
}
