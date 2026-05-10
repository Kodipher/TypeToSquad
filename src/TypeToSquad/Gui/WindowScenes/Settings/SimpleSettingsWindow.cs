using TypeToSquad.Model;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SimpleSettingsWindow : SettingsWindow {

	protected override void SetupSettingInputs() {

		ImplaceByProperInput(UserSettingsManager.Instance.Settings.Voice, "%MainVoiceInput");

		var deviceSelect = ImplaceByProperInput(UserSettingsManager.Instance.Settings.Device, "%OutputDeviceInput");
		FieldInputCreator.ConnectOnControlSubmit(
			deviceSelect,
			() => this.CallOneFrameLater(AudioManager.Instance.InitOutputDeviceSetting)
		);
	}

}
