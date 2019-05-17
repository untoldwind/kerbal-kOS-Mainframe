using System;
using kOSMainframe.Numerics;

namespace kOSMainframe.Orbital {
    /// <summary>
    /// Common interface of an orbit (in game + unit tests).
    /// </summary>
    public interface IOrbit {
        IBody ReferenceBody {
            get;
        }

        /// <summary>
        /// Radius of the apoapsis.
        /// </summary>
        double ApR {
            get;
        }

        /// <summary>
        /// Radius of the periapsis.
        /// </summary>
        double PeR {
            get;
        }

        /// <summary>
        /// Gets the semi major axis.
        /// </summary>
        double SemiMajorAxis {
            get;
        }

        /// <summary>
        /// Inclincation of the orbit (in deg).
        /// </summary>
        double Inclination {
            get;
        }

        /// <summary>
        /// Eccentricity of the orbit.
        /// </summary>
        double Eccentricity {
            get;
        }

        /// <summary>
        /// Longitude of ascending node (in deg).
        /// </summary>
        double LAN {
            get;
        }

        /// <summary>
        /// Mean motion is rate of increase of the mean anomaly
        /// </summary>
        double MeanMotion {
            get;
        }

        /// <summary>
        /// Orbital period.
        /// </summary>
        double Period {
            get;
        }

        Orbit.PatchTransitionType PatchEndTransition {
            get;
        }

        double PatchEndUT {
            get;
        }

        Vector3d SwappedOrbitNormal {
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

        /// <summary>
        /// Returns a new Orbit object that represents the result of applying a given dV to o at UT
        /// </summary>
        IOrbit PerturbedOrbit(double UT, Vector3d dV);

        /// <summary>
        /// The mean anomaly of the orbit.
        /// For elliptical orbits, the value return is always between 0 and 2pi
        /// For hyperbolic orbits, the value can be any number.
        /// </summary>
        double MeanAnomalyAtUT(double UT);

        /// <summary>
        /// The next time at which the orbiting object will reach the given mean anomaly.
        /// For elliptical orbits, this will be a time between UT and UT + o.period
        /// For hyperbolic orbits, this can be any time, including a time in the past, if
        /// the given mean anomaly occurred in the past
        /// </summary>
        double UTAtMeanAnomaly(double meanAnomaly, double UT);

        /// <summary>
        /// Converts an eccentric anomaly into a mean anomaly.
        /// For an elliptical orbit, the returned value is between 0 and 2pi
        /// For a hyperbolic orbit, the returned value is any number
        /// </summary>
        double GetMeanAnomalyAtEccentricAnomaly(double E);

        /// <summary>
        /// Converts a true anomaly into an eccentric anomaly.
        /// For elliptical orbits this returns a value between 0 and 2pi
        /// For hyperbolic orbits the returned value can be any number.
        /// NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        /// past the true anomaly of the asymptote) then an ArgumentException is thrown
        /// </summary>
        double GetEccentricAnomalyAtTrueAnomaly(double trueAnomaly);

        /// <summary>
        /// Next time of a certain true anomly.
        /// NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
        /// large enough that it never attains the given true anomaly.
        /// </summary>
        double TimeOfTrueAnomaly(double trueAnomaly, double UT);

        /// <summary>
        /// The next time at which the orbiting object will be at periapsis.
        /// For elliptical orbits, this will be between UT and UT + Period.
        /// For hyperbolic orbits, this can be any time, including a time in the past,
        /// if the periapsis is in the past.
        /// </summary>
        double NextPeriapsisTime(double UT);

        /// <summary>
        /// Returns the next time at which the orbiting object will be at apoapsis.
        /// For elliptical orbits, this is a time between UT and UT + period.
        /// For hyperbolic orbits, this throws an ArgumentException.
        /// </summary>
        double NextApoapsisTime(double UT);

        /// <summary>
        /// Finds the next time at which the orbiting object will achieve a given radius
        /// from the center of the primary.
        /// If the given radius is impossible for this orbit, an ArgumentException is thrown.
        /// For elliptical orbits this will be a time between UT and UT + period
        /// For hyperbolic orbits this can be any time. If the given radius will be achieved
        /// in the future then the next time at which that radius will be achieved will be returned.
        /// If the given radius was only achieved in the past, then there are no guarantees
        /// about which of the two times in the past will be returned.
        /// </summary>
        double NextTimeOfRadius(double UT, double radius);

        /// <summary>
        /// Computes the period of the phase angle between orbiting objects a and b.
        /// This only really makes sense for approximately circular orbits in similar planes.
        /// For noncircular orbits the time variation of the phase angle is only "quasiperiodic"
        /// and for high eccentricities and/or large relative inclinations, the relative motion is
        /// not really periodic at all.
        /// </summary>
        double SynodicPeriod(IOrbit other);


        NodeParameters DeltaVToNode(double UT, Vector3d dV);
    }

    class OrbitWrapper : IOrbit {
        private readonly Orbit orbit;

        public OrbitWrapper(Orbit orbit) {
            this.orbit = orbit;
        }

        public double ApR => orbit.ApR;

        public double PeR => orbit.PeR;

        public double SemiMajorAxis => orbit.semiMajorAxis;

        public double Inclination => orbit.inclination;

        public double Eccentricity => orbit.eccentricity;

        public double LAN => orbit.LAN;

        public double Period => orbit.period;

        public IBody ReferenceBody => orbit.referenceBody.wrap();

        public double MeanMotion {
            get {
                if (orbit.eccentricity > 1) {
                    return Math.Sqrt(orbit.referenceBody.gravParameter / Math.Abs(orbit.semiMajorAxis * orbit.semiMajorAxis * orbit.semiMajorAxis));
                } else {
                    // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
                    // This simpler formula should be foolproof for elliptical orbits:
                    return 2 * Math.PI / orbit.period;
                }
            }
        }

        public Vector3d SwappedOrbitNormal => -orbit.GetOrbitNormal().normalized.SwapYZ();

        public Orbit.PatchTransitionType PatchEndTransition => orbit.patchEndTransition;

        public double PatchEndUT => orbit.EndUT;

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

        public double UTAtMeanAnomaly(double meanAnomaly, double UT) {
            double currentMeanAnomaly = MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (orbit.eccentricity < 1) meanDifference = ExtraMath.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / MeanMotion;
        }

        public double GetMeanAnomalyAtEccentricAnomaly(double E) {
            double e = orbit.eccentricity;
            if (e < 1) { //elliptical orbits
                return ExtraMath.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            } else { //hyperbolic orbits
                return (e * Math.Sinh(E)) - E;
            }
        }

        public double GetEccentricAnomalyAtTrueAnomaly(double trueAnomaly) {
            double e = orbit.eccentricity;
            trueAnomaly = ExtraMath.ClampDegrees360(trueAnomaly);
            trueAnomaly = trueAnomaly * (UtilMath.Deg2Rad);

            if (e < 1) { //elliptical orbits
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI) sinE *= -1;

                return ExtraMath.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            } else { //hyperbolic orbits
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1) throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + orbit.eccentricity);

                double E = ExtraMath.Acosh(coshE);
                if (trueAnomaly > Math.PI) E *= -1;

                return E;
            }
        }

        public double TimeOfTrueAnomaly(double trueAnomaly, double UT) {
            return UTAtMeanAnomaly(GetMeanAnomalyAtEccentricAnomaly(GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
        }

        public double MeanAnomalyAtUT( double UT) {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (orbit.ObTAtEpoch + (UT - orbit.epoch)) * MeanMotion;
            if (orbit.eccentricity < 1) ret = ExtraMath.ClampRadiansTwoPi(ret);
            return ret;
        }

        public double NextPeriapsisTime(double UT) {
            if (orbit.eccentricity < 1) {
                return TimeOfTrueAnomaly(0, UT);
            } else {
                return UT - MeanAnomalyAtUT(UT) / MeanMotion;
            }
        }

        public double NextApoapsisTime(double UT) {
            if (orbit.eccentricity < 1) {
                return TimeOfTrueAnomaly(180, UT);
            } else {
                throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
            }
        }

        public double SynodicPeriod(IOrbit other) {
            int sign = (Vector3d.Dot(SwappedOrbitNormal, other.SwappedOrbitNormal) > 0 ? 1 : -1); //detect relative retrograde motion
            return Math.Abs(1.0 / (1.0 / Period - sign * 1.0 / other.Period)); //period after which the phase angle repeats
        }

        public double NextTimeOfRadius(double UT, double radius) {
            if (radius < orbit.PeR || (orbit.eccentricity < 1 && radius > orbit.ApR)) throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: PeR = " + orbit.PeR + " and ApR = " + orbit.ApR);

            double trueAnomaly1 = UtilMath.Rad2Deg * orbit.TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 360 - trueAnomaly1;
            double time1 = orbit.TimeOfTrueAnomaly(trueAnomaly1, UT);
            double time2 = orbit.TimeOfTrueAnomaly(trueAnomaly2, UT);
            if (time2 < time1 && time2 > UT) return time2;
            else return time1;
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
