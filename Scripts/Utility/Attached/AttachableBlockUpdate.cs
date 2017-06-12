using VRage.Game.ModAPI;

namespace Rynchodon.Attached
{
	public abstract class AttachableBlockUpdate : AttachableBlockBase
	{

		public AttachableBlockUpdate(IMyCubeBlock block, AttachedGrid.AttachmentKind kind)
			: base(block, kind)
		{ }

		public virtual void Update()
		{
			IMyCubeBlock partner = GetPartner();
			if (partner == null)
				Detach();
			else
				Attach(partner);
		}

		public override string ToString()
		{ return "AttachableBlock:" + myBlock.DisplayNameText; }

		protected abstract IMyCubeBlock GetPartner();
	}
}
