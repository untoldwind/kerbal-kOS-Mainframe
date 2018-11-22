using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Serialization;
using kOS.Safe.Exceptions;
using kOSMainframe.Orbital;
using System.Reflection;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("Maneuvers")]
    public class Maneuvers : Structure, IHasSharedObjects {
        private const BindingFlags BindFlags = BindingFlags.Instance
                                               | BindingFlags.Public
                                               | BindingFlags.NonPublic;

        private static FieldInfo OrbitField = typeof(OrbitInfo).GetField("orbit", BindFlags);

        public SharedObjects Shared {
            get;
            set;
        }
        private readonly Orbit orbit;
        private double minUT;

        public Maneuvers(SharedObjects sharedObjs) {
            Shared = sharedObjs;
            this.orbit = sharedObjs.Vessel.orbit;
            this.minUT = Planetarium.GetUniversalTime() + 10;
            InitializeSuffixes();
        }

        public Maneuvers(SharedObjects sharedObjs, OrbitInfo orbitInfo) {
            Shared = sharedObjs;
            this.orbit = GetOrbitFromOrbitInfo(orbitInfo);
            this.minUT = Planetarium.GetUniversalTime() + 10;
            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("MIN_TIME", new SetSuffix<TimeSpan>(GetMinTime, SetMinTime));
            AddSuffix("CIRCULARIZE", new NoArgsSuffix<Node>(CircularizeOrbit, "Circularize the given orbit at apoapsis if possible, otherwise at periapsis"));
            AddSuffix("CIRCULARIZE_AT", new OneArgsSuffix<Node, TimeSpan>(CircularizeOrbitAt, "Circularize the given orbit at the given time"));
            AddSuffix("ELLIPTICIZE", new ThreeArgsSuffix<Node, TimeSpan, ScalarValue, ScalarValue>(EllipticizeOrbit, "Ellipticize the given orbit (UT, newPeR, newApR"));
            AddSuffix("CHANGE_PERIAPSIS", new TwoArgsSuffix<Node, TimeSpan, ScalarValue>(ChangePeriapsis, "Change periapsis of given orbit (UT, newPeR"));
            AddSuffix("CHANGE_APOAPSIS", new TwoArgsSuffix<Node, TimeSpan, ScalarValue>(ChangeApoapsis, "Change apoapsis of given orbit (UT, newApR"));
            AddSuffix("MATCH_PLANES", new OneArgsSuffix<Node, OrbitInfo>(MatchPlanes, "Match planes of given orbit with target orbit"));
            AddSuffix("HOHMANN", new OneArgsSuffix<Node, OrbitInfo>(Hohmann, "Regular Hohmann transfer from given orbit to target orbit"));
            AddSuffix("HOHMANN_LAMBERT", new TwoArgsSuffix<Node, OrbitInfo, ScalarValue>(HohmannLambert));
            AddSuffix("BIIMPULSIVE", new OneArgsSuffix<Node, OrbitInfo>(BiImpulsive));
            AddSuffix("CORRECTION", new OneArgsSuffix<Node, OrbitInfo>(CourseCorrection));
            AddSuffix("CHEAPEST_CORRECTION", new OneArgsSuffix<Node, OrbitInfo>(CheapestCourseCorrection));
            AddSuffix("CHEAPEST_CORRECTION_BODY", new TwoArgsSuffix<Node, BodyTarget, ScalarValue>(CheapestCourseCorrectionBody));
            AddSuffix("CHEAPEST_CORRECTION_DIST", new TwoArgsSuffix<Node, OrbitInfo, ScalarValue>(CheapestCourseCorrectionDist));
            AddSuffix("MATCH_VELOCITIES", new OneArgsSuffix<Node, OrbitInfo>(MatchVelocities));
            AddSuffix("RETURN_FROM_MOON", new OneArgsSuffix<Node, ScalarValue>(ReturnFromMoon));
        }

        private TimeSpan GetMinTime() {
            return new TimeSpan(minUT);
        }

        private void SetMinTime(TimeSpan minTime) {
            this.minUT = minTime.ToUnixStyleTime();
        }

        private Node CircularizeOrbit() {
            double UT = minUT;
            if(orbit.eccentricity < 1) {
                UT = orbit.NextApoapsisTime(UT);
            } else {
                UT = orbit.NextPeriapsisTime(UT);
            }
            return OrbitChange.Circularize(orbit, UT).ToKOS(Shared);
        }

        private Node CircularizeOrbitAt(TimeSpan time) {
            return OrbitChange.Circularize(orbit, System.Math.Max(time.ToUnixStyleTime(), minUT)).ToKOS(Shared);
        }

        private Node EllipticizeOrbit(TimeSpan time, ScalarValue newPeR, ScalarValue newApR) {
            return OrbitChange.Ellipticize(orbit, System.Math.Max(time.ToUnixStyleTime(), minUT), newPeR, newApR).ToKOS(Shared);
        }

        private Node ChangePeriapsis(TimeSpan time, ScalarValue newPeR) {
            return OrbitChange.ChangePeriapsis(orbit, System.Math.Max(time.ToUnixStyleTime(), minUT), newPeR).ToKOS(Shared);
        }

        private Node ChangeApoapsis(TimeSpan time, ScalarValue newApR) {
            return OrbitChange.ChangeApoapsis(orbit, System.Math.Max(time.ToUnixStyleTime(), minUT), newApR).ToKOS(Shared);
        }

        private Node MatchPlanes(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            var anExists = orbit.AscendingNodeExists(target);
            var dnExists = orbit.DescendingNodeExists(target);
            var anNode = anExists ? OrbitMatch.MatchPlanesAscending(orbit, target, minUT) : NodeParameters.zero;
            var dnNode = dnExists ? OrbitMatch.MatchPlanesDescending(orbit, target, minUT) : NodeParameters.zero;

            if(!anExists && !dnExists) {
                throw new KOSException("neither ascending nor descending node with target exists.");
            } else if(!dnExists || anNode.time < dnNode.time) {
                return anNode.ToKOS(Shared);
            } else {
                return dnNode.ToKOS(Shared);
            }
        }

        private Node Hohmann(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, target, minUT, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node HohmannLambert(OrbitInfo targetInfo, ScalarValue subtractProgradeDV) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannLambertTransfer(orbit, target, minUT, out burnUT, subtractProgradeDV);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node BiImpulsive(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(orbit, target, minUT, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node CourseCorrection(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            var deltaV = OrbitalManeuverCalculator.DeltaVForCourseCorrection(orbit, minUT, target);

            return NodeFromDeltaV(deltaV, minUT);
        }

        private Node CheapestCourseCorrection(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, minUT, target, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node CheapestCourseCorrectionBody(BodyTarget body, ScalarValue finalPeR) {
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, minUT, body.Orbit, body.Body, finalPeR, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node CheapestCourseCorrectionDist(OrbitInfo targetInfo, ScalarValue caDistance) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, minUT, target, caDistance, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node MatchVelocities(OrbitInfo targetInfo) {
            var target = GetOrbitFromOrbitInfo(targetInfo);
            double collisionUT = orbit.NextClosestApproachTime(target, minUT);

            var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, collisionUT, target);

            return NodeFromDeltaV(deltaV, collisionUT);
        }

        private Node ReturnFromMoon(ScalarValue targetPrimaryRadius) {
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(orbit, minUT, targetPrimaryRadius, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
        }

        private Node NodeFromDeltaV(Vector3d deltaV, double UT) {
            return orbit.DeltaVToNode(UT, deltaV).ToKOS(Shared);
        }

        private static Orbit GetOrbitFromOrbitInfo(OrbitInfo orbitInfo) {
            return (Orbit)OrbitField.GetValue(orbitInfo);
        }
    }
}
