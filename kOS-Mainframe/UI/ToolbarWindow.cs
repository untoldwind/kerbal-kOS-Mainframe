using KramaxReloadExtensions;
using UnityEngine;
using kOS.Screen;
using KSP.UI.Screens;

namespace kOSMainframe.UI {
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class ToolbarWindow : ReloadableMonoBehaviour {
        private ApplicationLauncherButton launcher;

        public static void FirstTimeSetup() {
        }

        void Start() {
            AddLauncher();
            Logging.Debug("Toolbar woke: {0}", ToolbarManager.ToolbarAvailable);
        }

        void OnDestroy() {
            RemoveLauncher();
            Logging.Debug("Toolbar died");
        }


        private void AddLauncher() {
            if (ApplicationLauncher.Ready && launcher == null) {
                Texture2D appIcon = GameDatabase.Instance.GetTexture("kOS-Mainframe/Assets/ToolbarIcon", false);
                Logging.Debug("Icon Path: {0} {1}", appIcon.width, appIcon.height);
                launcher = ApplicationLauncher.Instance.AddModApplication(
                               OnToggleOn, OnToggleOff,
                               null, null,
                               null, null,
                               ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, appIcon
                           );
            }
        }

        private void RemoveLauncher() {
            if (launcher != null) {
                ApplicationLauncher.Instance.RemoveModApplication(launcher);
                launcher = null;
            }
        }

        private void OnToggleOn() {
        }

        private void OnToggleOff() {
        }
    }
}