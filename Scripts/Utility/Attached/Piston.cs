using Rynchodon.Update.Components.Attributes;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Rynchodon.Attached
{
	public static class Piston
	{
		[IsEntityComponent(typeof(IMyCubeBlock), typeof(MyObjectBuilder_ExtendedPistonBase), RunLocation.Both, groupId: 1, order: int.MinValue)]
		public class PistonBase : AttachableBlockUpdate
		{
			public PistonBase(IMyCubeBlock block)
				: base(block, AttachedGrid.AttachmentKind.Piston)
			{ }

			protected override IMyCubeBlock GetPartner()
			{
				IMyPistonBase piston = (IMyPistonBase)myBlock;
				if (piston.IsAttached)
					return piston.Top;
				else
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
