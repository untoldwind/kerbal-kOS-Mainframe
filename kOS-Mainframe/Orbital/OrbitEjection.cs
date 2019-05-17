using System;
using kOSMainframe.Numerics;

namespace kOSMainframe.Orbital {
    public static class OrbitEjection {
        /// <summary>
        /// Calculates the "ideal" ejection orbit for a given exit velocity at
        /// a given time. 
        /// To get a unique result the periapsis of the ejection orbit and a
        /// desired orbit normal has to be defined as well. As a rule of thumb: 
        /// The periapsis should be greater than the radius of the body 
        /// (and its atmosphere), but the lower the better.
        /// </summary>
        /// <param name="body">The body to eject from</param>
        /// <param name="UT">Univeral time when SOI should be left</param>
        /// <param name="peR">Desired altitude of periapsis of the ejection orbit</param>
        /// <param name="normal">Desired orbit normal (will not be an exact match most likely)</param>
        /// <param name="exitVelocity">Exit velocity relative to the body (i.e. inside the SOI)</param>
        public static IOrbit IdealEjection(IBody body, double UT, double peR, Vector3d normal, Vector3d exitVelocity) {
            // Implicitly have the specific energy of the ejection orbit
            double exitEnergy = 0.5 * exitVelocity.sqrMagnitude - body.GravParameter / body.SOIRadius;
            // Magnitude of velocity at periapsis of ejection orbit
            double peVelocity = Math.Sqrt(2 * (exitEnergy + body.GravParameter / peR));

            // Create a more or less arbitrary plane of reference where exitVelocity points to sampleX
            Vector3d sampleX = exitVelocity.normalized;
            Vector3d sampleZ = Vector3d.Exclude(sampleX, normal).normalized;
            Vector3d sampleY = Vector3d.Cross(sampleZ, sampleX).normalized;

            // Create a sample orbit in the plane perpendicular to sampleZ
            Vector3d sampleStartPos = sampleX * peR;
            Vector3d sampleStartVelocity = sampleY * peVelocity;
            IOrbit sampleOrbit = body.CreateOrbit(sampleStartPos, sampleStartVelocity, 0);
            // Now we get the true anomaly of the exit point
            double exitTA = sampleOrbit.TrueAnomalyAtRadius(body.SOIRadius) ;
            // ... the time it takes ot get from periapsis to exit point
            double dT = sampleOrbit.TimeOfTrueAnomaly(exitTA * ExtraMath.RadToDeg, 0);
            // ... the exitVelocity of the sample orbit
            Vector3d sampleExitVelocity = sampleOrbit.SwappedOrbitalVelocityAtUT(dT);

            // By choice of the reference plane neither exitVelocity nor sampleExitVelocity
            // should have an z-component. So we just have to turn everything
            // around sampleY so that sampleExitVelocity points to sampleX as well.
            double angle = -Math.Atan2(Vector3d.Dot(sampleY, sampleExitVelocity), Vector3d.Dot(sampleX, sampleExitVelocity));

            // This will now be the real starting position and velocity
            Vector3d startPos = peR * (Math.Cos(angle) * sampleX + Math.Sin(angle) * sampleY);
            Vector3d startVel = peVelocity * (Math.Cos(angle) * sampleY - Math.Sin(angle) * sampleX);

            IOrbit ejectionOrbit = body.CreateOrbit(startPos, startVel, UT - dT);

            Logging.Debug($"Ejection: exitTA={exitTA} dT={dT} exitV={exitVelocity} ejectV={ejectionOrbit.SwappedOrbitalVelocityAtUT(UT)}");
            Logging.DumpOrbit("Ejection orbit", ejectionOrbit);

            return ejectionOrbit;
        }
    }
}
