using Godot;

using TypeToSquad.Utils;
using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SettingsWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	public override void _Ready() {
		base._Ready();

		// Saving
		this.CloseRequested += OnClose;
		this.GetNodeNotNull<BaseButton>("%SaveButton").Pressed += OnClose;

		// Advanced settings toggle
		{
			var enableAdvancedCheckbox = this.GetNodeNotNull<BaseButton>("%ShowAdvancedInput");
			Field<bool> advancedSettingsField = CoreNode.UserSettings.UseAdvancedSettings;

			if (advancedSettingsField.Value) enableAdvancedCheckbox.ButtonPressed = true;
			enableAdvancedCheckbox.Toggled += newValue => {
				advancedSettingsField.Value = newValue;
				OnClose();

				var windowType = advancedSettingsField ? WindowType.Settings : WindowType.SimpleSettings;
				CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
			};
		}

		// All inputs
		SetupSettingInputs();
	}

	public void OnClose() {
		GD.Print("Closing settings");

		var windowManager = CoreNode.WindowManager;
		windowManager.GetExistingWindowAtSelf(WindowType.EditReplacements)?.QueueFree();
		windowManager.GetExistingWindowAtSelf(WindowType.EditVoiceChanges)?.QueueFree();

		UserSettingsLoader.Save(CoreNode.UserSettings);
		if (CoreNode.UserSettings.EnableErrorMonitoring) CoreNode.LogMonitor.CheckLog();

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
			CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
		};
	}

	/// <summary>
	/// Sets up inputs for user settings.
	/// Overriden in <see cref="SimpleSettingsWindow"/>.
	/// </summary>
	protected virtual void SetupSettingInputs() {
		var settings = CoreNode.UserSettings;

		// General
		ImplaceByProperInput(settings.EnableErrorMonitoring, "%EnableMonitoringInput");

		// Voices
		ImplaceByProperInput(settings.Voice, "%MainVoiceInput");
		ImplaceByProperInput(settings.VoicePitch, "%VoicePitchInput");
		ImplaceByProperInput(settings.VoiceRate, "%VoiceRateInput");
		LinkButtonToExternalWindow("%OpenVoiceChangesButton", WindowType.EditVoiceChanges);

		// Input
		var historySlotsInput = ImplaceByProperInput(settings.HistorySlots, "%HistorySlotsInput");
		FieldInputCreator.ConnectOnControlSubmit(
			historySlotsInput,
			_ => this.CallOneFrameLater(
					() => {
						CoreNode.HistoryTracker.MaxHistorySize = CoreNode.UserSettings.HistorySlots;
						CoreNode.HistoryTracker.EnforceHistoryCountMax();
					}
				)
		);

		LinkButtonToExternalWindow("%OpenReplacementsButton", WindowType.EditReplacements);
		ImplaceByProperInput(settings.MaxReplacementPasses, "%ReplacementPassesInput");

		LinkButtonToExternalWindow("%OpenShortcutsButton", WindowType.Shortcuts);

		// Audio
		var deviceSelect = ImplaceByProperInput(settings.Device, "%OutputDeviceInput");
		FieldInputCreator.ConnectOnControlSubmit(
			deviceSelect, 
			_ => this.CallOneFrameLater(CoreNode.AudioManager.SetOutputDeviceFromSettings)
		);

		var maxConcurrentInput = ImplaceByProperInput(settings.MaxConcurrentStreams, "%MaxConcurentInput");
		FieldInputCreator.ConnectOnControlSubmit(
			maxConcurrentInput, 
			_ => this.CallOneFrameLater(CoreNode.AudioManager.EnsureConcurrentNodeMax)
		);

		var volumeInput = ImplaceByProperInput(settings.SynthesisVolumePercent, "%SynthesisVolumeInput");
		((SpinBox)volumeInput).Suffix = "%";
	}

}
