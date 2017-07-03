using SEPC.Components;
using SEPC.Components.Attributes;
using Rynchodon.Settings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Rynchodon.Utility.Settings
{
	[IsSessionComponent(groupId: 1, order: 13)]
	public class PlacementEnforcer
	{
		private static void DisableBlock(MyObjectBuilderType type, string subtype)
		{
			MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(type, subtype)).Enabled = false;
		}

		public PlacementEnforcer()
		{
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowAutopilot))
			{
				Logger.AlwaysLog("Disabling autopilot blocks", Logger.severity.INFO);
				DisableBlock(typeof(MyObjectBuilder_Cockpit), "Autopilot-Block_Large");
				DisableBlock(typeof(MyObjectBuilder_Cockpit), "Autopilot-Block_Small");
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowGuidedMissile))
			{
				Logger.AlwaysLog("Disabling guided missile blocks", Logger.severity.INFO);
				DisableBlock(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_R12VP_Launcher");
				DisableBlock(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_R8EA_Launcher");
				DisableBlock(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_B3MP_Launcher");
				DisableBlock(typeof(MyObjectBuilder_LargeMissileTurret), "Souper_Missile_Defense_Turret");
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowHacker))
			{
				Logger.AlwaysLog("Disabling hacker blocks", Logger.severity.INFO);
				DisableBlock(typeof(MyObjectBuilder_LandingGear), "ARMS_SmallHackerBlock");
				DisableBlock(typeof(MyObjectBuilder_LandingGear), "ARMS_LargeHackerBlock");
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowRadar))
			{
				Logger.AlwaysLog("Disabling radar blocks", Logger.severity.INFO);
				DisableBlock(typeof(MyObjectBuilder_Beacon), "AWACSRadarLarge_JnSm");
				DisableBlock(typeof(MyObjectBuilder_Beacon), "AWACSRadarSmall_JnSm");
				DisableBlock(typeof(MyObjectBuilder_Beacon), "LargeBlockRadarRynAR");
				DisableBlock(typeof(MyObjectBuilder_Beacon), "SmallBlockRadarRynAR");
				DisableBlock(typeof(MyObjectBuilder_Beacon), "Radar_A_Large_Souper07");
				DisableBlock(typeof(MyObjectBuilder_Beacon), "Radar_A_Small_Souper07");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "AP_Radar_Jammer_Large");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "AP_Radar_Jammer_Small");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadar_Large_Souper07");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadar_Small_Souper07");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadarOffset_Large_Souper07");
				DisableBlock(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadarOffset_Small_Souper07");
			}

		}
	}
}
