using System;
namespace kOSMainframe.Orbital {
    public static class OrbitEjection {
        /// <summary>
        /// Calculates the next best (in terms of DeltaV) opportunity to leave
        /// the SOI at a given velocity.
        /// </summary>
        /// <param name="o">Current orbit</param>
        /// <param name="UT">Univeral time when SOI should be left</param>
        /// <param name="exitVelocity">Exit velocity (relative to current body)</param>
        public static void EjectionOrbit(IOrbit o, double UT, Vector3d exitVelocity) {
            // Implicitly have the specific energy of the ejection orbit
            double exitEnergy = 0.5 * exitVelocity.sqrMagnitude - o.ReferenceBody.GravParameter / o.ReferenceBody.SOIRadius;
            double minUT, maxUT;

            if (o.Eccentricity < 1.0) {
                // Assuming that we have some time to guess and neglicting the time
                // it will take to reach the SOI radius
                minUT = UT;
                maxUT = UT + o.Period;
            } else {
                // Already on an exit orbit, so we have to hurry up to correct it
                minUT = Planetarium.GetUniversalTime();
                maxUT = o.NextTimeOfRadius(minUT, o.ReferenceBody.SOIRadius);
            }
        }
    }
}
