using Godot;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class AdvancedSettingsWindow : SettingsWindow, IRefrencesCore {

	protected override void SetupSettingInputs() {

		if (CoreNode is null) return;

		// General
		SetupInputToggle(CoreNode.UserSettings.EnableErrorMonitoring, "%EnableMonitoringInput");

		// Voices
		SetupInputOption(CoreNode.UserSettings.Voice, "%MainVoiceInput");

		// Input
		SetupInputSpinBox(CoreNode.UserSettings.HistorySlots, "%HistorySlotsInput");

		// Audio
		SetupInputOption(CoreNode.UserSettings.Device, "%OutputDeviceInput");
		SetupInputSpinBox(CoreNode.UserSettings.MaxConcurrentStreams, "%MaxConcurentInput");
	}

}
