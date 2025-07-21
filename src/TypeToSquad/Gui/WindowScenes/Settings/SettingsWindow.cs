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

	protected virtual void SetupSettingInputs() {

		if (CoreNode is null) return;

		SetupInputOption(CoreNode.UserSettings.Voice, "%MainVoiceInput");
		SetupInputOption(CoreNode.UserSettings.Device, "%OutputDeviceInput");
	}

	#region //// Input Setup

	protected void SetupInputLineEdit<[MustBeVariant] T>(Field<T> settingsField, NodePath inputPath)
	where T : notnull 
	{

		var fieldInput = this.GetNodeNotNull<LineEdit>(inputPath);
		fieldInput.Text = settingsField.Value.ToString();

		// Connect input
		void OnTextSubmit(string text) {
			settingsField.ValueAsSavable = text; // a bit of a hack to parse input
			fieldInput.Text = settingsField.ValueAsSavable.ToString();
		}

		fieldInput.TextSubmitted += OnTextSubmit;
		fieldInput.FocusExited += () => OnTextSubmit(fieldInput.Text ?? "");
	}

	protected void SetupInputSpinBox(FieldIntRange field, NodePath inputPath) {

		var fieldInput = this.GetNodeNotNull<SpinBox>(inputPath);
		fieldInput.MinValue = field.MinInclusive;
		fieldInput.MaxValue = field.MaxInclusive;
		fieldInput.Rounded = true;
		fieldInput.Step = 1;
		fieldInput.Value = field.Value;

		// Connect input
		fieldInput.ValueChanged += newValue => field.Value = (int)newValue;
	}

	protected void SetupInputToggle(Field<bool> toggle, NodePath inputPath) {
		var inputToggle = this.GetNodeNotNull<Button>(inputPath);
		if (toggle.Value) inputToggle.ButtonPressed = true;
		inputToggle.Toggled += newValue => toggle.Value = newValue;
	}

	protected void SetupInputOption(FieldOptionsRuntime options, NodePath inputPath) {
		
		// Options not set guard
		if (options.Options is null) return;

		// Set up items
		var inputField = this.GetNodeNotNull<OptionButton>(inputPath);
		inputField.Clear();

		foreach (string option in options.Options) {
			inputField.AddItem(option);
			if (options.Value == option) inputField.Select(inputField.ItemCount - 1);
		}

		// Connect input
		inputField.ItemSelected += index => options.Value = inputField.GetItemText((int)index);
	}

	#endregion

}
