using KramaxReloadExtensions;
using kOSMainframe.Debugging;

namespace kOSMainframe
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class kOSMainFramePlugin : ReloadableMonoBehaviour
    {
        private DebuggingControl debuggingWindow = new DebuggingControl();
        private int instanceId = -1;

		public kOSMainFramePlugin()
        {
			Logging.Debug("Mainframe started");
        }

		void Awake() {
            Logging.Debug("Mainframe woke up");
		}

		void Start() {
            Logging.Debug("Mainframe starts");
		}

        void OnGUI()
        {
            if(debuggingWindow == null)
            {
                return;
            }

            if (instanceId < 0)
            {
                instanceId = GetInstanceID();
            }

            debuggingWindow.Draw(instanceId);
        }

        void OnDestroy()
        {
            Logging.Debug("Mainframe destroy");
        }
    }
}
