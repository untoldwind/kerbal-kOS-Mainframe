using System;
using System.Collections.Generic;
using System.Linq;
using kOS;
using kOS.Suffixed.Part;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOSMainframe.VesselExtra
{
	//A Stats struct describes the result of the simulation over a certain interval of time (e.g., one stage)
	[kOS.Safe.Utilities.KOSNomenclature("StageStats")]
	public class StageStats : Structure
	{
		public double startMass;
		public double endMass;
		public double startThrust;
		public double maxAccel;
		public double deltaTime;
		public double deltaV;

		public double resourceMass;
		public double isp;
		public double stagedMass;

		public double StartTWR(double geeASL) { return startMass > 0 ? startThrust / (9.80665 * geeASL * startMass) : 0; }
		public double MaxTWR(double geeASL) { return maxAccel / (9.80665 * geeASL); }

		public List<Part> parts;

		private readonly SharedObjects shared;

		public StageStats(SharedObjects shared)
		{
			this.shared = shared;
			InitializeSuffixes();
		}

		//Computes the deltaV from the other fields. Only valid when the thrust is constant over the time interval represented.
		public void ComputeTimeStepDeltaV()
		{
			if (deltaTime > 0 && startMass > endMass && startMass > 0 && endMass > 0)
			{
				deltaV = startThrust * deltaTime / (startMass - endMass) * Math.Log(startMass / endMass);
			}
			else
			{
				deltaV = 0;
			}
		}

		//Append joins two Stats describing adjacent intervals of time into one describing the combined interval
		public StageStats Append(StageStats s)
		{
			return new StageStats(shared)
			{
				startMass = this.startMass,
				endMass = s.endMass,
				resourceMass = startMass - s.endMass,
				startThrust = this.startThrust,
				maxAccel = Math.Max(this.maxAccel, s.maxAccel),
				deltaTime = this.deltaTime + (s.deltaTime < float.MaxValue && !double.IsInfinity(s.deltaTime) ? s.deltaTime : 0),
				deltaV = this.deltaV + s.deltaV,
				parts = this.parts,
				isp = this.startMass == s.endMass ? 0 : (this.deltaV + s.deltaV) / (9.80665f * Math.Log(this.startMass / s.endMass))
			};
		}

		private void InitializeSuffixes()
		{
			AddSuffix("START_MASS", new Suffix<ScalarDoubleValue>(() => startMass));
			AddSuffix("END_MASS", new Suffix<ScalarDoubleValue>(() => endMass));
			AddSuffix("START_THRUST", new Suffix<ScalarDoubleValue>(() => startThrust));
			AddSuffix("MAX_ACCEL", new Suffix<ScalarDoubleValue>(() => maxAccel));
			AddSuffix("DELTA_TIME", new Suffix<ScalarDoubleValue>(() => deltaTime));
			AddSuffix("DELTA_V", new Suffix<ScalarDoubleValue>(() => deltaV));
			AddSuffix("RESOURCE_MASS", new Suffix<ScalarDoubleValue>(() => resourceMass));
			AddSuffix("ISP", new Suffix<ScalarDoubleValue>(() => isp));
			AddSuffix("STAGED_MASS", new Suffix<ScalarDoubleValue>(() => stagedMass));
			AddSuffix("ENGINES", new Suffix<ListValue>(GetEngines));
		}
        
		private ListValue GetEngines()
		{
			return PartValueFactory.Construct(parts.Where(part => part.IsEngine()), shared);
		}

		public override String ToString()
		{
			return "StageStats(startMass=" + startMass +
				",endMass=" + endMass +
				",startThrust=" + startThrust +
				",deltaTime=" + deltaTime +
				",deltaV=" + deltaV +
				",resourceMass=" + resourceMass +
				",isp=" + isp +
				",stagedMass=" + stagedMass +
				",nParts=" + parts.Count() +
				")";
		}
	}
}
