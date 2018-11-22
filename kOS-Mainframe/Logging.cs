using System;

namespace kOSMainframe {
    public static class Logging {
        public static void Debug(String message, params object[] args) {
            UnityEngine.Debug.Log("kOS-MainFrame [Debug]: " + String.Format(message, args));
        }
    }
}
