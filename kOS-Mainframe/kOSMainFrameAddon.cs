using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS;
using kOS.AddOns;
using kOS.Suffixed;
using UnityEngine;
using kOSMainframe.VesselExtra;

namespace kOSMainframe
{
	[kOSAddon("MainFrame")]
	[kOS.Safe.Utilities.KOSNomenclature("MainFrameAddon")]
	public class kOSMainFrameAddon : kOS.Suffixed.Addon
    {
		public kOSMainFrameAddon(SharedObjects shared) : base(shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
			Debug.Log("MainFrame booting...");
			AddSuffix("CURRENT_STAGESTATS_VAC", new Suffix<ListValue>(() => GetStageStatsVac(shared.Vessel)));
			AddSuffix("CURRENT_STAGESTATS_ATM", new Suffix<ListValue>(() => GetStageStatsAtm(shared.Vessel)));
			AddSuffix("STAGESTATS_VAC", new OneArgsSuffix<ListValue, VesselTarget>(GetStageStatsVacForVessel, "Get vacuum stage stats of vessel"));
			AddSuffix("STAGESTATS_ATM", new OneArgsSuffix<ListValue, VesselTarget>(GetStageAtmForVessel, "Get atmospheric stage stats of vessel (based on current height)"));
        }

		public override BooleanValue Available()
		{
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

		private ListValue GetStageStatsAtm(Vessel vessel)
        {
            var list = new ListValue();
			var sim = new FuelFlowSimulation(shared);

            sim.Init(vessel.parts, true);

			foreach (StageStats stats in sim.SimulateAllStages(1.0f, vessel.staticPressurekPa, vessel.atmDensity / 1.225, vessel.mach))
            {
                list.Add(stats);
            }

            return list;
        }
    }
}
