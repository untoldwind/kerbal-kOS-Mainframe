using System;

namespace kOSMainframe {
    public static class Logging {
        public static void Debug(String message, params object[] args) {
            UnityEngine.Debug.Log("kOS-MainFrame [Debug]: " + String.Format(message, args));
        }

        public static void Warning(String message, params object[] args) {
            UnityEngine.Debug.Log("kOS-MainFrame [Warning]: " + String.Format(message, args));
        }

        public static void DumpOrbit(String name, Orbit o) {
            Debug($"Orbit {name}: body={o.referenceBody} inc={o.inclination} ecc={o.eccentricity} sMa={o.semiMajorAxis} sma={o.semiMinorAxis} PeR={o.PeR} ApR={o.ApR} Epoch={o.epoch} LAN={o.LAN} ArgPe={o.argumentOfPeriapsis} meanAtEpoch={o.meanAnomalyAtEpoch}");
        }
    }
}
