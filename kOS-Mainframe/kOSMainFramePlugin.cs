using KramaxReloadExtensions;
using kOSMainframe.Debugging;

namespace kOSMainframe {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class kOSMainFramePlugin : ReloadableMonoBehaviour {
        private static kOSMainFramePlugin instance;

        public static kOSMainFramePlugin Instance {
            get {
                return instance;
            }
        }

        public kOSMainFramePlugin() {
            Logging.Debug("Mainframe started");
        }

        void Awake() {
            Logging.Debug("Mainframe woke up");
            instance = this;
        }

        void Start() {
            Logging.Debug("Mainframe starts");

#if DEBUG
            DebuggingControl.Start();
#endif
        }

        void OnDestroy() {
            instance = null;
            Logging.Debug("Mainframe destroy");
        }
    }
}
