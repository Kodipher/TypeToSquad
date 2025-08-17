using Godot;

using TypeToSquad.Utils;
using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class SettingsWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Nodes (common for both setting windows)

	// All set in FindNodes, which is called in _Ready
	BaseButton saveButton = null!;
	BaseButton enableAdvancedCheckbox = null!;

	void FindNodes() {
		saveButton = this.GetNodeNotNull<BaseButton>("%SaveButton");
		enableAdvancedCheckbox = this.GetNodeNotNull<BaseButton>("%EnableAdvancedInput");
	}

	#endregion

	public override void _Ready() {
		base._Ready();

		FindNodes();

		// Saving
		this.CloseRequested += OnClose;
		saveButton.Pressed += OnClose;

		// Advanced settings toggle
		if (CoreNode is not null) {
			Field<bool> advancedSettingsField = CoreNode.UserSettings.UseAdvancedSettings;

			if (advancedSettingsField.Value) enableAdvancedCheckbox.ButtonPressed = true;
			enableAdvancedCheckbox.Toggled += newValue => {
				advancedSettingsField.Value = newValue;
				OnClose();

				var windowType = advancedSettingsField ? WindowType.AdvancedSettings : WindowType.Settings;
				CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
			};
		}

		// All inputs
		SetupSettingInputs();
	}

	public void OnClose() {
		GD.Print("Closing settings");

		if (CoreNode is not null) {
			UserSettingsLoader.Save(CoreNode.UserSettings);
			CoreNode.ReapplySettings();
		}

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

		if (CoreNode is null) throw new System.InvalidOperationException();

		this.GetNode<Button>(buttonPath).Pressed += () => {
			CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
		};
	}

	/// <summary>
	/// Sets up inputs for user settings.
	/// Overriden in derived <see cref="AdvancedSettingsWindow"/>.
	/// </summary>
	protected virtual void SetupSettingInputs() {

		if (CoreNode is null) return;

		ImplaceByProperInput(CoreNode.UserSettings.Voice, "%MainVoiceInput");
		ImplaceByProperInput(CoreNode.UserSettings.Device, "%OutputDeviceInput");
	}

}
