using kOS;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;
using kOS.Serialization;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using kOSMainframe.Orbital;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("VesselLaunch")]
    public class VesselLaunch : Structure {
        protected readonly SharedObjects shared;

        private readonly Vessel vessel;

        public VesselLaunch(SharedObjects sharedObjs) {
            this.shared = sharedObjs;
            this.vessel = sharedObjs.Vessel;
            InitializeSuffixes();
        }

        public VesselLaunch(SharedObjects sharedObjs, VesselTarget vessel) {
            this.shared = sharedObjs;
            this.vessel = vessel.Vessel;
            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("HEADING_FOR_INCLINATION", new OneArgsSuffix<ScalarValue, ScalarValue>(HeadingForInclination));
        }

        private ScalarValue HeadingForInclination(ScalarValue inclination) {
            return OrbitToGround.HeadingForLaunchInclination(vessel, inclination);
        }
    }
}
