

namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SimpleSettingsWindow : SettingsWindow, IRefrencesCore {

	protected override void SetupSettingInputs() {

		if (CoreNode is null) return;

		ImplaceByProperInput(CoreNode.UserSettings.Voice, "%MainVoiceInput");
		ImplaceByProperInput(CoreNode.UserSettings.Device, "%OutputDeviceInput");
	}

}
