using System;
using UnityEngine;

namespace kOSMainframe
{
	[KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
	public class kOSMainFramePlugin : MonoBehaviour
    {
		public kOSMainFramePlugin()
        {
			Debug.Log("Mainframe started");
        }

		void Awake() {
			Debug.Log("Mainframe woke up");
		}

		void Start() {
			Debug.Log("Mainframe starts");
		}
    }
}
