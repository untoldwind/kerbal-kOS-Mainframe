using System;

namespace kOSMainframe {
    public static class Logging {
        public static void Debug(String message, params object[] args) {
            UnityEngine.Debug.Log("kOS-MainFrame [Debug]: " + String.Format(message, args));
        }

        public static void DumpOrbit(String name, Orbit o) {
            Debug("Orbit {0}: body={1} sMa={2} sma={3} PeA={4} ApA={5} E={6}", name, o.referenceBody, o.semiMajorAxis, o.semiMinorAxis, o.PeA, o.ApA, o.eccentricity);
        }
    }
}
