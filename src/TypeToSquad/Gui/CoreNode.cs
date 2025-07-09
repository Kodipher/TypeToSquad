using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using WinRTSpeechSynthServer.Protocol.Messages;

using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui;


public interface IRefrencesCore {
	public void RecieveCoreReference(CoreNode? core);
}


public partial class CoreNode : Node {

	public override void _Ready() {

		// Find children
		CreateModel();
		FindNodes();

		// Init
		InitModel();

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

	bool hasPressedQuit = false;
	protected virtual void OnCloseRequest() {
		if (hasPressedQuit) {
			// Force quit on second press
			GD.PushWarning("Second close request detected. Force quitting without graceful terminate.");
			GetTree().Quit();
		} else {
			// Try quit gracfully
			hasPressedQuit = true;
			GD.PushWarning("Gracefuly terminating...");
			SpeechDaemon.DispatchRequest(new TerminateRequest(), (_) => GetTree().Quit());
		}
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) OnPreDelete();
		else if (what == NotificationWMCloseRequest) OnCloseRequest();
	}

	#region //// Model

	// Assume to be { get; private init; }
	// Need to be private set; because they are set in _Ready

	public UserSettings UserSettings { get; private set; } = null!; // Set in _Ready
	public SpeechDaemon SpeechDaemon { get; private set; } = null!; // Set in _Ready
	public LogMonitor LogMonitor { get; private set; } = null!; // Set in _Ready

	void CreateModel() {
		UserSettings = UserSettingsLoader.Load();
		SpeechDaemon = new SpeechDaemon();
		LogMonitor = new LogMonitor();
	}

	void InitModel() {

		// Init Audio 
		AudioManager.InitOutputDeviceOptions();
		AudioManager.SetOutputDeviceFromSettings();

		// Start Daemon
		SpeechDaemon.StartDaemon();

		// Init voices
		SpeechDaemon.DispatchRequestSeries(
			new GetVoicesRequest(),
			(resp) => {

				if (resp is not AllVoicesResponse voicesResponse) {
					GD.PushError($"{nameof(GetVoicesRequest)} did not give a {nameof(AllVoicesResponse)}");
					return null;
				}
				
				UserSettings.Voice.SetOptions(voicesResponse.Voices.Select(v => v.Name), voicesResponse.DefaultVoice.Name);
				
				// Set current voice
				return new SetVoiceRequest() { VoiceName = UserSettings.Voice };
			},
			(resp) => {
				SpeechDaemon.NoteVoiceSetResponse(resp);

				// Update settings
				UserSettingsLoader.Save(UserSettings);

				// Finish chain
				return null;
			}
		);

	}

	#endregion

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready
	public WindowScenes.MainWindowCore MainWindow { get; private set; } = null!; // Set in _Ready
	public AudioManager AudioManager { get; private set; } = null!; // Set in _Ready

	void FindNodes() {
		WindowManager = this.GetNodeNotNull<WindowManager>("%WindowManager");
		AudioManager = this.GetNodeNotNull<AudioManager>("%AudioManager");

		WindowManager.RecieveCoreReference(this);
		AudioManager.RecieveCoreReference(this);
	}

	#endregion

}
