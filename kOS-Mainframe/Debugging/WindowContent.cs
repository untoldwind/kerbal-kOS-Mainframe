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

    public class Param1Action : IWindowContent
    {
        public string Text
        {
            get;
            set;
        }
        public Action<double> OnClick
        {
            get;
            set;
        }
        public string Value
        {
            get;
            set;
        }

        public Param1Action(string text, double value, Action<double> onClick)
        {
            Text = text;
            Value = value.ToString();
            OnClick = onClick;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            Value = GUILayout.TextField(Value, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(Text))
                OnClick(double.Parse(Value));
            GUILayout.EndHorizontal();
        }
    }
}
