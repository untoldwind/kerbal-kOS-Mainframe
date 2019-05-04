using System;
using System.Collections.Generic;
using System.Linq;
using kOS;
using kOS.Serialization;
using kOS.Suffixed.Part;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOSMainframe.VesselExtra {
    //A Stats struct describes the result of the simulation over a certain interval of time (e.g., one stage)
    [kOS.Safe.Utilities.KOSNomenclature("StageStats")]
    public class StageStats : Structure {
        protected readonly SharedObjects shared;
        public double startMass;
        public double endMass;
        public double thrust;
        public double stageBurnTime;
        public double deltaV;

        public double fuelMass;
        public double isp;
        public double twr;
        public double stageMass;
        public double currentStageMass;

        public List<Part> parts = new List<Part>();

        public StageStats(SharedObjects sharedObj) {
            this.shared = sharedObj;
            InitializeSuffixes();
        }

        //Append joins two Stats describing adjacent intervals of time into one describing the combined interval
        public StageStats Append(StageStats s) {
            return new StageStats(this.shared) {
                startMass = this.startMass,
                endMass = s.endMass,
                fuelMass = startMass - s.endMass,
                thrust = this.thrust,
                stageBurnTime = this.stageBurnTime + (s.stageBurnTime < float.MaxValue && !double.IsInfinity(s.stageBurnTime) ? s.stageBurnTime : 0),
                deltaV = this.deltaV + s.deltaV,
                parts = this.parts,
                isp = this.startMass == s.endMass ? 0 : (this.deltaV + s.deltaV) / (9.80665f * Math.Log(this.startMass / s.endMass))
            };
        }

        private void InitializeSuffixes() {
            AddSuffix("START_MASS", new Suffix<ScalarDoubleValue>(() => startMass));
            AddSuffix("END_MASS", new Suffix<ScalarDoubleValue>(() => endMass));
            AddSuffix("THRUST", new Suffix<ScalarDoubleValue>(() => thrust));
            AddSuffix("STAGE_BURN_TIME", new Suffix<ScalarDoubleValue>(() => stageBurnTime));
            AddSuffix("DELTA_V", new Suffix<ScalarDoubleValue>(() => deltaV));
            AddSuffix("FUEL_MASS", new Suffix<ScalarDoubleValue>(() => fuelMass));
            AddSuffix("ISP", new Suffix<ScalarDoubleValue>(() => isp));
            AddSuffix("TWR", new Suffix<ScalarDoubleValue>(() => twr));
            AddSuffix("STAGE_MASS", new Suffix<ScalarDoubleValue>(() => stageMass));
            AddSuffix("CURRENT_STAGE_MASS", new Suffix<ScalarDoubleValue>(() => stageMass));
        }

        public override String ToString() {
            return "StageStats(startMass=" + startMass +
                   ",endMass=" + endMass +
                   ",thrust=" + thrust +
                   ",stageBurnTime=" + stageBurnTime +
                   ",deltaV=" + deltaV +
                   ",resourceMass=" + fuelMass +
                   ",isp=" + isp +
                   ",twr=" + twr +
                   ",stageMass=" + stageMass +
                   ",nParts=" + parts.Count() +
                   ")";
        }
    }
}
