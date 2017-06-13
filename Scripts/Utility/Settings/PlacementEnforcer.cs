using Rynchodon.Update.Components.Attributes;
using Rynchodon.Settings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.ObjectBuilders;

namespace Rynchodon.Utility.Settings
{
	[IsSessionComponent(RunLocation.Both, groupId: 1, order: 13)]
	public class PlacementEnforcer
	{
		public PlacementEnforcer()
		{
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowAutopilot))
			{
				Logger.AlwaysLog("Disabling autopilot blocks", Logger.severity.INFO);
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Cockpit), "Autopilot-Block_Large")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Cockpit), "Autopilot-Block_Small")).Enabled = false;
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowGuidedMissile))
			{
				Logger.AlwaysLog("Disabling guided missile blocks", Logger.severity.INFO);
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_R12VP_Launcher")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_R8EA_Launcher")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "Souper_B3MP_Launcher")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "Souper_Missile_Defense_Turret")).Enabled = false;
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowHacker))
			{
				Logger.AlwaysLog("Disabling hacker blocks", Logger.severity.INFO);
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_LandingGear), "ARMS_SmallHackerBlock")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_LandingGear), "ARMS_LargeHackerBlock")).Enabled = false;
			}
			if (!ServerSettings.GetSetting<bool>(ServerSettings.SettingName.bAllowRadar))
			{
				Logger.AlwaysLog("Disabling radar blocks", Logger.severity.INFO);
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "LargeBlockRadarRynAR")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "SmallBlockRadarRynAR")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "Radar_A_Large_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "Radar_A_Small_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadar_Large_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadar_Small_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadarOffset_Large_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "PhasedArrayRadarOffset_Small_Souper07")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "AWACSRadarLarge_JnSm")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Beacon), "AWACSRadarSmall_JnSm")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "AP_Radar_Jammer_Large")).Enabled = false;
				MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_RadioAntenna), "AP_Radar_Jammer_Small")).Enabled = false;
			}

		}
	}
}
