namespace kOSMainframe.Orbital {
    public interface IOrbit {
        IBody referenceBody {
            get;
        }

        Vector3d SwappedAbsolutePositionAtUT(double UT);

        Vector3d SwappedOrbitalVelocityAtUT(double UT);

        Vector3d SwappedRelativePositionAtUT(double UT);

        Vector3d Prograde(double UT);

        Vector3d NormalPlus(double UT);

        Vector3d RadialPlus(double UT);

        Vector3d Up(double UT);

        double Radius(double UT);

        Vector3d Horizontal(double UT);

        IOrbit PerturbedOrbit(double UT, Vector3d dV);

        NodeParameters DeltaVToNode(double UT, Vector3d dV);
    }

    class OrbitWrapper : IOrbit {
        private readonly Orbit orbit;

        public OrbitWrapper(Orbit orbit) {
            this.orbit = orbit;
        }

        public IBody referenceBody => orbit.referenceBody.wrap();

        public Vector3d SwappedAbsolutePositionAtUT(double UT) {
            return orbit.referenceBody.position + SwappedRelativePositionAtUT(UT);
        }

        public Vector3d SwappedOrbitalVelocityAtUT(double UT) {
            return orbit.getOrbitalVelocityAtUT(UT).SwapYZ();
        }

        public Vector3d SwappedRelativePositionAtUT(double UT) {
            return orbit.getRelativePositionAtUT(UT).SwapYZ();
        }

        public Vector3d Prograde(double UT) {
            return SwappedOrbitalVelocityAtUT(UT).normalized;
        }

        public Vector3d NormalPlus(double UT) {
            return -orbit.GetOrbitNormal().normalized.SwapYZ();
        }

        public Vector3d RadialPlus(double UT) {
            return Vector3d.Exclude(Prograde(UT), Up(UT)).normalized;
        }

        public Vector3d Up(double UT) {
            return SwappedRelativePositionAtUT(UT).normalized;
        }

        public double Radius(double UT) {
            return SwappedRelativePositionAtUT(UT).magnitude;
        }

        public Vector3d Horizontal(double UT) {
            return Vector3d.Exclude(Up(UT), Prograde(UT)).normalized;
        }

        public IOrbit PerturbedOrbit(double UT, Vector3d dV) {
            return Helper.OrbitFromStateVectors(SwappedAbsolutePositionAtUT(UT), SwappedOrbitalVelocityAtUT(UT) + dV, orbit.referenceBody, UT).wrap();
        }

        public NodeParameters DeltaVToNode(double UT, Vector3d dV) {
            return new NodeParameters(UT,
                                      Vector3d.Dot(RadialPlus(UT), dV),
                                      Vector3d.Dot(-NormalPlus(UT), dV),
                                      Vector3d.Dot(Prograde(UT), dV),
                                      dV);
        }
    }
}
