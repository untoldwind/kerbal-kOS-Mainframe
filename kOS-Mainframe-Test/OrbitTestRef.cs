using System;
using kOSMainframe.Orbital;
using kOSMainframe.Numerics;
using UnityEngine;

namespace kOSMainframeTest {
    public class OrbitTestRef : IOrbit {
        public const double DegToRad = Math.PI / 180.0;
        public const double RadToDeg = 180.0 / Math.PI;

        public BodyTestRef body;
        public double inclination;
        public double LAN;
        public double eccentricity;
        public double semiMajorAxis;
        public double argumentOfPeriapsis;
        public double meanAnomalyAtEpoch;
        public double epoch;
        public double meanMotion;
        public double orbitTimeAtEpoch;
        public double period;
        public Vector3d FrameX;
        public Vector3d FrameY;
        public Vector3d FrameZ;
        public Vector3d ascendingNode;
        public Vector3d eccVec;

        public double PeR => (1.0 - eccentricity) * semiMajorAxis;

        public double ApR => (1.0 + eccentricity) * semiMajorAxis;

        public double SemiMajorAxis => semiMajorAxis;

        public double Inclination => inclination;

        public double Eccentricity => eccentricity;

        double IOrbit.LAN => LAN;

        public IBody ReferenceBody => body;

        public double MeanMotion => meanMotion;

        public double Epoch => epoch;

        public double ArgumentOfPeriapsis => argumentOfPeriapsis;

        public double MeanAnomalyAtEpoch => meanAnomalyAtEpoch;

        public double Period => period;

        public Vector3d SwappedOrbitNormal => -FrameZ.normalized.SwapYZ();

        // This is pretty much fake ATM
        public Orbit.PatchTransitionType PatchEndTransition => Orbit.PatchTransitionType.INITIAL;

        public double PatchEndUT => 0;

        public OrbitTestRef(BodyTestRef body,
                            double inclination,
                            double eccentricity,
                            double semiMajorAxis,
                            double LAN,
                            double argumentOfPeriapsis,
                            double epoch,
                            double meanAnomalyAtEpoch) {
            this.body = body;
            this.inclination = inclination;
            this.eccentricity = eccentricity;
            this.semiMajorAxis = semiMajorAxis;
            this.LAN = LAN;
            this.argumentOfPeriapsis = argumentOfPeriapsis;
            this.meanAnomalyAtEpoch = meanAnomalyAtEpoch;
            this.epoch = epoch;

            double anX = Math.Cos(LAN * DegToRad);
            double anY = Math.Sin(LAN * DegToRad);
            double incX = Math.Cos(inclination * DegToRad);
            double incY = Math.Sin(inclination * DegToRad);
            double peX = Math.Cos(argumentOfPeriapsis * DegToRad);
            double peY = Math.Sin(argumentOfPeriapsis * DegToRad);
            FrameX = new Vector3d(anX * peX - anY * incX * peY, anY * peX + anX * incX * peY, incY * peY);
            FrameY = new Vector3d(-anX * peY - anY * incX * peX, -anY * peY + anX * incX * peX, incY * peX);
            FrameZ = new Vector3d(anY * incY, -anX * incY, incX);

            ascendingNode = Vector3d.Cross(Vector3d.forward, FrameZ);
            if (ascendingNode.sqrMagnitude == 0.0) {
                ascendingNode = Vector3d.right;
            }
            eccVec = FrameX * eccentricity;
            meanMotion = GetMeanMotion();
            orbitTimeAtEpoch = meanAnomalyAtEpoch / meanMotion;
            if (eccentricity < 1.0) {
                period = 2 * Math.PI / meanMotion;
            } else {
                period = double.PositiveInfinity;
            }
        }

        public OrbitTestRef(BodyTestRef body, Vector3d position, Vector3d velocity, double UT) {
            this.body = body;

            Vector3d H = Vector3d.Cross(position, velocity);
            double orbitalEnergy = velocity.sqrMagnitude / 2.0 - body.mu / position.magnitude;

            if (H.sqrMagnitude == 0.0) {
                ascendingNode = Vector3d.Cross(position, Vector3d.forward);
            } else {
                ascendingNode = Vector3d.Cross(Vector3d.forward, H);
            }
            if (ascendingNode.sqrMagnitude == 0.0) {
                ascendingNode = Vector3d.right;
            }
            LAN = RadToDeg * Math.Atan2(ascendingNode.y, ascendingNode.x);
            eccVec = Vector3d.Cross(velocity, H) / body.mu - position / position.magnitude;
            eccentricity = eccVec.magnitude;
            if (eccentricity < 1.0) {
                semiMajorAxis = -body.mu / (2.0 * orbitalEnergy);
            } else {
                semiMajorAxis = -H.sqrMagnitude / body.mu / (eccVec.sqrMagnitude - 1.0);
            }
            if (eccentricity == 0.0) {
                FrameX = ascendingNode.normalized;
                argumentOfPeriapsis = 0.0;
            } else {
                FrameX = eccVec.normalized;
                argumentOfPeriapsis = RadToDeg * Math.Acos(Vector3d.Dot(ascendingNode, FrameX) / ascendingNode.magnitude);
                if (FrameX.z < 0.0) {
                    argumentOfPeriapsis = 360.0 - argumentOfPeriapsis;
                }
            }
            if (H.sqrMagnitude == 0.0) {
                FrameY = ascendingNode.normalized;
                FrameZ = Vector3d.Cross(FrameX, FrameY);
            } else {
                FrameZ = H.normalized;
                FrameY = Vector3d.Cross(FrameZ, FrameX);
            }
            inclination = RadToDeg * Math.Acos(FrameZ.z);
            epoch = UT;
            double trueAnomaly = Math.Atan2(Vector3d.Dot(FrameY, position), Vector3d.Dot(FrameX, position));
            double eccentricAnomaly = GetEccentricAnomalyForTrue(trueAnomaly);
            double meanAnomaly = GetMeanAnomaly(eccentricAnomaly);
            meanAnomalyAtEpoch = meanAnomaly;
            meanMotion = GetMeanMotion();
            orbitTimeAtEpoch = meanAnomalyAtEpoch / meanMotion;
            if (eccentricity < 1.0) {
                period = 2 * Math.PI / meanMotion;
            } else {
                period = double.PositiveInfinity;
            }
        }

        public double GetEccentricAnomalyForTrue(double trueAnomaly) {
            double x = Math.Cos(trueAnomaly / 2.0);
            double y = Math.Sin(trueAnomaly / 2.0);
            if (eccentricity < 1.0) {
                return 2.0 * Math.Atan2(Math.Sqrt(1.0 - eccentricity) * y, Math.Sqrt(1.0 + eccentricity) * x);
            }
            double r = Math.Sqrt((eccentricity - 1.0) / (eccentricity + 1.0)) * y / x;
            if (r >= 1.0) {
                return double.PositiveInfinity;
            }
            if (r <= -1.0) {
                return double.NegativeInfinity;
            }
            return Math.Log((1.0 + r) / (1.0 - r));
        }

        public double GetMeanAnomaly(double eccentricAnomaly) {
            if (eccentricity < 1.0) {
                return eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly);
            }
            if (double.IsInfinity(eccentricAnomaly)) {
                return eccentricAnomaly;
            }
            return eccentricity * Math.Sinh(eccentricAnomaly) - eccentricAnomaly;
        }

        public double GetMeanMotion() {
            return Math.Sqrt(body.mu / Math.Abs(semiMajorAxis * semiMajorAxis * semiMajorAxis));
        }

        public double GetTrueAnomalyAtUT(double UT) {
            return GetTrueAnomalyAtOrbitTime(GetOrbitTimeAtUT(UT));
        }

        public double GetOrbitTimeAtUT(double UT) {
            double orbitTime;
            if (eccentricity < 1.0) {
                orbitTime = (UT - epoch + orbitTimeAtEpoch) % period;
                if (orbitTime > period / 2.0) {
                    orbitTime -= period;
                }
            } else {
                orbitTime = orbitTimeAtEpoch + (UT - epoch);
            }
            return orbitTime;
        }

        public double GetOrbitTimeAtMeanAnomaly(double meanAnomaly) {
            return meanAnomaly / meanMotion;
        }


        public Vector3d GetRelativePositionAtUT(double UT) {
            return GetPositionAtOrbitTime(GetOrbitTimeAtUT(UT));
        }

        public Vector3d GetPositionAtOrbitTime(double orbitTime) {
            return GetPositionForTrueAnomaly(GetTrueAnomalyAtOrbitTime(orbitTime));
        }

        public Vector3d GetPositionForTrueAnomaly(double trueAnomaly) {
            double x = Math.Cos(trueAnomaly);
            double y = Math.Sin(trueAnomaly);
            double r = semiMajorAxis * (1.0 - eccentricity * eccentricity) / (1.0 + eccentricity * x);
            return r * (FrameX * x + FrameY * y);
        }

        public Vector3d GetOrbitalVelocityAtUT(double UT) {
            return GetOrbitalVelocityAtOrbitTime(GetOrbitTimeAtUT(UT));
        }

        public Vector3d GetOrbitalVelocityAtOrbitTime(double orbitTime) {
            return  GetOrbitalVelocityAtTrueAnomaly(GetTrueAnomalyAtOrbitTime(orbitTime));
        }

        public Vector3d GetOrbitalVelocityAtTrueAnomaly(double trueAnomaly) {
            double x = Math.Cos(trueAnomaly);
            double y = Math.Sin(trueAnomaly);
            double mu_over_h = Math.Sqrt(body.mu / (semiMajorAxis * (1.0 - eccentricity * eccentricity)));
            double vx = -y * mu_over_h;
            double vy = (x + eccentricity) * mu_over_h;
            return FrameX * vx + FrameY * vy;
        }

        public double GetTrueAnomalyAtOrbitTime(double orbitTime) {
            double meanAnomaly = orbitTime * meanMotion;
            double eccentricAnomaly = GetEccentricAnomalyForMean(meanAnomaly);
            return GetTrueAnomalyForEccentric(eccentricAnomaly);
        }

        public double GetEccentricAnomalyForMean(double meanAnomaly) {
            if (eccentricity < 1.0) {
                if (eccentricity < 0.8) {
                    return solveEccentricAnomalyNewton(meanAnomaly);
                } else {
                    return solveEccentricAnomalySeries(meanAnomaly);
                }
            } else {
                return solveEccentricAnomalyHypNewton(meanAnomaly);
            }
        }

        private double solveEccentricAnomalyNewton(double meanAnomaly) {
            double dE = 1.0;
            double E = meanAnomaly + eccentricity * Math.Sin(meanAnomaly) + 0.5 * eccentricity * eccentricity * Math.Sin(2.0 * meanAnomaly);
            while (Math.Abs(dE) > 1e-7) {
                double y = E - eccentricity * Math.Sin(E);
                dE = (meanAnomaly - y) / (1.0 - eccentricity * Math.Cos(E));
                E += dE;
            }
            return E;
        }

        private double solveEccentricAnomalySeries(double M) {
            double E = M + 0.85 * eccentricity * (double)Math.Sign(Math.Sin(M));
            for (int i = 0; i < 8; i++) {
                double f1 = eccentricity * Math.Sin(E);
                double f2 = eccentricity * Math.Cos(E);
                double f4 = E - f1 - M;
                double f5 = 1.0 - f2;
                E += -5.0 * f4 / (f5 + (double)Math.Sign(f5) * Math.Sqrt(Math.Abs(16.0 * f5 * f5 - 20.0 * f4 * f1)));
            }
            return E;
        }

        private double solveEccentricAnomalyHypNewton(double meanAnomaly) {
            double dE = 1.0;
            double f = 2.0 * meanAnomaly / eccentricity;
            double E = Math.Log(Math.Sqrt(f * f + 1.0) + f);
            while (Math.Abs(dE) > 1e-7) {
                dE = (eccentricity * Math.Sinh(E) - E - meanAnomaly) / (eccentricity * Math.Cosh(E) - 1.0);
                E -= dE;
            }
            return E;
        }

        public double GetTrueAnomalyForEccentric(double eccentricAnomaly) {
            if (eccentricity < 1.0) {
                double x = Math.Cos(eccentricAnomaly / 2.0);
                double y = Math.Sin(eccentricAnomaly / 2.0);
                return 2.0 * Math.Atan2(Math.Sqrt(1.0 + eccentricity) * y, Math.Sqrt(1.0 - eccentricity) * x);
            } else {
                double x = Math.Cosh(eccentricAnomaly / 2.0);
                double y = Math.Sinh(eccentricAnomaly / 2.0);
                return 2.0 * Math.Atan2(Math.Sqrt(eccentricity + 1.0) * y, Math.Sqrt(eccentricity - 1.0) * x);
            }
        }

        public override string ToString() {
            return $"Orbit: body={body.name} inc={inclination} ecc={eccentricity} sMa={semiMajorAxis} Epoch={epoch} LAN={LAN} ArgPe={argumentOfPeriapsis} meanAtEpoch={meanAnomalyAtEpoch} FrameX={FrameX} FrameY={FrameY} FrameZ={FrameZ}";
        }

        public Vector3d SwappedAbsolutePositionAtUT(double UT) {
            Vector3d bodyPosition = body.orbit?.SwappedAbsolutePositionAtUT(UT) ?? Vector3d.zero;

            return bodyPosition + SwappedRelativePositionAtUT(UT);
        }

        public Vector3d SwappedOrbitalVelocityAtUT(double UT) {
            return GetOrbitalVelocityAtUT(UT).SwapYZ();
        }

        public Vector3d SwappedRelativePositionAtUT(double UT) {
            return GetRelativePositionAtUT(UT).SwapYZ();
        }

        public Vector3d Prograde(double UT) {
            return SwappedOrbitalVelocityAtUT(UT).normalized;
        }

        public Vector3d NormalPlus(double UT) {
            return -FrameZ.SwapYZ();
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
            return new OrbitTestRef(body, GetRelativePositionAtUT(UT), GetOrbitalVelocityAtUT(UT) + dV.SwapYZ(), UT);
        }

        public double MeanAnomalyAtUT(double UT) {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (orbitTimeAtEpoch + (UT - epoch)) * MeanMotion;
            if (eccentricity < 1) ret = ExtraMath.ClampRadiansTwoPi(ret);
            return ret;
        }

        public double UTAtMeanAnomaly(double meanAnomaly, double UT) {
            double currentMeanAnomaly = MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (eccentricity < 1) meanDifference = ExtraMath.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / MeanMotion;
        }

        public double GetMeanAnomalyAtEccentricAnomaly(double E) {
            double e = eccentricity;
            if (e < 1) { //elliptical orbits
                return ExtraMath.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            } else { //hyperbolic orbits
                return (e * Math.Sinh(E)) - E;
            }
        }

        public double GetEccentricAnomalyAtTrueAnomaly(double trueAnomaly) {
            double e = eccentricity;
            trueAnomaly = ExtraMath.ClampDegrees360(trueAnomaly);
            trueAnomaly = trueAnomaly * (UtilMath.Deg2Rad);

            if (e < 1) { //elliptical orbits
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI) sinE *= -1;

                return ExtraMath.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            } else { //hyperbolic orbits
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1) throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + eccentricity);

                double E = ExtraMath.Acosh(coshE);
                if (trueAnomaly > Math.PI) E *= -1;

                return E;
            }
        }

        public double TimeOfTrueAnomaly(double trueAnomaly, double UT) {
            return UTAtMeanAnomaly(GetMeanAnomalyAtEccentricAnomaly(GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
        }

        public double NextPeriapsisTime(double UT) {
            if (eccentricity < 1) {
                return TimeOfTrueAnomaly(0, UT);
            } else {
                return UT - MeanAnomalyAtUT(UT) / MeanMotion;
            }
        }

        public double NextApoapsisTime(double UT) {
            if (eccentricity < 1) {
                return TimeOfTrueAnomaly(180, UT);
            } else {
                throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
            }
        }

        public double TrueAnomalyAtRadius(double radius) {
            if (eccentricity < 1) {
                radius = Math.Min(Math.Max(radius, PeR), ApR);
            } else {
                radius = Math.Max(radius, PeR);
            }
            return Math.Acos((semiMajorAxis * (1.0 - eccentricity * eccentricity) / radius - 1.0) / eccentricity);
        }

        public double NextTimeOfRadius(double UT, double radius) {
            if (radius < PeR || (eccentricity < 1 && radius > ApR)) throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: PeR = " + PeR + " and ApR = " + ApR);

            double trueAnomaly1 = UtilMath.Rad2Deg * TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 360 - trueAnomaly1;
            double time1 = TimeOfTrueAnomaly(trueAnomaly1, UT);
            double time2 = TimeOfTrueAnomaly(trueAnomaly2, UT);
            if (time2 < time1 && time2 > UT) return time2;
            else return time1;
        }

        public double SynodicPeriod(IOrbit other) {
            int sign = (Vector3d.Dot(SwappedOrbitNormal, other.SwappedOrbitNormal) > 0 ? 1 : -1); //detect relative retrograde motion
            return Math.Abs(1.0 / (1.0 / Period - sign * 1.0 / other.Period)); //period after which the phase angle repeats
        }

        public Vector3d SwappedRelativePositionAtPeriapsis {
            get {
                Vector3d vectorToAN = QuaternionD.AngleAxis(-LAN, Vector3d.up) * Vector3d.right;
                Vector3d vectorToPe = QuaternionD.AngleAxis(argumentOfPeriapsis, SwappedOrbitNormal) * vectorToAN;
                return PeR * vectorToPe;
            }
        }

        public double TrueAnomalyFromVector(Vector3d vec) {
            Vector3d oNormal = SwappedOrbitNormal;
            Vector3d projected = Vector3d.Exclude(oNormal, vec);
            Vector3d vectorToPe = SwappedRelativePositionAtPeriapsis;
            double angleFromPe = Vector3d.Angle(vectorToPe, projected);

            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90) {
                return angleFromPe;
            } else {
                return 360 - angleFromPe;
            }
        }

        public double AscendingNodeTrueAnomaly(IOrbit b) {
            Vector3d vectorToAN = Vector3d.Cross(SwappedOrbitNormal, b.SwappedOrbitNormal);
            return TrueAnomalyFromVector(vectorToAN);
        }

        public double DescendingNodeTrueAnomaly(IOrbit b) {
            return ExtraMath.ClampDegrees360(AscendingNodeTrueAnomaly(b) + 180);
        }

        public double TimeOfAscendingNode(IOrbit b, double UT) {
            return TimeOfTrueAnomaly(AscendingNodeTrueAnomaly(b), UT);
        }

        public double TimeOfDescendingNode(IOrbit b, double UT) {
            return TimeOfTrueAnomaly(DescendingNodeTrueAnomaly(b), UT);
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