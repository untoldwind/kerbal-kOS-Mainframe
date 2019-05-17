using kOSMainframe.Orbital;
namespace kOSMainframe {
    public interface ILoggingBackend {
        void Log(string line);
    }

    public static class Logging {
        public static ILoggingBackend backend = new UnityLoggingBackend();

        public static void Debug(string message, params object[] args) {
            backend.Log("kOS-MainFrame [Debug]: " + string.Format(message, args));
        }

        public static void Warning(string message, params object[] args) {
            backend.Log("kOS-MainFrame [Warning]: " + string.Format(message, args));
        }

        public static void DumpOrbit(string name, IOrbit o) {
            Debug($"Orbit {name}: body={o.ReferenceBody.Name} inc={o.Inclination} ecc={o.Eccentricity} sma={o.SemiMajorAxis} PeR={o.PeR} ApR={o.ApR} Epoch={o.Epoch} LAN={o.LAN} ArgPe={o.ArgumentOfPeriapsis} meanAtEpoch={o.MeanAnomalyAtEpoch}");
        }
    }

    class UnityLoggingBackend : ILoggingBackend {
        public void Log(string line) {
            UnityEngine.Debug.Log(line);
        }
    }
}
