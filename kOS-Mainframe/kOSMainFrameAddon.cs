using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS;
using kOS.AddOns;
using kOS.Suffixed;
using UnityEngine;

namespace kOSMainframe {
    [kOSAddon("MainFrame")]
    [kOS.Safe.Utilities.KOSNomenclature("MainFrameAddon")]
    public class kOSMainFrameAddon : kOS.Suffixed.Addon {
        public kOSMainFrameAddon(SharedObjects shared) : base(shared) {
            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            Debug.Log("MainFrame booting...");
            AddSuffix("MANEUVERS", new Suffix<Maneuvers>(() => new Maneuvers(shared), "Get maneuvers for current vessel"));
            AddSuffix("MANEUVERS_FOR", new OneArgsSuffix<Maneuvers, Orbitable>(GetManeuvers, "Get maneuvers for a given orbitable"));
            AddSuffix("LAUNCH", new Suffix<VesselLaunch>(() => new VesselLaunch(shared), "Get launch helper for current vessel"));
            AddSuffix("LAUNCH_FOR", new OneArgsSuffix<VesselLaunch, VesselTarget>(GetLaunch, "Get launch helper for given vessel"));
            AddSuffix("LANDING", new Suffix<VesselLanding>(() => new VesselLanding(shared), "Get landing helper for current vessel"));
            AddSuffix("LANDING_FOR", new OneArgsSuffix<VesselLanding, VesselTarget>(GetLanding, "Get landing helper for given vessel"));
            AddSuffix("INFO", new Suffix<VesselExtendedInfo>(() => new VesselExtendedInfo(shared), "Get information for current vessel"));
            AddSuffix("INFO_FOR", new OneArgsSuffix<VesselExtendedInfo, VesselTarget>(GetVesselExtendedInfo, "Get information for given vessel"));
        }

        public override BooleanValue Available() {
            return true;
        }

        private Maneuvers GetManeuvers(Orbitable orbitable) {
            return new Maneuvers(shared, orbitable);
        }

        private VesselLaunch GetLaunch(VesselTarget vessel) {
            return new VesselLaunch(shared, vessel);
        }

        private VesselLanding GetLanding(VesselTarget vessel) {
            return new VesselLanding(shared, vessel);
        }

        private VesselExtendedInfo GetVesselExtendedInfo(VesselTarget vessel) {
            return new VesselExtendedInfo(shared, vessel);
        }
    }
}
