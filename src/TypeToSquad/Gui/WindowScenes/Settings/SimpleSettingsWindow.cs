using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SimpleSettingsWindow : SettingsWindow, IRefrencesCore {

	protected override void SetupSettingInputs() {

		ImplaceByProperInput(CoreNode.UserSettings.Voice, "%MainVoiceInput");

		var deviceSelect = ImplaceByProperInput(CoreNode.UserSettings.Device, "%OutputDeviceInput");
		FieldInputCreator.ConnectOnControlSubmit(
			deviceSelect,
			_ => this.CallOneFrameLater(CoreNode.AudioManager.InitOutputDeviceSetting)
		);
	}

}
