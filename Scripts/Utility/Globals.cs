using SEPC.Components.Attributes;
using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Rynchodon
{
	[IsSessionComponent(isStatic: true, order: int.MinValue)]
	public static class Globals
	{

		private class StaticVariables
		{
			public readonly Vector3I[] NeighboursOne = new Vector3I[]
			{
				new Vector3I(0, 0, -1),
				new Vector3I(0, 0, 1),
				new Vector3I(0, -1, 0),
				new Vector3I(0, 1, 0),
				new Vector3I(-1, 0, 0),
				new Vector3I(1, 0, 0)
			};
			public readonly Vector3I[] NeighboursTwo = new Vector3I[]
			{
				new Vector3I(0, 1, -1),
				new Vector3I(1, 0, -1),
				new Vector3I(0, -1, -1),
				new Vector3I(-1, 0, -1),
				new Vector3I(1, 1, 0),
				new Vector3I(1, -1, 0),
				new Vector3I(-1, -1, 0),
				new Vector3I(-1, 1, 0),
				new Vector3I(0, 1, 1),
				new Vector3I(1, 0, 1),
				new Vector3I(0, -1, 1),
				new Vector3I(-1, 0, 1),
			};
			public readonly Vector3I[] NeighboursThree = new Vector3I[]
			{
				new Vector3I(1, 1, -1),
				new Vector3I(1, -1, -1),
				new Vector3I(-1, -1, -1),
				new Vector3I(-1, 1, -1),
				new Vector3I(1, 1, 1),
				new Vector3I(1, -1, 1),
				new Vector3I(-1, -1, 1),
				new Vector3I(-1, 1, 1),
			};
		}

		#region SE Constants

		public const int UpdatesPerSecond = 60;
		public const float PlayerBroadcastRadius = 200f;

		#endregion SE Constants

		/// <summary>Duration of one update in seconds.</summary>
		public const float UpdateDuration = 1f / (float)UpdatesPerSecond;

		public const double UpdatesToTicks = (double)TimeSpan.TicksPerSecond / (double)UpdatesPerSecond;

		public static readonly Random Random = new Random();

		private static DateTime LastUpdateAt;

		/// <summary>The number of updates since mod started.</summary>
		public static ulong UpdateCount;

		/// <summary>Simulation speed of game based on time between updates.</summary>
		public static float SimSpeed = 1f;

		/// <summary>Elapsed time based on number of updates i.e. not incremented while paused.</summary>
		public static TimeSpan ElapsedTime
		{
			get { return new TimeSpan((long)(UpdateCount * UpdatesToTicks)); }
		}

		public static long ElapsedTimeTicks
		{
			get { return (long)(UpdateCount * UpdatesToTicks); }
		}

		public static readonly MyDefinitionId Electricity = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

		private static StaticVariables Static = new StaticVariables();

		public static bool WorldClosed;

		public static IEnumerable<Vector3I> NeighboursOne { get { return Static.NeighboursOne; } }

		public static IEnumerable<Vector3I> NeighboursTwo { get { return Static.NeighboursTwo; } }

		public static IEnumerable<Vector3I> NeighboursThree { get { return Static.NeighboursThree; } }

		public static IEnumerable<Vector3I> Neighbours
		{
			get
			{
				foreach (Vector3I vector in Static.NeighboursOne)
					yield return vector;
				foreach (Vector3I vector in Static.NeighboursTwo)
					yield return vector;
				foreach (Vector3I vector in Static.NeighboursThree)
					yield return vector;
			}
		}

		private static List<MyVoxelMap> m_voxelMaps = new List<MyVoxelMap>();
		private static List<MyPlanet> m_planets = new List<MyPlanet>();
		private static FastResourceLock lock_voxels = new FastResourceLock();

		[OnSessionUpdate]
		public static void Update()
		{
			UpdateCount++;
			float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
			if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
				SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
			//Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
			LastUpdateAt = DateTime.UtcNow;
		}

		[OnSessionUpdate(100)]
		public static void Update100()
		{
			using (lock_voxels.AcquireExclusiveUsing())
			{
				m_voxelMaps.Clear();
				m_planets.Clear();
				foreach (MyVoxelBase voxel in MySession.Static.VoxelMaps.Instances)
				{
					if (voxel is MyVoxelMap)
						m_voxelMaps.Add((MyVoxelMap)voxel);
					else if (voxel is MyPlanet)
						m_planets.Add((MyPlanet)voxel);
				}
			}
		}

		public static IEnumerable<MyPlanet> AllPlanets()
		{
			using (lock_voxels.AcquireSharedUsing())
				foreach (MyPlanet planet in m_planets)
					yield return planet;
		}

		public static IEnumerable<MyVoxelMap> AllVoxelMaps()
		{
			using (lock_voxels.AcquireSharedUsing())
				foreach (MyVoxelMap voxel in m_voxelMaps)
					yield return voxel;
		}

		[OnStaticSessionComponentInit]
		private static void Init()
		{
			LastUpdateAt = DateTime.UtcNow;
			UpdateCount = 0;
			WorldClosed = false;
		}

		[OnSessionClose(order: int.MaxValue - 1)]
		private static void Unload()
		{
			WorldClosed = true;
			using (lock_voxels.AcquireExclusiveUsing())
			{
				m_voxelMaps.Clear();
				m_planets.Clear();
			}
		}

		public static Vector3D Invalid = Vector3.Invalid;

		public static void Swap<T>(ref T first, ref T second)
		{
			T temp = first;
			first = second;
			second = temp;
		}

		public static MyStringId WeaponLaser = MyStringId.GetOrCompute("WeaponLaser");

	}
}
