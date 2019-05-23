namespace kOSMainframe.Orbital {
    public static class OrbitMatch {
        /// <summary>
        /// Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        /// will be equal to the time of the first ascending node with respect to the target after the given UT.
        /// Throws an ArgumentException if o is hyperbolic and doesn't have an ascending node relative to the target.
        /// </summary>
        public static NodeParameters MatchPlanesAscending(IOrbit o, IOrbit target, double UT) {
            double burnUT = o.TimeOfAscendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal, o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return o.DeltaVToNode(burnUT, desiredHorizontalVelocity - actualHorizontalVelocity);
        }

        /// <summary>
        /// Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        /// will be equal to the time of the first descending node with respect to the target after the given UT.
        /// Throws an ArgumentException if o is hyperbolic and doesn't have a descending node relative to the target.
        /// </summary>
        public static NodeParameters MatchPlanesDescending(IOrbit o, IOrbit target, double UT) {
            double burnUT = o.TimeOfDescendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal, o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return o.DeltaVToNode(burnUT, desiredHorizontalVelocity - actualHorizontalVelocity);
        }

        /// <summary>
        /// Computes the delta-V of the burn at a given time required to zero out the difference in orbital velocities
        /// between a given orbit and a target.
        /// </summary>
        public static NodeParameters MatchVelocities(IOrbit o, double UT, Orbit target) {
            return o.DeltaVToNode(UT, target.SwappedOrbitalVelocityAtUT(UT) - o.SwappedOrbitalVelocityAtUT(UT));
        }
    }
}
