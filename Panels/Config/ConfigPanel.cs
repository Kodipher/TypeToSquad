using Godot;
using System;


namespace Kodipher.TypeToSqaud.Panels.Config;


public partial class ConfigPanel : Control {

	#region //// Signals and Exports

	[Signal]
	public delegate void ClosePressedEventHandler();

	#endregion

	public override void _Ready() {

		// Connect close button
		GetNode<Button>("%ButtonClose").Pressed += delegate { EmitSignal(SignalName.ClosePressed); };

	}

}
