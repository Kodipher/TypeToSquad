using Godot;
using System;
using System.Linq;

using TypeToSquad.Utils;
using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class AdvancedSettingsWindow : SettingsWindow, IRefrencesCore {


	protected override void SetupSettingInputs() {

		if (CoreNode is null) return;

		SetupInputOption(CoreNode.UserSettings.Voice, "%MainVoiceInput");
		SetupInputOption(CoreNode.UserSettings.Device, "%OutputDeviceInput");
		SetupInputLineEdit(CoreNode.UserSettings.HistorySlots, "%HistorySlotsInput");
		SetupInputLineEdit(CoreNode.UserSettings.MaxConcurrentStreams, "%MaxConcurentInput");
	}

}
