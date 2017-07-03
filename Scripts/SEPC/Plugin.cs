using Rynchodon;
using Sandbox.Game.World;
using SEPC.Extensions;
using System.Reflection;
using VRage.Game;
using VRage.Plugins;

namespace SEPC
{
	/// <summary>
	/// The entry point for SEPC's game logic.
	/// Loaded with the game and persists until it's closed.
	/// </summary>
	public class Plugin : IPlugin
	{
		private bool SessionComponentsRegistered;

		public void Dispose() { }

		public void Init(object gameInstance)
		{
			if (!MyFinalBuildConstantsExtensions.GetBool("IS_OFFICIAL"))
				Logger.AlwaysLog("Space Engineers build is UNOFFICIAL", Logger.severity.WARNING);
			if (MyFinalBuildConstantsExtensions.GetBool("IS_DEBUG"))
				Logger.AlwaysLog("Space Engineers build is DEBUG", Logger.severity.WARNING);

			Logger.AlwaysLog("Space Engineers version: " + MyFinalBuildConstants.APP_VERSION_STRING, Logger.severity.INFO);
		}

		/// <summary>
		/// Registers ComponentSession with SE as a MySessionComponent, allowing it to serve as the entry point for all Session logic.
		/// </summary>
		public void Update()
		{
			if (SessionComponentsRegistered || MySession.Static == null || !MySession.Static.Ready)
				return;

			Logger.DebugLog("Registering ComponentSession as a MySessionComponent.");
			MySession.Static.RegisterComponentsFromAssembly(Assembly.GetExecutingAssembly(), true);
			SessionComponentsRegistered = true;
		}
	}
}
