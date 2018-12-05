using System;
using NUnit.Framework;

namespace kOSMainframeTest {
    public class OrbitTestRef {
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

        public double periapsisRadius {
            get {
                return (1.0 - eccentricity) * semiMajorAxis;
            }
        }

        public double apoapsisRadius {
            get {
                return (1.0 + eccentricity) * semiMajorAxis;
            }
        }

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
            if (ascendingNode.sqrMagnitude == 0.0)
            {
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


        public Vector3d GetPositionAtUT(double UT) {
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
            double h = Math.Sqrt(body.mu / (semiMajorAxis * (1.0 - eccentricity * eccentricity)));
            double vx = -y * h;
            double vy = (x + eccentricity) * h;
            return FrameX * vx + FrameY * vy;
        }

        public double GetTrueAnomalyAtOrbitTime(double orbitTime) {
            double meanAnomaly = orbitTime * meanMotion;
            double eccentricAnomaly = GetEccentricAnomalyForMean(meanAnomaly);
            return GetTrueAnomaly(eccentricAnomaly);
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

        public double GetTrueAnomaly(double E) {
            if (eccentricity < 1.0) {
                double num = Math.Cos(E / 2.0);
                double num2 = Math.Sin(E / 2.0);
                return 2.0 * Math.Atan2(Math.Sqrt(1.0 + eccentricity) * num2, Math.Sqrt(1.0 - eccentricity) * num);
            }
            double num3 = Math.Sinh(E / 2.0);
            double num4 = Math.Cosh(E / 2.0);
            return 2.0 * Math.Atan2(Math.Sqrt(eccentricity + 1.0) * num3, Math.Sqrt(eccentricity - 1.0) * num4);
        }

        public override string ToString()
        {
            return $"Orbit: body={body.name} inc={inclination} ecc={eccentricity} sMa={semiMajorAxis} Epoch={epoch} LAN={LAN} ArgPe={argumentOfPeriapsis} meanAtEpoch={meanAnomalyAtEpoch} FrameX={FrameX} FrameY={FrameY} FrameZ={FrameZ}";
        }
    }
}