using System;
using System.Collections.Generic;
using System.Reflection;
using Rynchodon.Threading;
using Rynchodon.Update.Components.Attributes;
using Rynchodon.Update.Components.Stores;
using Rynchodon.Update.Components.Registration;
using Rynchodon.Utility;
using Rynchodon.Utility.Collections;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Rynchodon.Update
{
	/// <summary>
	/// <para>Completely circumvents MyGameLogicComponent to avoid conflicts, and offers a bit more flexibility.</para>
	/// <para>Will send updates after creating object, until object is closing.</para>
	/// <para>Creation of script objects is delayed until MyAPIGateway fields are filled.</para>
	/// <para>If an update script throws an exception, it will stop receiving updates.</para>
	/// </summary>
	/// <remarks>
	/// <para>Comparision to MyGameLogicComponent</para>
	/// <para>    Disadvantages of MyGameLogicComponent:</para>
	/// <para>        NeedsUpdate can be changed by the game after you set it, so you have to work around that. i.e. For remotes it is set to NONE and UpdatingStopped() never gets called.</para>
	/// <para>        Scripts can get created before MyAPIGateway fields are filled, which can be a serious problem for initializing.</para>
	/// <para> </para>
	/// <para>    Advantages of UpdateManager:</para>
	/// <para>        Scripts can be registered conditionally. MyGameLogicComponent now supports subtypes but UpdateManager can technically do any condition.</para>
	/// <para>        Elegant handling of Exception thrown by script.</para>
	/// <para>        You don't have to create a new object for every entity if you don't need one.</para>
	/// <para>        You can set any update frequency you like without having to create a counter.</para>
	/// <para>        UpdateManager supports characters and players.</para>
	/// </remarks>
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class UpdateManager : MySessionComponentBase
	{
		private enum Status : byte { Not_Initialized, Initialized, Terminated }

		private static UpdateManager Instance;

		#region Thread-safe registration and event raising

		/// <param name="unregisterOnClosing">Leave as null if you plan on using Unregister at all.</param>
		public static void Register(uint frequency, Action toInvoke, IMyEntity unregisterOnClosing = null)
		{
			Instance?.ExternalRegistrations?.AddTail(() => {
				Instance.ComponentStore.AddUpdateHandler(frequency, toInvoke);
				if (unregisterOnClosing != null)
					unregisterOnClosing.OnClosing += (entity) => Instance.ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
			});
		}

		public static void Unregister(uint frequency, Action toInvoke)
		{
			Instance?.ExternalRegistrations?.AddTail(() => {
				Instance.ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
			});
		}

		public static void RegisterComponentGroup(int group)
		{
			var collection = LogicComponentRegistrar.GetComponents(Assembly.GetCallingAssembly(), group);
			Instance?.ExternalRegistrations?.AddTail(() => {
				Instance.ComponentStore.AddCollection(collection);
			});
		}

		public static void RaiseSessionEvent(string eventName)
		{
			Instance?.ExternalRegistrations?.AddTail(() => {
				Instance.ComponentStore.RaiseSessionEvent(eventName); ;
			});
		}

		public static void RaiseEntityEvent(string eventName, IMyEntity entity)
		{
			Instance?.ExternalRegistrations?.AddTail(() => {
				Instance.ComponentStore.RaiseEntityEvent(eventName, entity); ;
			});
		}

		#endregion

		private LockedDeque<Action> ExternalRegistrations = new LockedDeque<Action>();
		private DateTime LastUpdateAt;
		private ComponentCollectionStore ComponentStore;
		private Status ManagerStatus;

		private Logable Log { get { return new Logable("", ManagerStatus.ToString()); } }

		public UpdateManager()
		{
			ThreadTracker.SetGameThread();
			Instance = this;
			MainLock.MainThread_AcquireExclusive();
		}

		#region Lifecycle

		public void Init()
		{
			try
			{
				if (MyAPIGateway.CubeBuilder == null || MyAPIGateway.Entities == null || MyAPIGateway.Multiplayer == null || MyAPIGateway.Parallel == null
					|| MyAPIGateway.Players == null || MyAPIGateway.Session == null || MyAPIGateway.TerminalActionsHelper == null || MyAPIGateway.Utilities == null)
					return;

				if (!MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.Player == null)
					return;

				var runningOn = !MyAPIGateway.Multiplayer.MultiplayerActive ? RunLocation.Both : (MyAPIGateway.Multiplayer.IsServer ? RunLocation.Server : RunLocation.Client);

				Log.DebugLog($"Initializing UpdateManager. Session: {MyAPIGateway.Session.Name}, Path: {MyAPIGateway.Session.CurrentPath}, RunningOn: {runningOn}", Logger.severity.INFO);
				if (!CheckFinalBuildConstant("IS_OFFICIAL"))
					Log.AlwaysLog("Space Engineers build is UNOFFICIAL; this build is not supported. Version: " + MyFinalBuildConstants.APP_VERSION_STRING, Logger.severity.WARNING);
				else if (CheckFinalBuildConstant("IS_DEBUG"))
					Log.AlwaysLog("Space Engineers build is DEBUG; this build is not supported. Version: " + MyFinalBuildConstants.APP_VERSION_STRING, Logger.severity.WARNING);
				else
					Log.AlwaysLog("Space Engineers version: " + MyFinalBuildConstants.APP_VERSION_STRING, Logger.severity.INFO);
				Logger.DebugNotify("ARMS DEBUG build loaded", 10000, Logger.severity.INFO);

				ComponentStore = new ComponentCollectionStore(runningOn);

				MyAPIGateway.Entities.OnCloseAll += Terminate;
				MyAPIGateway.Entities.OnEntityAdd += AddEntity;

				var entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities);
				foreach (IMyEntity entity in entities)
					AddEntity(entity);

				foreach (var collection in LogicComponentRegistrar.GetInitComponents())
					ComponentStore.AddCollection(collection);

				ManagerStatus = Status.Initialized;
			}
			catch (Exception ex)
			{
				Log.AlwaysLog("Failed to Init(): " + ex, Logger.severity.FATAL);
				ManagerStatus = Status.Terminated;
			}
		}

		private void Terminate()
		{
			if (ManagerStatus != Status.Terminated)
			{
				Log.DebugLog("Terminating Update Manager");
				MainLock.MainThread_ReleaseExclusive();

				if (ManagerStatus == Status.Initialized)
				{
					MyAPIGateway.Entities.OnEntityAdd -= AddEntity;
					MyAPIGateway.Entities.OnCloseAll -= Terminate;
					ComponentStore.RaiseSessionEvent(ComponentEventNames.SessionClose);
				}

				// in case SE doesn't clean up properly, clear all fields
				foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					if (!field.IsLiteral && !field.IsInitOnly)
						field.SetValue(this, null);

				ManagerStatus = Status.Terminated;
			}
		}

		/// <summary>
		/// Initializes if needed, issues updates.
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			MainLock.MainThread_ReleaseExclusive();
			try
			{
				switch (ManagerStatus)
				{
					case Status.Not_Initialized:
						Init();
						return;
					case Status.Terminated:
						return;
				}

				if (ExternalRegistrations.Count != 0)
					try
					{
						Log.DebugLog($"Running {ExternalRegistrations.Count} external registrations.");
						ExternalRegistrations.PopHeadInvokeAll();
					}
					catch (Exception ex)
					{ Log.AlwaysLog("Exception in ExternalRegistrations: " + ex, Logger.severity.ERROR); }

				ComponentStore.Update(Globals.UpdateCount);
			}
			catch (Exception ex)
			{
				Log.AlwaysLog("Exception: " + ex, Logger.severity.FATAL);
				ManagerStatus = Status.Terminated;
			}
			finally
			{
				Globals.UpdateCount++;

				float instantSimSpeed = Globals.UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
				if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
					Globals.SimSpeed = Globals.SimSpeed * 0.9f + instantSimSpeed * 0.1f;
				//Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + Globals.SimSpeed);
				LastUpdateAt = DateTime.UtcNow;

				MainLock.MainThread_AcquireExclusive();
			}
		}

		#endregion
		#region Entity Add/Remove Handlers

		private void AddEntity(IMyEntity entity)
		{
			ComponentStore.AddEntity(entity);
			entity.OnClosing += RemoveEntity;

			// CubeBlocks aren't included in Entities.OnEntityAdd
			IMyCubeGrid asGrid = entity as IMyCubeGrid;
			if (asGrid != null && asGrid.Save)
			{
				var blocksInGrid = new List<IMySlimBlock>();
				asGrid.GetBlocks(blocksInGrid, slim => slim.FatBlock != null);
				foreach (IMySlimBlock slim in blocksInGrid)
					AddSlim(slim);

				asGrid.OnBlockAdded += AddSlim;
				asGrid.OnBlockRemoved += RemoveSlim;
			}
		}

		private void AddSlim(IMySlimBlock entity)
		{
			IMyCubeBlock asBlock = entity.FatBlock;
			if (asBlock != null)
				ComponentStore.AddEntity(asBlock);
			// We skip entity.OnClosing and rely on Grid closing/remove instead to handle grid splits
		}

		private void RemoveEntity(IMyEntity entity)
		{
			if (ManagerStatus == Status.Terminated)
				return;

			ComponentStore.RemoveEntity(entity);
			entity.OnClosing -= RemoveEntity;

			// CubeBlocks aren't included in Entities.OnEntityRemove
			IMyCubeGrid asGrid = entity as IMyCubeGrid;
			if (asGrid != null)
			{
				asGrid.OnBlockAdded -= AddSlim;
				asGrid.OnBlockRemoved -= RemoveSlim;

				var blocksInGrid = new List<IMySlimBlock>();
				asGrid.GetBlocks(blocksInGrid, slim => slim.FatBlock != null);
				foreach (IMySlimBlock slim in blocksInGrid)
					RemoveSlim(slim);
			}
		}

		private void RemoveSlim(IMySlimBlock entity)
		{
			if (ManagerStatus == Status.Terminated)
				return;

			IMyCubeBlock asBlock = entity.FatBlock;
			if (asBlock != null)
				RemoveEntity(asBlock);
			// We skip entity.OnClosing and rely on Grid closing/remove instead to handle grid splits
		}

		#endregion
		#region MySessionComponent Event handlers

		public override void SaveData()
		{
			base.SaveData();
			// TODO test this is called when saved on exit
			RaiseSessionEvent(ComponentEventNames.SessionSave);
			// otherwise use this
			//if (ManagerStatus == Status.Initialized)
			//	Managed.RaiseSessionEvent(ComponentEventNames.SessionSave);
		}

		protected override void UnloadData()
		{
			base.UnloadData();
			//Terminate();
		}

		#endregion

		private bool CheckFinalBuildConstant(string fieldName)
		{
			FieldInfo field = typeof(MyFinalBuildConstants).GetField(fieldName);
			if (field == null)
				throw new NullReferenceException("MyFinalBuildConstants does not have a field named " + fieldName + " or it has unexpected binding");
			return (bool)field.GetValue(null);
		}

	}
}
