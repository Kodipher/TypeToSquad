using Godot;
using System;
using Kodipher.TypeToSqaud.Modules.Configuration;


namespace Kodipher.TypeToSqaud.Panels.Config;


public partial class ConfigPanel : Control {

	#region //// Signals and Exports

	[Signal]
	public delegate void ClosePressedEventHandler();

	#endregion

	public void PropogateConfigurationReference(ConfigurationManager.Configuration configuration) {

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot propogate configuration: Node is not ready.");
		}

		//TODO
		GD.Print($"[TODO] Propogating config reference");
	}

	public override void _Ready() {

		// Connect close button
		GetNode<Button>("%ButtonClose").Pressed += delegate { EmitSignal(SignalName.ClosePressed); };

	}

}
