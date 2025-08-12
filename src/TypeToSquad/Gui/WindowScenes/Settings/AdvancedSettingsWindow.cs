using Godot;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class AdvancedSettingsWindow : SettingsWindow, IRefrencesCore {

	public override void _Ready() {
		base._Ready();

		// View shortcuts button
		var openShortcutsButton = this.GetNodeNotNull<BaseButton>("%OpenShortcutsButton");
		openShortcutsButton.Pressed += () => {
			if (CoreNode is null) return;
			CoreNode.WindowManager.CreateWindowAtSelfUnique(WindowType.Shortcuts);
		};
	}

	protected override void SetupSettingInputs() {

		if (CoreNode is null) return;

		var settings = CoreNode.UserSettings;

		// General
		ImplaceByProperInput(settings.EnableErrorMonitoring, "%EnableMonitoringInput");

		// Voices
		ImplaceByProperInput(settings.Voice, "%MainVoiceInput");
		ImplaceByProperInput(settings.VoicePitch, "%VoicePitchInput");
		ImplaceByProperInput(settings.VoiceRate, "%VoiceRateInput");

		// Input
		ImplaceByProperInput(settings.HistorySlots, "%HistorySlotsInput");
		ImplaceByProperInput(settings.MaxReplacementPasses, "%ReplacementPassesInput");

		// Audio
		ImplaceByProperInput(settings.Device, "%OutputDeviceInput");
		ImplaceByProperInput(settings.MaxConcurrentStreams, "%MaxConcurentInput");

		var volumeInput = ImplaceByProperInput(settings.SynthesisVolumePercent, "%SynthesisVolumeInput");
		((SpinBox)volumeInput).Suffix = "%";
	}

}
