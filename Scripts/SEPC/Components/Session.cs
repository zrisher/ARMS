using Rynchodon;
using Rynchodon.Utility.Collections;
using Sandbox.ModAPI;
using SEPC.Components.Stores;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace SEPC.Components
{
	/// <summary>
	/// Holds a ComponentStore and propagates events, updates, and entity changes to it.
	/// Provides thread-safe component registration and event raising.
	/// Receives updates and restarts from a standard SE MySessionComponent.
	/// </summary>
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class ComponentSession : MySessionComponentBase
	{
		private enum SessionStatus : byte { NotInitialized, Initialized, Terminated }

		private static ComponentCollectionStore ComponentStore;
		private static LockedDeque<Action> ExternalRegistrations;
		private static SessionStatus Status;

		#region Thread-safe registration and event raising

		/// <param name="unregisterOnClosing">Leave as null if you plan on manually unregistering</param>
		public static void RegisterUpdateHandler(uint frequency, Action toInvoke, IMyEntity unregisterOnClosing = null)
		{
			ExternalRegistrations?.AddTail(() => {
				ComponentStore.AddUpdateHandler(frequency, toInvoke);
				if (unregisterOnClosing != null)
					unregisterOnClosing.OnClosing += (entity) => ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
			});
		}

		public static void UnregisterUpdateHandler(uint frequency, Action toInvoke)
		{
			ExternalRegistrations?.AddTail(() => {
				ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
			});
		}

		public static void RegisterComponentGroup(int group)
		{
			var collection = ComponentRegistrar.GetComponents(Assembly.GetCallingAssembly(), group);
			ExternalRegistrations?.AddTail(() => {
				ComponentStore.TryAddCollection(collection);
			});
		}

		public static void RaiseSessionEvent(string eventName)
		{
			ExternalRegistrations?.AddTail(() => {
				ComponentStore.RaiseSessionEvent(eventName); ;
			});
		}

		public static void RaiseEntityEvent(string eventName, IMyEntity entity)
		{
			ExternalRegistrations?.AddTail(() => {
				ComponentStore.RaiseEntityEvent(eventName, entity); ;
			});
		}

		#endregion
		#region Lifecycle

		public ComponentSession() : base() { }

		/// <summary>
		/// Resets the status for a new session.
		/// </summary>
		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			base.Init(sessionComponent);
			Status = SessionStatus.NotInitialized;
		}

		/// <summary>
		/// Propagates the Save event to components
		/// </summary>
		public override void SaveData()
		{
			base.SaveData();
			RaiseSessionEvent(ComponentEventNames.SessionSave); // TODO call this when saved on exit too
		}

		/// <summary>
		/// Initializes once MySession is ready, runs external actions, and propagates updates to the store.
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			try
			{
				if (Status == SessionStatus.Terminated)
					return;

				if (Status == SessionStatus.NotInitialized)
				{
					Initialize();
					return;
				}

				if (ExternalRegistrations.Count != 0)
				{
					ExternalRegistrations.PopHeadInvokeAll();
				}

				ComponentStore.Update();
			}
			catch (Exception error)
			{
				Logger.AlwaysLog("Error: " + error, Logger.severity.FATAL);
				Status = SessionStatus.Terminated;
			}
		}

		/*
		// Not called when force-closed
		protected override void UnloadData()
		{
			base.UnloadData();
		}
		*/

		private void Initialize()
		{
			// return unless session initialized
			if (MyAPIGateway.CubeBuilder == null || MyAPIGateway.Entities == null || MyAPIGateway.Multiplayer == null || MyAPIGateway.Parallel == null
				|| MyAPIGateway.Players == null || MyAPIGateway.Session == null || MyAPIGateway.TerminalActionsHelper == null || MyAPIGateway.Utilities == null ||
				(!MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.Player == null))
				return;

			var runningOn = !MyAPIGateway.Multiplayer.MultiplayerActive ? RunLocation.Both : (MyAPIGateway.Multiplayer.IsServer ? RunLocation.Server : RunLocation.Client);

			Logger.DebugLog($"Initializing. RunningOn: {runningOn}, SessionName: {MyAPIGateway.Session.Name}, SessionPath: {MyAPIGateway.Session.CurrentPath}", Logger.severity.INFO);

			ComponentStore = new ComponentCollectionStore(runningOn);
			ExternalRegistrations = new LockedDeque<Action>();

			MyAPIGateway.Entities.OnCloseAll += Terminate;
			MyAPIGateway.Entities.OnEntityAdd += EntityAdded;

			var entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);
			foreach (IMyEntity entity in entities)
				EntityAdded(entity);

			foreach (var collection in ComponentRegistrar.GetInitComponents())
				ComponentStore.TryAddCollection(collection);

			Status = SessionStatus.Initialized;
		}

		private void Terminate()
		{
			Logger.DebugLog("Terminating");

			if (ComponentStore != null)
				ComponentStore.RaiseSessionEvent(ComponentEventNames.SessionClose);

			MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
			MyAPIGateway.Entities.OnCloseAll -= Terminate;

			// clear fields in case SE doesn't clean up properly
			ComponentStore = null;
			ExternalRegistrations = null;

			Status = SessionStatus.Terminated;
		}

		#endregion
		#region Entity Add/Remove Handlers

		private void EntityAdded(IMyEntity entity)
		{
			ComponentStore.AddEntity(entity);
			entity.OnClosing += EntityRemoved;

			// CubeBlocks aren't included in Entities.OnEntityAdd
			IMyCubeGrid asGrid = entity as IMyCubeGrid;
			if (asGrid != null && asGrid.Save)
			{
				var blocksInGrid = new List<IMySlimBlock>();
				asGrid.GetBlocks(blocksInGrid, slim => slim.FatBlock != null);
				foreach (IMySlimBlock slim in blocksInGrid)
					BlockAdded(slim);

				asGrid.OnBlockAdded += BlockAdded;
			}
		}

		private void BlockAdded(IMySlimBlock entity)
		{
			IMyCubeBlock asBlock = entity.FatBlock;
			if (asBlock != null)
				EntityAdded(asBlock);
		}

		private void EntityRemoved(IMyEntity entity)
		{
			// Attached to entities themselves, so can be called after terminated
			if (ComponentStore != null)
				ComponentStore.RemoveEntity(entity);

			entity.OnClosing -= EntityRemoved;

			// CubeBlocks aren't included in Entities.OnEntityAdd
			IMyCubeGrid asGrid = entity as IMyCubeGrid;
			if (asGrid != null)
				asGrid.OnBlockAdded -= BlockAdded;
		}

		#endregion
	}
}
