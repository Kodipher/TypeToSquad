using Godot;
using System;
using System.Collections.Generic;

using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui;


public interface IRefrencesCore {
	public void RecieveCoreReference(CoreNode? core);
}


public partial class CoreNode : Node {

	public override void _Ready() {
		base._Ready();

		// Init Model
		UserSettings = UserSettingsLoader.Load();
		SpeechDaemon = new SpeechDaemon();
		LogMonitor = new LogMonitor();

		SpeechDaemon.StartDaemon();
		UserSettingsLoader.Save(UserSettings);
		
		// Find and init Children
		WindowManager = this.GetNodeNotNull<WindowManager>("%WindowManager");
		AudioManager = this.GetNodeNotNull<AudioManager>("%AudioManager");

		WindowManager.RecieveCoreReference(this);
		AudioManager.RecieveCoreReference(this);

		AudioManager.InitOutputDeviceOptions();

		// Instantiate main window
		Node mainWindowCoreParent = this.GetNodeNotNull<Node>("%MainWindowUnpackParent");
		WindowManager.CreateWindowUnpacked(WindowType.Main, mainWindowCoreParent);
		MainWindow = mainWindowCoreParent.GetChild<WindowScenes.MainWindowCore>(0);
	}

	public override void _Process(double delta) {
		SpeechDaemon?.ConsumeResponses();
	}

	protected virtual void OnPreDelete() {
		SpeechDaemon.Dispose();
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) OnPreDelete();
	}

	#region //// Model

	// Assume to be { get; private init; }
	// Need to be private set; because they are set in _Ready

	public UserSettings UserSettings { get; private set; } = null!; // Set in _Ready
	public SpeechDaemon SpeechDaemon { get; private set; } = null!; // Set in _Ready
	public LogMonitor LogMonitor { get; private set; } = null!; // Set in _Ready

	#endregion

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready
	public WindowScenes.MainWindowCore MainWindow { get; private set; } = null!; // Set in _Ready
	public AudioManager AudioManager { get; private set; } = null!; // Set in _Ready

	#endregion

}
