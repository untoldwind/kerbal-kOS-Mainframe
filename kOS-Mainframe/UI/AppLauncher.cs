using KSP.UI.Screens;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace kOSMainframe.UI {
    public static class AppLauncher {
        private static ApplicationLauncherButton btnLauncher;
        private static PopupDialog popupDialog;
        private static DialogGUIVerticalLayout manouverList;

        public static void Start(GameObject gameObject) {
            if (btnLauncher == null) {
                Texture2D appIcon = GameDatabase.Instance.GetTexture("kOS-Mainframe/Assets/ToolbarIcon", false);
                btnLauncher = ApplicationLauncher.Instance.AddModApplication(OnToggleTrue, OnToggleFalse, null, null, null, null,
                              ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                              appIcon);
            }
        }

        public static void OnDestroy(GameObject gameObject) {
            if (btnLauncher != null) {
                ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
                btnLauncher = null;
            }
        }

        private static void OnToggleTrue() {
            if(popupDialog == null) {
                List<DialogGUIBase> dialog = new List<DialogGUIBase>();

                dialog.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.UpperCenter, new DialogGUIBase[] {
                    new DialogGUILabel("Transmit data to the selected vessel:", false, false)
                }));

                manouverList = new DialogGUIVerticalLayout(10, 10, new DialogGUIBase[0]);
                dialog.Add(new DialogGUIScrollList(Vector2.one, false, true, manouverList));

                popupDialog = PopupDialog.SpawnPopupDialog(
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  new MultiOptionDialog("MainFrame dialog", "", "MainFrame",
                                                        UISkinManager.defaultSkin, new Rect(0.5f, 0.5f, 300, 300), dialog.ToArray()),
                                  false, UISkinManager.defaultSkin, false);
            }
        }

        private static void OnToggleFalse() {
            if(popupDialog != null) {
                popupDialog.Dismiss();
            }
        }

        private static void AddManouver() {
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(manouverList.uiItem.gameObject.transform);
            List<DialogGUIBase> manouvers = manouverList.children;
            // manouvers.Add(createManouver());
            // manouvers.Last().Create(ref stack, UISkinManager.defaultSkin);
        }

        private static void RemoveManouver(int removeIdx) {
            List<DialogGUIBase> manouvers = manouverList.children;

            DialogGUIBase thisChild = manouvers.ElementAt(removeIdx);
            manouvers.RemoveAt(removeIdx);
            thisChild.uiItem.gameObject.DestroyGameObjectImmediate();
        }
    }
}
