using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS;
using kOS.AddOns;
using kOS.Suffixed;
using kOS.Utilities;
using UnityEngine;

namespace kOSMainframe
{
	[kOSAddon("MainFrame")]
	[kOS.Safe.Utilities.KOSNomenclature("MainFrameAddon")]
	public class kOSMainFrameAddon : kOS.Suffixed.Addon
    {
		public kOSMainFrameAddon(SharedObjects shared) : base(shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
			Debug.Log("MainFrame booting...");
        }

		public override BooleanValue Available()
		{
			return true;
		}
    }
}
