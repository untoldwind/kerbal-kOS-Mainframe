using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOSMainframe.VesselExtra;
using Math = System.Math;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("VesselExtendedInfo")]
    public class VesselExtendedInfo : Structure  {
        protected readonly SharedObjects shared;

        private readonly Vessel vessel;

        public VesselExtendedInfo(SharedObjects sharedObjs) {
            this.shared = sharedObjs;
            this.vessel = sharedObjs.Vessel;
            InitializeSuffixes();
        }

        public VesselExtendedInfo(SharedObjects sharedObjs, VesselTarget vessel) {
            this.shared = sharedObjs;
            this.vessel = vessel.Vessel;
            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("STAGES", new Suffix<ListValue>(() => GetStageStats(DeltaVSituationOptions.Altitude), "Get stages information for ths current altitude/situtation"));
            AddSuffix("STAGES_ASL", new Suffix<ListValue>(() => GetStageStats(DeltaVSituationOptions.SeaLevel), "Get stages information in sea level (of the current body)"));
            AddSuffix("STAGES_VAC", new Suffix<ListValue>(() => GetStageStats(DeltaVSituationOptions.Vaccum), "Get stages information in vaccum"));
            AddSuffix("BURN_TIME", new TwoArgsSuffix<BurnTime, ScalarValue, ScalarValue>(CalculateBurnTime, "Calculate burn time for (deltaV, stageDelay)"));
            AddSuffix("BURN_TIME_LIMIT", new ThreeArgsSuffix<BurnTime, ScalarValue, ScalarValue, ScalarValue>(CalculateBurnTimeWithLimit, "Calculate burn time for (deltaV, stageDelay, thrustLimit)"));
        }

        private ListValue GetStageStats(DeltaVSituationOptions situation) {
            var list = new ListValue();

            foreach(var stats in CollectStageStats(situation)) {
                list.Add(stats);
            }

            return list;
        }

        private BurnTime CalculateBurnTime(ScalarValue deltaV, ScalarValue stageDelay) {
            return CalculateBurnTimeWithLimit(deltaV, stageDelay, 1.0);
        }

        private BurnTime CalculateBurnTimeWithLimit(ScalarValue deltaV, ScalarValue stageDelay, ScalarValue throttleLimit) {
            var vacStats = CollectStageStats(DeltaVSituationOptions.Vaccum);

            double dvLeft = deltaV.GetDoubleValue();
            double halfDvLeft = deltaV.GetDoubleValue() / 2;

            double burnTime = 0;
            double halfBurnTime = 0;

            double lastStageBurnTime = 0;
            for (int i = vacStats.Length - 1; i >= 0 && dvLeft > 0; i--) {
                var s = vacStats[i];
                if (s.deltaV <= 0 || s.thrust <= 0) {
                    // We staged again before autostagePreDelay is elapsed.
                    // Add the remaining wait time
                    if (burnTime - lastStageBurnTime < stageDelay.GetDoubleValue() && i != vacStats.Length - 1)
                        burnTime += stageDelay.GetDoubleValue() - (burnTime - lastStageBurnTime);
                    burnTime += stageDelay.GetDoubleValue();
                    lastStageBurnTime = burnTime;
                    continue;
                }

                double stageBurnDv = Math.Min(s.deltaV, dvLeft);
                dvLeft -= stageBurnDv;

                double stageBurnFraction = stageBurnDv / s.deltaV;

                // Delta-V is proportional to ln(m0 / m1) (where m0 is initial
                // mass and m1 is final mass). We need to know the final mass
                // after this stage burns (m1b):
                //      ln(m0 / m1) * stageBurnFraction = ln(m0 / m1b)
                //      exp(ln(m0 / m1) * stageBurnFraction) = m0 / m1b
                //      m1b = m0 / (exp(ln(m0 / m1) * stageBurnFraction))
                double stageBurnFinalMass = s.startMass / Math.Exp(Math.Log(s.startMass / s.endMass) * stageBurnFraction);
                double stageAvgAccel = s.thrust / ((s.startMass + stageBurnFinalMass) / 2d);

                // Right now, for simplicity, we're ignoring throttle limits for
                // all but the current stage. This is wrong, but hopefully it's
                // close enough for now.
                if (i == vacStats.Length - 1) {
                    stageAvgAccel *= throttleLimit.GetDoubleValue();
                }

                halfBurnTime += Math.Min(halfDvLeft, stageBurnDv) / stageAvgAccel;
                halfDvLeft = Math.Max(0, halfDvLeft - stageBurnDv);

                burnTime += stageBurnDv / stageAvgAccel;

            }

            /* infinity means acceleration is zero for some reason, which is dangerous nonsense, so use zero instead */
            if (double.IsInfinity(halfBurnTime)) {
                halfBurnTime = 0.0;
            }

            if (double.IsInfinity(burnTime)) {
                burnTime = 0.0;
            }

            return new BurnTime(burnTime, halfBurnTime);
        }

        private StageStats[] CollectStageStats(DeltaVSituationOptions situation) {
            var stageStats = new StageStats[this.vessel.currentStage + 1];
            var vesselDeltaV = this.vessel.VesselDeltaV;

            for(int stage = 0; stage <= vessel.currentStage; stage++) {
                var stageInfo = vesselDeltaV.GetStage(stage);
                var stats = new StageStats(this.shared);
                stageStats[stage] = stats;

                if(stageInfo != null) {
                    stats.deltaV = stageInfo.GetSituationDeltaV(situation);
                    stats.stageBurnTime = stageInfo.stageBurnTime;
                    stats.thrust = stageInfo.GetSituationThrust(situation);
                    stats.isp = stageInfo.GetSituationISP(situation);
                    stats.twr = stageInfo.GetSituationTWR(situation);
                    stats.startMass = stageInfo.startMass;
                    stats.endMass = stageInfo.endMass;
                    stats.stageMass = stageInfo.stageMass;
                    stats.fuelMass = stageInfo.fuelMass;
                }
            }
            return stageStats;
        }
    }
}
