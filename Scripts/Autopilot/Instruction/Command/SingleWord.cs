﻿using System;
using System.Collections.Generic;
using Rynchodon.Autopilot.Movement;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Rynchodon.Autopilot.Instruction.Command
{
	public abstract class SingleWord : ACommand
	{

		public override string AddName
		{
			get { return char.ToUpper(Identifier[0]) + Identifier.Substring(1); }
		}

		public override string Description
		{
			get { return AddDescription; }
		}

		public override bool HasControls
		{
			get { return false; }
		}

		public override void AddControls(List<IMyTerminalControl> controls)
		{
			throw new InvalidOperationException("No controls to add");
		}

		protected override Action<Movement.Mover> Parse(VRage.Game.ModAPI.IMyCubeBlock autopilot, string command, out string message)
		{
			if (string.IsNullOrWhiteSpace(command))
			{
				message = null;
				return Action;
			}
			message = "extraneous: " + command;
			return null;
		}

		protected override string TermToString()
		{
			return Identifier;
		}

		protected abstract void Action(Mover mover);

	}
}
