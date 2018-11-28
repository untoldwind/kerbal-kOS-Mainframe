using kOS;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Serialization;
using kOSMainframe.Landing;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("LandingPrediction")]
    public class LandingPrediction : Structure, IHasSharedObjects {
        public SharedObjects Shared {
            get;
            set;
        }

        public readonly Result result;

        public LandingPrediction(SharedObjects sharedObjs, Result result) {
            Shared = sharedObjs;
            this.result = result;

            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("OUTCOME", new Suffix<StringValue>(GetOutcome));
            AddSuffix("LANDING_SITE", new Suffix<Vector>(GetLandingSite));
        }

        private StringValue GetOutcome() {
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

        private Vector GetLandingSite()
        {
            if (result == null && result.outcome != Outcome.LANDED) return Vector.Zero;

            return new Vector(result.WorldEndPosition() - result.body.position);
        }
    }

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
            AddSuffix("PREDICTION_RESULT", new Suffix<LandingPrediction>(GetPredictionResult));
        }

        private ScalarValue GetDesiredSpeed() {
            var speedPolicy = LandingSimulation.Current?.descentSpeedPolicy;

            if (speedPolicy == null) return 0;

            return speedPolicy.MaxAllowedSpeed(vessel.CoMD - vessel.mainBody.position, vessel.GetSrfVelocity());
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

        private LandingPrediction GetPredictionResult() {
            return new LandingPrediction(Shared, LandingSimulation.Current?.result);
        }
    }
}
