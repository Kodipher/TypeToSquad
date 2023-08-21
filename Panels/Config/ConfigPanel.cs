using Godot;
using System;
using Kodipher.TypeToSquad.Modules.Configuration;


namespace Kodipher.TypeToSquad.Panels.Config;


public partial class ConfigPanel : Control {

	#region //// Signals and Exports

	[Signal]
	public delegate void ClosePressedEventHandler();

	#endregion

	public void PropogateConfigurationReference(Configuration configuration) {

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot propogate configuration: Node is not ready.");
		}

		// Give out field references
		GetNode<OptionFieldButton>("%VoiceSelection").SetFieldReference(configuration.Voice);
		GetNode<OptionFieldButton>("%DeviceSelection").SetFieldReference(configuration.Device);
		GetNode<IntRangeFieldLineEdit>("%MaxConcurrentInput").SetFieldReference(configuration.MaxConcurrentStreams);
		GetNode<IntRangeFieldLineEdit>("%HistorySlotsInput").SetFieldReference(configuration.HistorySlots);
	}

	public override void _Ready() {

		// Connect close button
		GetNode<Button>("%ButtonClose").Pressed += delegate { EmitSignal(SignalName.ClosePressed); };

	}

}
