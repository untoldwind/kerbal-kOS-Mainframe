using kOS;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Serialization;
using kOSMainframe.Landing;
using kOS.Safe.Exceptions;
using kOSMainframe.Orbital;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("VesselLanding")]
    public class VesselLanding : Structure, IHasSharedObjects {
        public SharedObjects Shared {
            get;
            set;
        }
        private readonly Vessel vessel;

        public VesselLanding(SharedObjects sharedObjs) {
            Shared = sharedObjs;
            this.vessel = sharedObjs.Vessel;
            InitializeSuffixes();
        }

        public VesselLanding(SharedObjects sharedObjs, VesselTarget vessel) {
            Shared = sharedObjs;
            this.vessel = vessel.Vessel;
            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("DESIRED_SPEED", new Suffix<ScalarValue>(GetDesiredSpeed));
            AddSuffix("DESIRED_SPEED_AFTER", new OneArgsSuffix<ScalarValue, TimeSpan>(GetDesiredSpeedAfter));
            AddSuffix("PREDICTION_START", new OneArgsSuffix<GeoCoordinates>(PredictionStart));
            AddSuffix("PREDICTION_STOP", new NoArgsVoidSuffix(PredictionStop));
            AddSuffix("PREDICTION_RUNNING", new Suffix<BooleanValue>(PredictionRunning));
            AddSuffix("PREDICTED_OUTCOME", new Suffix<StringValue>(GetOutcome));
            AddSuffix("PREDICTED_SITE", new Suffix<GeoCoordinates>(GetLandingSite));
            AddSuffix("PREDICTED_BREAK_TIME", new Suffix<TimeSpan>(GetDeaccelerationTime));
            AddSuffix("PREDICTED_LAND_TIME", new Suffix<TimeSpan>(GetLandingTime));
            AddSuffix("COURSE_CORRECTION", new TwoArgsSuffix<Node, TimeSpan, BooleanValue>(CourseCorrection));
            AddSuffix("COURSE_CORRECTION_DETLAV", new OneArgsSuffix<Vector, BooleanValue>(CourseCorrectionDeltaV));
        }

        private ScalarValue GetDesiredSpeed() {
            var speedPolicy = LandingSimulation.Current?.descentSpeedPolicy;

            if (speedPolicy == null) return 0;

            double maxSpeed = speedPolicy.MaxAllowedSpeed(vessel.CoMD - vessel.mainBody.position, vessel.GetSrfVelocity());

            if (double.IsNaN(maxSpeed) || double.IsInfinity(maxSpeed)) return 0;
            return maxSpeed;
        }

        private ScalarValue GetDesiredSpeedAfter(TimeSpan time) {
            var speedPolicy = LandingSimulation.Current?.descentSpeedPolicy;

            if (speedPolicy == null) return 0;

            double dt = time.ToUnixStyleTime();
            return speedPolicy.MaxAllowedSpeed(vessel.CoMD + vessel.obt_velocity * dt - vessel.mainBody.position,
                                               vessel.GetSrfVelocity() + dt * vessel.graviticAcceleration);
        }

        private void PredictionStart(GeoCoordinates coordinates) {
            LandingSimulation.Start(vessel, coordinates.Latitude, coordinates.Longitude);
        }

        private void PredictionStop() {
            LandingSimulation.Stop();
        }

        private BooleanValue PredictionRunning() {
            return LandingSimulation.Current != null;
        }

        private StringValue GetOutcome() {
            var result = LandingSimulation.Current?.result;
            if (result == null) return "NO_RESULT";
            switch (result.outcome) {
            case Outcome.LANDED:
                return "LANDED";
            case Outcome.TIMED_OUT:
                return "TIMED_OUT";
            case Outcome.NO_REENTRY:
                return "NO_REENTRY";
            case Outcome.AEROBRAKED:
                return "AEROBRAKED";
            case Outcome.ERROR:
                return "ERROR";
            default:
                return "NO_RESULT";
            }
        }

        private GeoCoordinates GetLandingSite() {
            var result = LandingSimulation.Current?.result;
            if (result == null && result.outcome != Outcome.LANDED) throw new KOSException("No prediction");

            return new GeoCoordinates(Shared, result.endPosition.latitude, result.endPosition.longitude);
        }

        private TimeSpan GetDeaccelerationTime() {
            var result = LandingSimulation.Current?.result;
            if (result == null && result.outcome != Outcome.LANDED) return new TimeSpan(Planetarium.GetUniversalTime());

            return new TimeSpan(result.startUT);
        }

        private TimeSpan GetLandingTime() {
            var result = LandingSimulation.Current?.result;
            if (result == null && result.outcome != Outcome.LANDED) return new TimeSpan(Planetarium.GetUniversalTime());

            return new TimeSpan(result.endUT);
        }

        private Node CourseCorrection(TimeSpan time, BooleanValue allowPrograde) {
            var deltaV = LandingSimulation.Current?.ComputeCourseCorrection(time.ToUnixStyleTime(), allowPrograde);
            if (deltaV.HasValue)
                return vessel.orbit.DeltaVToNode(time.ToUnixStyleTime(), deltaV.Value).ToKOS(Shared);
            throw new KOSException("Course correction not available");
        }

        private Vector CourseCorrectionDeltaV(BooleanValue allowPrograde) {
            var deltaV = LandingSimulation.Current?.ComputeCourseCorrection(Planetarium.GetUniversalTime(), allowPrograde);
            if (deltaV.HasValue)
                return new Vector(deltaV.Value);
            throw new KOSException("Course correction not available");
        }
    }
}
