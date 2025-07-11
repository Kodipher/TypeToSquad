using Godot;
using System;
using System.Linq;

using TypeToSquad.Utils;
using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes;


public partial class SettingsWindow : Window, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Nodes (except settings)

	// All set in FindNodes, which is called in _Ready
	BaseButton saveButton = null!;

	void FindNodes() {
		saveButton = this.GetNodeNotNull<BaseButton>("%SaveButton");
	}

	#endregion

	public override void _Ready() {
		this.CloseRequested += OnClose;

		FindNodes();
		saveButton.Pressed += OnClose;

		if (CoreNode is not null) {
			SetupInputOption(CoreNode.UserSettings.Voice, "%MainVoiceInput");
			SetupInputOption(CoreNode.UserSettings.Device, "%OutputDeviceInput");
			SetupInputLineEdit(CoreNode.UserSettings.HistorySlots, "%HistorySlotsInput");
			SetupInputLineEdit(CoreNode.UserSettings.MaxConcurrentStreams, "%MaxConcurentInput");
		}
	}

	public void OnClose() {
		GD.Print("Closing settings");

		if (CoreNode is not null) {
			UserSettingsLoader.Save(CoreNode.UserSettings);
			CoreNode.AudioManager.SetOutputDeviceFromSettings();
		}

		this.QueueFree();
	}

	#region //// Input setup

	public void SetupInputLineEdit<[MustBeVariant] T>(Field<T> settingsField, NodePath inputPath)
	where T : notnull 
	{

		var inputField = this.GetNodeNotNull<LineEdit>(inputPath);
		inputField.Text = settingsField.Value.ToString();

		// Connect input
		void OnTextSubmit(string text) {
			settingsField.ValueAsSavable = text;
			inputField.Text = settingsField.ValueAsSavable.ToString();
		}

		inputField.TextSubmitted += OnTextSubmit;
		inputField.FocusExited += () => OnTextSubmit(inputField.Text ?? "");
	}

	public void SetupInputToggle(Field<bool> toggle, NodePath inputPath) {
		var inputToggle = this.GetNodeNotNull<Button>(inputPath);
		if (toggle.Value) inputToggle.ButtonPressed = true;
		inputToggle.Toggled += newValue => toggle.Value = newValue;
	}

	public void SetupInputOption(FieldOptionsRuntime options, NodePath inputPath) {
		
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
