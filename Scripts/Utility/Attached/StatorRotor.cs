using SEPC.Components.Attributes;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Rynchodon.Attached
{
	public static class StatorRotor
	{
		[IsEntityComponent(typeof(IMyCubeBlock), new[] { typeof(MyObjectBuilder_MotorStator), typeof(MyObjectBuilder_MotorAdvancedStator), typeof(MyObjectBuilder_MotorSuspension) }, groupId: 1, order: 2)]
		public class Stator : AttachableBlockUpdate
		{
			public Stator(IMyCubeBlock block)
				: base(block, AttachedGrid.AttachmentKind.Motor)
			{ }

			protected override IMyCubeBlock GetPartner()
			{
				IMyMotorBase block = (IMyMotorBase)myBlock;
				if (block.IsAttached)
					return block.Top;
				return null;
			}

			[OnEntityUpdate(100)]
			public override void Update()
			{
				base.Update();
			}
		}
	}
}
