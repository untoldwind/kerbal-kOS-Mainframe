using UnityEngine;
using System;

namespace kOSMainframe.Debugging {
    public interface IWindowContent {
        void Draw();
    }

    public class Button : IWindowContent {
        public string Text {
            get;
            set;
        }
        public Action OnClick {
            get;
            set;
        }

        public Button(string text, Action onClick) {
            Text = text;
            OnClick = onClick;

        }

        public void Draw() {
            if (GUILayout.Button(Text, GUILayout.ExpandWidth(true)))
                OnClick();
        }
    }
}
