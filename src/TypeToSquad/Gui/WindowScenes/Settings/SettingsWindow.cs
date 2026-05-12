using Godot;

using TypeToSquad.Utils;
using TypeToSquad.Model;
using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SettingsWindow : WindowEx {

	public override void _Ready() {
		base._Ready();

		// Saving
		this.CloseRequested += OnClose;
		this.GetNodeNotNull<BaseButton>("%SaveButton").Pressed += OnClose;

		// Advanced settings toggle
		{
			var enableAdvancedCheckbox = this.GetNodeNotNull<BaseButton>("%ShowAdvancedInput");
			Field<bool> advancedSettingsField = UserSettingsManager.Instance.Settings.ShowAdvancedSettings;

			if (advancedSettingsField.Value) enableAdvancedCheckbox.ButtonPressed = true;
			enableAdvancedCheckbox.Toggled += newValue => {
				advancedSettingsField.Value = newValue;
				OnClose();

				var windowType = advancedSettingsField ? WindowType.Settings : WindowType.SimpleSettings;
				WindowManager.Instance.CreateWindowAtSelfUnique(windowType);
			};
		}

		// All inputs
		SetupSettingInputs();
	}

	public void OnClose() {
		GD.Print("Closing settings");

		var windowManager = WindowManager.Instance;
		windowManager.GetExistingWindowAtSelf(WindowType.EditReplacements)?.QueueFree();
		windowManager.GetExistingWindowAtSelf(WindowType.EditVoiceChanges)?.QueueFree();

		UserSettingsManager.Instance.Save();

		this.QueueFree();
	}

	/// <returns>The new node that was placed at the location of the old one.</returns>
	protected Control ImplaceByProperInput(Field settingsField, NodePath inputPath) {

		Control newNode = FieldInputCreator.CreateFor(settingsField);
		Control oldNode = this.GetNodeNotNull<Control>(inputPath);

		oldNode.ReplaceBy(newNode);
		newNode.SizeFlagsHorizontal = oldNode.SizeFlagsHorizontal;
		newNode.SizeFlagsVertical = oldNode.SizeFlagsVertical;
		newNode.SizeFlagsStretchRatio = oldNode.SizeFlagsStretchRatio;

		oldNode.QueueFree();

		return newNode;
	}

	protected void LinkButtonToExternalWindow(NodePath buttonPath, WindowType windowType) {
		this.GetNodeNotNull<Button>(buttonPath).Pressed += () => {
			WindowManager.Instance.CreateWindowAtSelfUnique(windowType);
		};
	}

	/// <summary>
	/// Sets up inputs for user settings.
	/// Overriden in <see cref="SimpleSettingsWindow"/>.
	/// </summary>
	protected virtual void SetupSettingInputs() {
		var settings = UserSettingsManager.Instance.Settings;

		// General
		ImplaceByProperInput(settings.EnableErrorNotifications, "%EnableErrorNotificationsInput");
		ImplaceByProperInput(settings.EnableWarningNotifications, "%EnableWarnNotificationsInput");

		// Voice
		ImplaceByProperInput(settings.VoiceKey, "%MainVoiceInput");
		ImplaceByProperInput(settings.VoicePitch, "%VoicePitchInput");
		ImplaceByProperInput(settings.VoiceRate, "%VoiceRateInput");

		var volumeInput = ImplaceByProperInput(settings.SynthesisVolumePercent, "%VoiceVolumeInput");
		((SpinBox)volumeInput).Suffix = "%";

		// Output
		var deviceSelect = ImplaceByProperInput(settings.Device, "%OutputDeviceInput");
		FieldInputCreator.ConnectOnControlSubmit(
			deviceSelect,
			() => this.CallOneFrameLater(AudioManager.Instance.InitOutputDeviceSetting)
		);

		var maxConcurrentInput = ImplaceByProperInput(settings.MaxConcurrentStreams, "%MaxConcurrentInput");
		FieldInputCreator.ConnectOnControlSubmit(
			maxConcurrentInput,
			() => this.CallOneFrameLater(AudioManager.Instance.EnsureConcurrentNodeMax)
		);

		// Input
		var historySlotsInput = ImplaceByProperInput(settings.HistorySlots, "%HistorySlotsInput");
		FieldInputCreator.ConnectOnControlSubmit(
			historySlotsInput,
			() => this.CallOneFrameLater(HistoryTracker.Instance.EnforceHistoryCountMax)
		);

		ImplaceByProperInput(settings.MaxReplacementPasses, "%ReplacementPassesInput");
		LinkButtonToExternalWindow("%OpenReplacementsButton", WindowType.EditReplacements);
		
		LinkButtonToExternalWindow("%OpenShortcutsButton", WindowType.Shortcuts);

		// Contexts
		LinkButtonToExternalWindow("%OpenVoiceChangesButton", WindowType.EditVoiceChanges);
		LinkButtonToExternalWindow("%OpenSoundEffectsButton", WindowType.EditSoundEffects);
		LinkButtonToExternalWindow("%OpenUserTagsButton", WindowType.EditUserTags);
		ImplaceByProperInput(settings.TabToInsertTag, "%EnableTabToInsertTagInput");
		ImplaceByProperInput(settings.AutocompleteTags, "%EnableTagCompletionInput");

	}

}
