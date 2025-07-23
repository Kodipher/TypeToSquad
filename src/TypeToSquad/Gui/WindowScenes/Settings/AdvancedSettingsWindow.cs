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

		// General
		SetupInputToggle(CoreNode.UserSettings.EnableErrorMonitoring, "%EnableMonitoringInput");

		// Voices
		SetupInputOption(CoreNode.UserSettings.Voice, "%MainVoiceInput");
		SetupInputSpinBox(CoreNode.UserSettings.VoicePitch, "%VoicePitchInput");
		SetupInputSpinBox(CoreNode.UserSettings.VoiceRate, "%VoiceRateInput");

		// Input
		SetupInputSpinBox(CoreNode.UserSettings.HistorySlots, "%HistorySlotsInput");

		// Audio
		SetupInputOption(CoreNode.UserSettings.Device, "%OutputDeviceInput");
		SetupInputSpinBox(CoreNode.UserSettings.SynthesisVolumePercent, "%SynthesisVolumeInput");
		SetupInputSpinBox(CoreNode.UserSettings.MaxConcurrentStreams, "%MaxConcurentInput");
	}

}
