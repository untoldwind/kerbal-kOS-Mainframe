using System;

namespace kOSMainframe.VesselExtra
{
    public class VesselInfo
    {
		private Vessel vessel;

		public VesselInfo(Vessel vessel)
        {
			this.vessel = vessel;
        }

		public double RCSDeltaVVacuum()
        {
            // Use the average specific impulse of all RCS parts.
            double totalIsp = 0;
            int numThrusters = 0;
            double gForRCS = 9.81;

            double monopropMass = vessel.TotalResourceMass("MonoPropellant");

            foreach (ModuleRCS pm in vessel.GetModules<ModuleRCS>())
            {
                totalIsp += pm.atmosphereCurve.Evaluate(0);
                numThrusters++;
                gForRCS = pm.G;
            }

            double m0 = VesselMass();
            double m1 = m0 - monopropMass;
            if (numThrusters == 0 || m1 <= 0) return 0;
            double isp = totalIsp / numThrusters;
            return isp * gForRCS * Math.Log(m0 / m1);
        }

        public double MonoPropellantMass()
        {
            return vessel.TotalResourceMass("MonoPropellant");
        }

        public double VesselMass()
        {
			return vessel.totalMass;
        }
    }
}
