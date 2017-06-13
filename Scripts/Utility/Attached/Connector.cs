using Rynchodon.Update.Components.Attributes;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Rynchodon.Attached
{
	[IsEntityComponent(typeof(IMyCubeBlock), typeof(MyObjectBuilder_ShipConnector), RunLocation.Both, groupId: 1, order: 2)]
	public class Connector : AttachableBlockUpdate
	{
		public Connector(IMyCubeBlock block)
			: base(block, AttachedGrid.AttachmentKind.Connector)
		{ }

		protected override IMyCubeBlock GetPartner()
		{
			IMyShipConnector myConn = (IMyShipConnector)myBlock;
			if (myConn.Status != Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected)
				return null;

			return myConn.OtherConnector;
		}

		[OnEntityUpdate(10)]
		public override void Update()
		{
			base.Update();
		}
	}
}
