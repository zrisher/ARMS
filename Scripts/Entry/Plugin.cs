using Rynchodon.Update.Components.Registration;
using System;
using System.Reflection;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Platform;
using Sandbox.Game.World;
using VRage.ObjectBuilders;
using VRage.Plugins;

namespace Rynchodon.Update
{
	/// <summary>
	/// Loaded with the game and persists until game is closed.
	/// </summary>
	public class Plugin : IPlugin
	{
		private bool _loaded;

		public void Dispose() { }

		public void Init(object gameInstance)
		{
			try
			{
				LogicComponentRegistrar.DebugConditional();
				LogicComponentRegistrar.ProfileConditional();
				LogicComponentRegistrar.AddComponents();
				LogicComponentRegistrar.LoadOnInit(0);
			}
			catch (Exception ex)
			{
				Logger.DebugLog($"Error registering components: {ex}");
			}
		}

		public void Update()
		{
			bool ready = MySession.Static != null && MySession.Static.Ready;

			if (_loaded != ready)
			{
				if (!_loaded)
					CheckForArmsAndRegister();
				_loaded = ready;
			}
		}

		private static void CheckForArmsAndRegister()
		{
			if (!Game.IsDedicated && MyDefinitionManager.Static.GetCubeBlockDefinition(new SerializableDefinitionId(typeof(MyObjectBuilder_Cockpit), "Autopilot-Block_Large")) == null)
				return;

			Logger.DebugLog("Registering UpdateManager as MySessionComponent.");
			MySession.Static.RegisterComponentsFromAssembly(Assembly.GetExecutingAssembly(), true);
		}
	}
}
