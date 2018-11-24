using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KramaxReloadExtensions;

namespace kOSMainframe.Utils {
    public interface IUIDrawable {
        void Draw(int id);
    }

    public class UIDrawer : ReloadableMonoBehaviour {
        public static UIDrawer Instance {
            get {
                return kOSMainFramePlugin.Instance.GetComponent<UIDrawer>() ?? (kOSMainFramePlugin.Instance.AddComponent(typeof(UIDrawer)) as UIDrawer);
            }
        }

        private int instanceId = -1;
        private List<IUIDrawable> drawables = new List<IUIDrawable>();

        public void OnGUI() {
            if (instanceId < 0) {
                instanceId = GetInstanceID();
            }
            drawables.ForEach(drawable => drawable.Draw(instanceId));
        }

        public void AddDrawable(IUIDrawable drawable) {
            drawables.Add(drawable);
        }
    }
}
