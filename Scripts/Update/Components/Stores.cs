using Rynchodon.Update.Components.Attributes;
using Rynchodon.Update.Components.Descriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Rynchodon.Update.Components.Stores
{
	public abstract class ComponentStore
	{
		protected HashSet<ComponentDescription> Components = new HashSet<ComponentDescription>();
		protected readonly RunLocation RunningOn;
		protected Dictionary<uint, HashSet<ComponentEventAction>> SharedUpdateRegistry;

		public ComponentStore(RunLocation runningOn, Dictionary<uint, HashSet<ComponentEventAction>> sharedUpdateRegistry)
		{
			RunningOn = runningOn;
			SharedUpdateRegistry = sharedUpdateRegistry;
		}

		protected void AddUpdateHandler(ComponentEventAction handler)
		{
			HashSet<ComponentEventAction> handlers;
			if (!SharedUpdateRegistry.TryGetValue(handler.Frequency, out handlers))
				SharedUpdateRegistry[handler.Frequency] = handlers = new HashSet<ComponentEventAction>();
			handlers.Add(handler);
		}

		protected void RemoveUpdateHandler(ComponentEventAction handler)
		{
			HashSet<ComponentEventAction> handlers;
			if (SharedUpdateRegistry.TryGetValue(handler.Frequency, out handlers))
				handlers.Remove(handler);
		}
	}

	/// <summary>
	/// Holds a set of entity components and the entities they're applied to.
	/// Holds a reference to a shared update registry where it attaches and detaches component update events for efficient access.
	/// Provides helpers to add/remove and raise events.
	/// </summary>
	public class EntityComponentStore<TEntity> : ComponentStore where TEntity : IMyEntity
	{
		private readonly Dictionary<TEntity, List<ComponentInstanceDescription>> ComponentInstancesByEntity = new Dictionary<TEntity, List<ComponentInstanceDescription>>();

		public EntityComponentStore(RunLocation runningOn, Dictionary<uint, HashSet<ComponentEventAction>>  sharedUpdateRegistry) : 
			base (runningOn, sharedUpdateRegistry) { }

		public void AddComponent(EntityComponentDescription<TEntity> component)
		{
			//Logger.AlwaysLog($"{component}", Logger.severity.TRACE);
			if (component.ShouldRunOn(RunningOn) && Components.Add(component))
				foreach (var entity in ComponentInstancesByEntity.Keys)
					AddComponentInstance(component, entity);
		}

		public void AddEntity(TEntity entity)
		{
			//Logger.AlwaysLog($"{entity.nameWithId()}", Logger.severity.TRACE);
			if (ComponentInstancesByEntity.Keys.Contains(entity))
			{
				Logger.AlwaysLog($"Already added {entity.nameWithId()}.", Logger.severity.ERROR);
				return;
			}

			ComponentInstancesByEntity[entity] = new List<ComponentInstanceDescription>();
			foreach (var component in Components)
				AddComponentInstance((EntityComponentDescription<TEntity>)component, entity);
		}

		public void RemoveEntity(TEntity entity)
		{
			//Logger.AlwaysLog($"{entity.nameWithId()}", Logger.severity.TRACE);
			if (!ComponentInstancesByEntity.Keys.Contains(entity))
			{
				Logger.AlwaysLog($"Never received {entity.nameWithId()}.", Logger.severity.ERROR);
				return;
			}

			foreach (var instance in ComponentInstancesByEntity[entity])
				foreach (var handler in instance.EventActions)
					if (handler.EventName == ComponentEventNames.EntityClose)
						handler.TryInvoke();
					else if (handler.EventName == ComponentEventNames.Update)
						RemoveUpdateHandler(handler);

			ComponentInstancesByEntity.Remove(entity);
		}

		public void RaiseEvent(string eventName, TEntity entity)
		{
			Logger.AlwaysLog($"{eventName}, {entity.nameWithId()}", Logger.severity.TRACE);
			List <ComponentInstanceDescription> instances;
			if (ComponentInstancesByEntity.TryGetValue(entity, out instances))
				foreach (var component in instances)
					component.EventActions.RemoveAll(x => x.EventName == eventName && !x.TryInvoke());
		}

		private void AddComponentInstance(EntityComponentDescription<TEntity> component, TEntity entity)
		{
			ComponentInstanceDescription instance;
			if (!component.TryCreateInstance(RunningOn, entity, out instance))
				return;

			if (component.Debug)
				Logger.AlwaysLog($"Adding component {component} for entity {entity.nameWithId()}", Logger.severity.DEBUG);

			//List<ComponentInstanceDescription> instances;
			//if (!ComponentInstancesByEntity.TryGetValue(entity, out instances))
			//	ComponentInstancesByEntity[entity] = instances = new List<ComponentInstanceDescription>();
			//instances.Add(instance);
			ComponentInstancesByEntity[entity].Add(instance);

			foreach (var handler in instance.EventActions.Where(x => x.EventName == ComponentEventNames.Update))
				AddUpdateHandler(handler);
		}
	}

	/// <summary>
	/// Holds a set of session components.
	/// Holds a reference to a shared update registry where it attaches and detaches component update events for efficient access.
	/// Provides helpers to add/remove and raise events.
	/// </summary>
	public class SessionComponentStore : ComponentStore
	{
		private Dictionary<string, SortedDictionary<int, List<ComponentEventAction>>> SessionEventRegistry = new Dictionary<string, SortedDictionary<int, List<ComponentEventAction>>>();

		public SessionComponentStore(RunLocation runningOn, Dictionary<uint, HashSet<ComponentEventAction>> sharedUpdateRegistry) : 
			base (runningOn, sharedUpdateRegistry) { }

		public void AddComponent(SessionComponentDescription component)
		{
			//Logger.AlwaysLog($"{component}", Logger.severity.TRACE);
			if (!component.ShouldRunOn(RunningOn) || !Components.Add(component))
				return;

			if (component.Debug)
				Logger.AlwaysLog($"Adding component {component}", Logger.severity.DEBUG);

			ComponentInstanceDescription instance;
			if (!component.TryCreateInstance(RunningOn, null, out instance))
				return;

			foreach (var handler in instance.EventActions)
				if (handler.EventName == ComponentEventNames.StaticSessionComponentInit && component.IsStatic)
					handler.TryInvoke();
				else if (handler.EventName == ComponentEventNames.Update)
					AddUpdateHandler(handler);
				else
					AddToSessionEventRegistry(handler);
		}

		public void RaiseEvent(string eventName)
		{
			//Logger.AlwaysLog($"{eventName}", Logger.severity.TRACE);
			SortedDictionary<int, List<ComponentEventAction>> handlersByOrder;
			if (SessionEventRegistry.TryGetValue(eventName, out handlersByOrder))
				foreach (var kvp in handlersByOrder)
					kvp.Value.RemoveAll(x => !x.TryInvoke());
		}

		private void AddToSessionEventRegistry(ComponentEventAction handler)
		{
			//Logger.AlwaysLog($"{handler}", Logger.severity.TRACE);
			SortedDictionary<int, List<ComponentEventAction>> eventHandlersByOrder;
			if (!SessionEventRegistry.TryGetValue(handler.EventName, out eventHandlersByOrder))
				SessionEventRegistry[handler.EventName] = eventHandlersByOrder = new SortedDictionary<int, List<ComponentEventAction>>();

			List<ComponentEventAction> eventHandlers;
			if (!eventHandlersByOrder.TryGetValue(handler.Order, out eventHandlers))
				eventHandlersByOrder[handler.Order] = eventHandlers = new List<ComponentEventAction>();

			eventHandlers.Add(handler);
		}
	}

	/// <summary>
	/// Holds a set of logic components and the entities they're attached to.
	/// Provides helpers to add/remove and raise events and updates.
	/// </summary>
	public class ComponentCollectionStore : ComponentStore
	{
		private EntityComponentStore<IMyCubeBlock> BlockStore;
		private EntityComponentStore<IMyCharacter> CharacterStore;
		private EntityComponentStore<IMyCubeGrid> GridStore;
		private SessionComponentStore SessionStore;

		public ComponentCollectionStore(RunLocation runningOn) : base (runningOn, new Dictionary<uint, HashSet<ComponentEventAction>>())
		{
			BlockStore = new EntityComponentStore<IMyCubeBlock>(RunningOn, SharedUpdateRegistry);
			CharacterStore = new EntityComponentStore<IMyCharacter>(RunningOn, SharedUpdateRegistry);
			GridStore = new EntityComponentStore<IMyCubeGrid>(RunningOn, SharedUpdateRegistry);
			SessionStore = new SessionComponentStore(RunningOn, SharedUpdateRegistry);
		}
	
		public void AddCollection(ComponentDescriptionCollection collection)
		{
			if (collection.Debug)
				Logger.AlwaysLog($"Adding components from {collection}", Logger.severity.DEBUG);

			// Session components first
			foreach (var component in collection.SessionComponents)
				SessionStore.AddComponent(component);

			foreach (var component in collection.BlockComponents)
				BlockStore.AddComponent(component);
			foreach (var component in collection.CharacterComponents)
				CharacterStore.AddComponent(component);
			foreach (var component in collection.GridComponents)
				GridStore.AddComponent(component);

		}

		public void AddUpdateHandler(uint frequency, Action action)
		{
			Logger.AlwaysLog($"{frequency}", Logger.severity.TRACE);
			var methodName = action.Method.DeclaringType.FullName + '.' + action.Method.Name;
			AddUpdateHandler(new ComponentEventAction(action, false, ComponentEventNames.Update, frequency, methodName, 0, false));
		}

		public void RemoveUpdateHandler(uint frequency, Action action)
		{
			Logger.AlwaysLog($"{frequency}", Logger.severity.TRACE);
			HashSet<ComponentEventAction> handlers;
			if (SharedUpdateRegistry.TryGetValue(frequency, out handlers))
				handlers.RemoveWhere(x => x.Action == action);
		}

		public void AddEntity(IMyEntity entity)
		{
			//Logger.AlwaysLog($"{entity.nameWithId()}", Logger.severity.TRACE);
			var asBlock = entity as IMyCubeBlock;
			var asChar = entity as IMyCharacter;
			var asGrid = entity as IMyCubeGrid;

			if (asBlock != null)
				BlockStore.AddEntity(asBlock);
			else if (asChar != null)
				CharacterStore.AddEntity(asChar);
			else if (asGrid != null)
				GridStore.AddEntity(asGrid);
		}

		public void RemoveEntity(IMyEntity entity)
		{
			//Logger.AlwaysLog($"{entity.nameWithId()}", Logger.severity.TRACE);
			var asBlock = entity as IMyCubeBlock;
			var asChar = entity as IMyCharacter;
			var asGrid = entity as IMyCubeGrid;

			if (asBlock != null)
				BlockStore.RemoveEntity(asBlock);
			else if (asChar != null)
				CharacterStore.RemoveEntity(asChar);
			else if (asGrid != null)
				GridStore.RemoveEntity(asGrid);
		}

		public void RaiseEntityEvent(string eventName, IMyEntity entity)
		{
			Logger.AlwaysLog($"{eventName}, {entity.nameWithId()}", Logger.severity.TRACE);
			var asBlock = entity as IMyCubeBlock;
			var asChar = entity as IMyCharacter;
			var asGrid = entity as IMyCubeGrid;

			if (asBlock != null)
				BlockStore.RaiseEvent(eventName, asBlock);
			else if (asChar != null)
				CharacterStore.RaiseEvent(eventName, asChar);
			else if (asGrid != null)
				GridStore.RaiseEvent(eventName, asGrid);
		}

		public void RaiseSessionEvent(string eventName)
		{
			Logger.AlwaysLog($"{eventName}", Logger.severity.TRACE);
			SessionStore.RaiseEvent(eventName);
		}

		public void Update(ulong frame)
		{
			foreach (var kvp in SharedUpdateRegistry)
				if (frame % kvp.Key == 0)
					kvp.Value.RemoveWhere(x => !x.TryInvoke());
		}
	}
}
