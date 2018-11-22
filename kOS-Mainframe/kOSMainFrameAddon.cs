using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS;
using kOS.AddOns;
using kOS.Suffixed;
using UnityEngine;
using kOSMainframe.VesselExtra;

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
            AddSuffix("MANEUVERS_FOR", new OneArgsSuffix<Maneuvers, OrbitInfo>(GetManeuvers, "Get maneuvers for a given orbit"));
            AddSuffix("LAUNCH", new Suffix<VesselLaunch>(() => new VesselLaunch(shared), "Get launch helper for current vessel"));
            AddSuffix("LAUNCH_FOR", new OneArgsSuffix<VesselLaunch, VesselTarget>(GetLaunch, "Get launch helper for given vessel"));
            AddSuffix("LANDING", new Suffix<VesselLanding>(() => new VesselLanding(shared), "Get landing helper for current vessel"));
            AddSuffix("LANDING_FOR", new OneArgsSuffix<VesselLanding, VesselTarget>(GetLanding, "Get landing helper for given vessel"));
            AddSuffix("STAGESTATS_VAC", new Suffix<ListValue>(() => GetStageStatsVac(shared.Vessel)));
            AddSuffix("STAGESTATS_ATM", new Suffix<ListValue>(() => GetStageStatsAtm(shared.Vessel)));
            AddSuffix("TARGET_STAGESTATS_VAC", new OneArgsSuffix<ListValue, VesselTarget>(GetStageStatsVacForVessel, "Get vacuum stage stats of vessel"));
            AddSuffix("TARGET_STAGESTATS_ATM", new OneArgsSuffix<ListValue, VesselTarget>(GetStageAtmForVessel, "Get atmospheric stage stats of vessel (based on current height)"));
        }

        public override BooleanValue Available() {
            return true;
        }

        private ListValue GetStageStatsVacForVessel(VesselTarget vessel) {
            return GetStageStatsVac(vessel.Vessel);
        }

        private ListValue GetStageStatsVac(Vessel vessel) {
            var list = new ListValue();
            var sim = new FuelFlowSimulation(shared);

            sim.Init(vessel.parts, true);

            foreach (StageStats stats in sim.SimulateAllStages(1.0f, 0.0, 0.0, vessel.mach)) {
                list.Add(stats);
            }

            return list;
        }

        private ListValue GetStageAtmForVessel(VesselTarget vessel) {
            return GetStageStatsAtm(vessel.Vessel);
        }

        private ListValue GetStageStatsAtm(Vessel vessel) {
            var list = new ListValue();
            var sim = new FuelFlowSimulation(shared);

            sim.Init(vessel.parts, true);

            foreach (StageStats stats in sim.SimulateAllStages(1.0f, vessel.staticPressurekPa, vessel.atmDensity / 1.225, vessel.mach)) {
                list.Add(stats);
            }

            return list;
        }

        private Maneuvers GetManeuvers(OrbitInfo orbitInfo) {
            return new Maneuvers(shared, orbitInfo);
        }

        private VesselLaunch GetLaunch(VesselTarget vessel) {
            return new VesselLaunch(shared, vessel);
        }

        private VesselLanding GetLanding(VesselTarget vessel) {
            return new VesselLanding(shared, vessel);
        }
    }
}
