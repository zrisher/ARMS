using System.Reflection;
using Sandbox.Game.World;
using VRage.Plugins;

namespace Rynchodon.SEPC
{
	/// <summary>
	/// Loaded with the game and persists until it's closed.
	/// Registers UpdateManager as a MySessionComponent, allowing it to serve as the entry point for all other logic.
	/// </summary>
	public class Plugin : IPlugin
	{
		private bool Initialized;

		public void Dispose() { }

		public void Init(object gameInstance) { }

		public void Update()
		{
			if (!Initialized)
				Initialize();
		}

		private void Initialize()
		{
			if (MySession.Static == null || !MySession.Static.Ready)
				return;

			Logger.DebugLog("Registering UpdateManager as MySessionComponent.");
			MySession.Static.RegisterComponentsFromAssembly(Assembly.GetExecutingAssembly(), true);

			Initialized = true;
		}
	}
}
