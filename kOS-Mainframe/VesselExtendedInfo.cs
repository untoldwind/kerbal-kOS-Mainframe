using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Serialization;
using kOSMainframe.VesselExtra;
using Math = System.Math;

namespace kOSMainframe {
    [kOS.Safe.Utilities.KOSNomenclature("BurnTime")]
    public class BurnTime : Structure {
        public readonly TimeSpan full;
        public readonly TimeSpan half;

        public BurnTime(double fullUT, double halfUT) {
            full = new TimeSpan(fullUT);
            half = new TimeSpan(halfUT);

            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("FULL", new Suffix<TimeSpan>(() => full));
            AddSuffix("HALF", new Suffix<TimeSpan>(() => half));
        }
    }

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
            AddSuffix("STAGES", new Suffix<ListValue>(GetStageStatsVac, "Get stages information for vacuum"));
            AddSuffix("STAGES_ATM", new Suffix<ListValue>(GetStageStatsAtm, "Get stages information in atmosphere at current height"));
            AddSuffix("BURN_TIME", new TwoArgsSuffix<BurnTime, ScalarValue, ScalarValue>(CalculateBurnTime, "Calculate burn time for (deltaV, stageDelay)"));
        }

        private ListValue GetStageStatsVac() {
            var list = new ListValue();
            var sim = new FuelFlowSimulation(this.shared);

            sim.Init(vessel.parts, true);

            foreach (StageStats stats in sim.SimulateAllStages(1.0f, 0.0, 0.0, vessel.mach)) {
                list.Add(stats);
            }

            return list;
        }

        private ListValue GetStageStatsAtm() {
            var list = new ListValue();
            var sim = new FuelFlowSimulation(this.shared);

            sim.Init(vessel.parts, true);

            foreach (StageStats stats in sim.SimulateAllStages(1.0f, vessel.staticPressurekPa, vessel.atmDensity / 1.225, vessel.mach)) {
                list.Add(stats);
            }

            return list;
        }

        private BurnTime CalculateBurnTime(ScalarValue deltaV, ScalarValue stageDelay) {
            return CalculateBurnTimeWithLimit(deltaV, stageDelay, 1.0);
        }

        private BurnTime CalculateBurnTimeWithLimit(ScalarValue deltaV, ScalarValue stageDelay, ScalarValue throttleLimit) {
            var sim = new FuelFlowSimulation(this.shared);

            sim.Init(vessel.parts, true);

            var vacStats = sim.SimulateAllStages(1.0f, 0.0, 0.0, vessel.mach);

            double dvLeft = deltaV.GetDoubleValue();
            double halfDvLeft = deltaV.GetDoubleValue() / 2;

            double burnTime = 0;
            double halfBurnTime = 0;

            double lastStageBurnTime = 0;
            for (int i = vacStats.Length - 1; i >= 0 && dvLeft > 0; i--) {
                var s = vacStats[i];
                if (s.deltaV <= 0 || s.startThrust <= 0) {
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
                double stageAvgAccel = s.startThrust / ((s.startMass + stageBurnFinalMass) / 2d);

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
    }
}
