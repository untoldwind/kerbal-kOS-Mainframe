using kOS;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;
using kOS.Serialization;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOSMainframe
{
    [kOS.Safe.Utilities.KOSNomenclature("VesselLanding")]
    public class VesselLanding : Structure, IHasSharedObjects
    {
        public SharedObjects Shared { get; set; }
        private readonly Vessel vessel;

        public VesselLanding(SharedObjects sharedObjs)
        {
            Shared = sharedObjs;
            this.vessel = sharedObjs.Vessel;
            InitializeSuffixes();
        }

        public VesselLanding(SharedObjects sharedObjs, VesselTarget vessel)
        {
            Shared = sharedObjs;
            this.vessel = vessel.Vessel;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {

        }
    }
}
