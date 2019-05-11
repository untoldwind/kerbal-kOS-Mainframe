using KramaxReloadExtensions;
using kOSMainframe.UnityToolbag;
using kOSMainframe.Debugging;
using UnityEngine;
using kOSMainframe.UI;

namespace kOSMainframe {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class kOSMainFramePlugin : ReloadableMonoBehaviour {
        private static kOSMainFramePlugin instance;
        private Dispatcher dispatcher;

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
            dispatcher = AddComponent(typeof(Dispatcher)) as Dispatcher;
        }

        void Start() {
            AppLauncher.Start(this.gameObject);
            Logging.Debug("Mainframe starts");

#if DEBUG
            DebuggingControl.Start();
#endif
        }

        void OnDestroy() {
            AppLauncher.OnDestroy(this.gameObject);
            instance = null;
            Object.Destroy(dispatcher);
            Logging.Debug("Mainframe destroy");
        }
    }
}
