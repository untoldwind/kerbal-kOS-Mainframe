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
            Value = GUILayout.TextField(Value, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            if (GUILayout.Button(Text, GUILayout.ExpandWidth(false)))
                OnClick(double.Parse(Value));
            GUILayout.EndHorizontal();
        }
    }

    public class Param2Action : IWindowContent {
        public string Text {
            get;
            set;
        }
        public Action<double, double> OnClick {
            get;
            set;
        }
        public string Value1 {
            get;
            set;
        }
        public string Value2 {
            get;
            set;
        }

        public Param2Action(string text, double value1, double value2, Action<double, double> onClick) {
            Text = text;
            Value1 = value1.ToString();
            Value2 = value2.ToString();
            OnClick = onClick;
        }

        public void Draw() {
            GUILayout.BeginHorizontal();
            Value1 = GUILayout.TextField(Value1, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            Value2 = GUILayout.TextField(Value2, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            if (GUILayout.Button(Text, GUILayout.ExpandWidth(false)))
                OnClick(double.Parse(Value1), double.Parse(Value2));
            GUILayout.EndHorizontal();
        }
    }
}
