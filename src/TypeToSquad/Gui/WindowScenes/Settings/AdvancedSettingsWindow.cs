using Godot;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class AdvancedSettingsWindow : SettingsWindow, IRefrencesCore {

	protected override void SetupSettingInputs() {

		if (CoreNode is null) return;

		var settings = CoreNode.UserSettings;

		// General
		ImplaceByProperInput(settings.EnableErrorMonitoring, "%EnableMonitoringInput");

		// Voices
		ImplaceByProperInput(settings.Voice, "%MainVoiceInput");
		ImplaceByProperInput(settings.VoicePitch, "%VoicePitchInput");
		ImplaceByProperInput(settings.VoiceRate, "%VoiceRateInput");
		LinkButtonToExternalWindow("%OpenVoiceChangesButton", WindowType.EditVoiceChanges);

		// Input
		ImplaceByProperInput(settings.HistorySlots, "%HistorySlotsInput");
		LinkButtonToExternalWindow("%OpenReplacementsButton", WindowType.EditReplacements);
		ImplaceByProperInput(settings.MaxReplacementPasses, "%ReplacementPassesInput");

		LinkButtonToExternalWindow("%OpenShortcutsButton", WindowType.Shortcuts);

		// Audio
		ImplaceByProperInput(settings.Device, "%OutputDeviceInput");
		ImplaceByProperInput(settings.MaxConcurrentStreams, "%MaxConcurentInput");

		var volumeInput = ImplaceByProperInput(settings.SynthesisVolumePercent, "%SynthesisVolumeInput");
		((SpinBox)volumeInput).Suffix = "%";
	}

}
