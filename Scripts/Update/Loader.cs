using Rynchodon.Update;
using Rynchodon.Update.Components.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rynchodon.Update
{
	[IsSessionComponent(RunLocation.Both)]
	class Loader
	{
		[OnSessionEvent("ARMS.ServerSettingsLoaded")]
		public void LoadWithSettings()
		{
			UpdateManager.RegisterComponentGroup(1);
			UpdateManager.RegisterComponentGroup(2);
		}
	}
}
