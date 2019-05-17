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
    public class Maneuvers : Structure {
        protected readonly SharedObjects shared;

        private readonly Orbit orbit;
        private double minUT;

        public Maneuvers(SharedObjects sharedObjs) {
            this.shared = sharedObjs;
            this.orbit = sharedObjs.Vessel.orbit;
            this.minUT = Planetarium.GetUniversalTime() + 10;
            InitializeSuffixes();
        }

        public Maneuvers(SharedObjects sharedObjs, Orbitable orbitable) {
            this.shared = sharedObjs;
            this.orbit = orbitable.Orbit;
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
            AddSuffix("CHANGE_INCLINATION", new TwoArgsSuffix<Node, TimeSpan, ScalarValue>(ChangeInclination, "Change inclination of given orbit (UT, newInc"));
            AddSuffix("MATCH_PLANES", new OneArgsSuffix<Node, Orbitable>(MatchPlanes, "Match planes of given orbit with target orbit"));
            AddSuffix("HOHMANN", new OneArgsSuffix<Node, Orbitable>(Hohmann, "Regular Hohmann transfer from given orbit to target orbit"));
            AddSuffix("HOHMANN_LAMBERT", new TwoArgsSuffix<Node, Orbitable, ScalarValue>(HohmannLambert));
            AddSuffix("BIIMPULSIVE", new OneArgsSuffix<Node, Orbitable>(BiImpulsive));
            AddSuffix("CORRECTION", new OneArgsSuffix<Node, Orbitable>(CourseCorrection));
            AddSuffix("CHEAPEST_CORRECTION", new OneArgsSuffix<Node, Orbitable>(CheapestCourseCorrection));
            AddSuffix("CHEAPEST_CORRECTION_BODY", new TwoArgsSuffix<Node, BodyTarget, ScalarValue>(CheapestCourseCorrectionBody));
            AddSuffix("CHEAPEST_CORRECTION_DIST", new TwoArgsSuffix<Node, Orbitable, ScalarValue>(CheapestCourseCorrectionDist));
            AddSuffix("MATCH_VELOCITIES", new OneArgsSuffix<Node, Orbitable>(MatchVelocities));
            AddSuffix("RETURN_FROM_MOON", new OneArgsSuffix<Node, ScalarValue>(ReturnFromMoon));
            AddSuffix("INTERPLANETARY", new TwoArgsSuffix<Node, Orbitable, BooleanValue>(InterplanetaryTransfer));
            AddSuffix("INTERPLANETARY_LAMBERT", new OneArgsSuffix<Node, Orbitable>(InterplanetaryLambertTransfer));
            AddSuffix("INTERPLANETARY_BIIMPULSIVE", new TwoArgsSuffix<Node, Orbitable, ScalarValue>(InterplanetaryBiImpulsiveTransfer));
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
            return OrbitChange.Circularize(orbit.wrap(), UT).ToKOS(this.shared);
        }

        private Node CircularizeOrbitAt(TimeSpan time) {
            return OrbitChange.Circularize(orbit.wrap(), System.Math.Max(time.ToUnixStyleTime(), minUT)).ToKOS(this.shared);
        }

        private Node EllipticizeOrbit(TimeSpan time, ScalarValue newPeR, ScalarValue newApR) {
            return OrbitChange.Ellipticize(orbit.wrap(), System.Math.Max(time.ToUnixStyleTime(), minUT), newPeR, newApR).ToKOS(this.shared);
        }

        private Node ChangePeriapsis(TimeSpan time, ScalarValue newPeR) {
            return OrbitChange.ChangePeriapsis(orbit.wrap(), System.Math.Max(time.ToUnixStyleTime(), minUT), newPeR).ToKOS(this.shared);
        }

        private Node ChangeApoapsis(TimeSpan time, ScalarValue newApR) {
            return OrbitChange.ChangeApoapsis(orbit.wrap(), System.Math.Max(time.ToUnixStyleTime(), minUT), newApR).ToKOS(this.shared);
        }

        private Node ChangeInclination(TimeSpan time, ScalarValue newInc) {
            return OrbitChange.ChangeInclination(orbit, System.Math.Max(time.ToUnixStyleTime(), minUT), newInc).ToKOS(this.shared);
        }

        private Node MatchPlanes(Orbitable orbitable) {
            var target = orbitable.Orbit;
            var anExists = orbit.AscendingNodeExists(target);
            var dnExists = orbit.DescendingNodeExists(target);
            var anNode = anExists ? OrbitMatch.MatchPlanesAscending(orbit, target, minUT) : NodeParameters.zero;
            var dnNode = dnExists ? OrbitMatch.MatchPlanesDescending(orbit, target, minUT) : NodeParameters.zero;

            if(!anExists && !dnExists) {
                throw new KOSException("neither ascending nor descending node with target exists.");
            } else if(!dnExists || anNode.time < dnNode.time) {
                return anNode.ToKOS(this.shared);
            } else {
                return dnNode.ToKOS(this.shared);
            }
        }

        private Node Hohmann(Orbitable orbitable) {
            var target = orbitable.Orbit;
            return OrbitIntercept.HohmannTransfer(orbit, target, minUT).ToKOS(this.shared);
        }

        private Node HohmannLambert(Orbitable orbitable, ScalarValue subtractProgradeDV) {
            var target = orbitable.Orbit;
            return OrbitIntercept.HohmannLambertTransfer(orbit, target, minUT, subtractProgradeDV).ToKOS(this.shared);
        }

        private Node BiImpulsive(Orbitable orbitable) {
            var target = orbitable.Orbit;
            return OrbitIntercept.BiImpulsiveAnnealed(orbit.wrap(), target.wrap(), minUT).ToKOS(this.shared);
        }

        private Node CourseCorrection(Orbitable orbitable) {
            var target = orbitable.Orbit;
            return OrbitIntercept.CourseCorrection(orbit, minUT, target).ToKOS(this.shared);
        }

        private Node CheapestCourseCorrection(Orbitable orbitable) {
            var target = orbitable.Orbit;
            return OrbitIntercept.CheapestCourseCorrection(orbit, minUT, target).ToKOS(this.shared);
        }

        private Node CheapestCourseCorrectionBody(BodyTarget body, ScalarValue finalPeR) {
            return OrbitIntercept.CheapestCourseCorrection(orbit, minUT, body.Orbit, body.Body, finalPeR).ToKOS(this.shared);
        }

        private Node CheapestCourseCorrectionDist(Orbitable orbitable, ScalarValue caDistance) {
            var target = orbitable.Orbit;
            return OrbitIntercept.CheapestCourseCorrection(orbit, minUT, target, caDistance).ToKOS(this.shared);
        }

        private Node MatchVelocities(Orbitable orbitable) {
            var target = orbitable.Orbit;
            double collisionUT = orbit.NextClosestApproachTime(target, minUT);

            return OrbitMatch.MatchVelocities(orbit, collisionUT, target).ToKOS(this.shared);
        }

        private Node ReturnFromMoon(ScalarValue targetPrimaryRadius) {
            return OrbitSOIChange.MoonReturnEjection(orbit, minUT, targetPrimaryRadius).ToKOS(this.shared);
        }

        private Node InterplanetaryTransfer(Orbitable target, BooleanValue syncPhaseAngle) {
            return OrbitSOIChange.InterplanetaryTransferEjection(orbit, minUT, target.Orbit, syncPhaseAngle).ToKOS(this.shared);
        }

        private Node InterplanetaryLambertTransfer(Orbitable target) {
            return OrbitSOIChange.InterplanetaryLambertTransferEjection(orbit, minUT, target.Orbit).ToKOS(this.shared);
        }

        private Node InterplanetaryBiImpulsiveTransfer(Orbitable target, ScalarValue maxUT) {
            return OrbitSOIChange.InterplanetaryBiImpulsiveEjection(orbit, minUT, target.Orbit, maxUT).ToKOS(this.shared);
        }
    }
}
