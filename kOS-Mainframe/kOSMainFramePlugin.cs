using KramaxReloadExtensions;
using kOSMainframe.Debugging;

namespace kOSMainframe {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class kOSMainFramePlugin : ReloadableMonoBehaviour {

        public kOSMainFramePlugin() {
            Logging.Debug("Mainframe started");
        }

        void Awake() {
            Logging.Debug("Mainframe woke up");
        }

        void Start() {
            Logging.Debug("Mainframe starts");
        }

#if DEBUG
        private DebuggingControl debuggingWindow = new DebuggingControl();
        private int instanceId = -1;

        void OnGUI() {
            if(debuggingWindow == null) {
                return;
            }

            if (instanceId < 0) {
                instanceId = GetInstanceID();
            }

            debuggingWindow.Draw(instanceId);
        }
#endif

        void OnDestroy() {
            Logging.Debug("Mainframe destroy");
        }
    }
}
