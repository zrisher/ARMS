using SEPC.Components;
using SEPC.Components.Attributes;
using Sandbox.Game.World;
using VRage;
using VRageMath;

namespace Rynchodon
{
	[IsSessionComponent(RunLocation.Server)]
	public class SunProperties
	{

		private static SunProperties Instance;

		private Vector3 mySunDirection;
		private readonly FastResourceLock lock_mySunDirection = new FastResourceLock();

		public SunProperties()
		{
			Instance = this;
		}

		[OnSessionUpdate(10)]
		public void Update10()
		{
			using (Instance.lock_mySunDirection.AcquireExclusiveUsing())
				mySunDirection = MySector.DirectionToSunNormalized;
		}

		public static Vector3 SunDirection
		{
			get
			{
				using (Instance.lock_mySunDirection.AcquireSharedUsing())
					return Instance.mySunDirection;
			}
		}

	}
}
