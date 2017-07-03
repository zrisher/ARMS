using SEPC.Components.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Platform;
using Sandbox.Game.World;
using VRage.ObjectBuilders;
using VRage.Plugins;

using SEPC.Components;
using Rynchodon.Settings;

using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;


namespace Rynchodon
{

	/// <summary>
	/// Loaded with a Session and persists until it's closed
	/// </summary>
	[IsSessionComponent(groupId: (int)Groups.Loader)]
	class Loader
	{
		public enum Groups : int
		{
			Loader = -1000,
			Network = -999,
			Settings = -998,
		}

		public enum InitGroupOrder : int
		{
			ThreadTracker = 1,
			MainLock = 2,
		}

		private static MySessionComponentBase FindSteamComponent()
		{
			foreach (MyObjectBuilder_Checkpoint.ModItem mod in MyAPIGateway.Session.Mods)
				if (mod.PublishedFileId == 363880940uL || mod.Name == "ARMS")
				{
					Logger.DebugLog("ARMS mod: FriendlyName: " + mod.FriendlyName + ", Name: " + mod.Name + ", Published ID: " + mod.PublishedFileId);
					MySessionComponentBase component = Mods.FindModSessionComponent(mod.Name, "SteamShipped", "SteamShipped.Notify");
					if (component == null)
					{
						Logger.AlwaysLog($"Failed to find Session Component.", Logger.severity.ERROR);
						continue;
					}
					return component;
				}

			Logger.AlwaysLog("Failed to find mod", Logger.severity.ERROR);
			return null;
		}

		public Loader()
		{
			// Only load if the steam version has been loaded too.
			var steamComponent = FindSteamComponent();
			if (steamComponent == null)
				return;

			// Cancel the Plugin Missing notification from steam code and provide a success notification if DEBUG
			steamComponent.GetType().GetField("HasNotified").SetValue(steamComponent, true);
			Logger.DebugNotify("ARMS DEBUG build loading.", 10000, Logger.severity.INFO);

		}

		public OnSettingsLoaded()
		{
			// 				ComponentSession.RegisterComponentGroup((int)Groups.Autopilot);

		}

		[OnSessionEvent("ARMS.ServerSettingsLoaded")]
		public void LoadWithSettings()
		{
			ComponentSession.RegisterComponentGroup(1);
			ComponentSession.RegisterComponentGroup(2);
		}


	}
}
