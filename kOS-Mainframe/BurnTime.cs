using kOS.Suffixed;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

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
}