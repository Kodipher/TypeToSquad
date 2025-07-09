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
	}

	public void OnClose() {
		GD.Print("Closing settings");

		if (CoreNode is not null) {
			UserSettingsLoader.Save(CoreNode.UserSettings);
			CoreNode.AudioManager.SetOutputDeviceFromSettings();
		}

		this.QueueFree();
	}

}
