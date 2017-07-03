using SEPC.Components.Attributes;
using Rynchodon.Weapons.SystemDisruption;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Rynchodon.Attached
{
	[IsEntityComponent(typeof(IMyCubeBlock), typeof(MyObjectBuilder_LandingGear), groupId: 1, order: 2)]
	public class LandingGear : AttachableBlockBase
	{
		private MyLandingGear myGear { get { return (MyLandingGear)myBlock; } }

		public LandingGear(IMyCubeBlock block)
			: base (block, AttachedGrid.AttachmentKind.LandingGear)
		{
			this.myGear.LockModeChanged += MyGear_LockModeChanged;

			IMyCubeGrid attached = myGear.GetAttachedEntity() as IMyCubeGrid;
			if (attached != null)
				Attach(attached);

			myGear.OnClosing += myGear_OnClosing;
		}

		[EntityComponentIf]
		public static bool NotHacker(IMyCubeBlock block)
		{
			return !Hacker.IsHacker(block);
		}

		private void myGear_OnClosing(IMyEntity obj)
		{
			this.myGear.LockModeChanged -= MyGear_LockModeChanged;
		}

		private void MyGear_LockModeChanged(Sandbox.Game.Entities.Interfaces.IMyLandingGear gear, SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode oldMode)
		{
			try
			{
				if (myGear.IsLocked)
				{
					Logger.DebugLog("Is now attached to: " + myGear.GetAttachedEntity().getBestName(), Logger.severity.DEBUG, primaryState: myGear.CubeGrid.nameWithId(), secondaryState: myGear.nameWithId());
					IMyCubeGrid attached = myGear.GetAttachedEntity() as IMyCubeGrid;
					if (attached != null)
						Attach(attached);
					else
						Detach();
				}
				else
				{
					Logger.DebugLog("Is now disconnected", Logger.severity.DEBUG, primaryState: myGear.CubeGrid.nameWithId(), secondaryState: myGear.nameWithId());
					Detach();
				}
			}
			catch (Exception ex)
			{
				Logger.AlwaysLog("Exception: " + ex, Logger.severity.ERROR, primaryState: myGear.CubeGrid.nameWithId(), secondaryState: myGear.nameWithId());
				Logger.DebugNotify("LandingGear encountered an exception", 10000, Logger.severity.ERROR);
			}
		}
	}
}
