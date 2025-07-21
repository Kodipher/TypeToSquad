using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using WinRTSpeechSynthServer.Protocol.Messages;

using TypeToSquad.Gui;
using TypeToSquad.Gui.WindowScenes;
using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad;


public interface IRefrencesCore {
	/// <summary>
	/// Called once to share the <see cref="CoreNode"/> reference.
	/// <b>Note:</b> Might be called before <see cref="Node._Ready"/>.
	/// </summary>
	public void RecieveCoreReference(CoreNode? core);
}


public partial class CoreNode : Node {

	public override void _Ready() {

		// Find children
		CreateModel();
		FindNodes();

		// Init
		InitModel();

		// Instantiate main window after ready
		CallDeferred(nameof(PostReady));
	}

	public void PostReady() {
		MainWindow = (MainWindow)WindowManager.CreateWindowIntoRoot(WindowType.Main);
		MainWindow._Ready(); // Call ready again manually after the new script is attached
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
			return;
		}

		// Try quit gracfully
		hasPressedQuit = true;
		GD.Print("Gracefuly terminating daemon...");
		SpeechDaemon.DispatchRequest(
			new TerminateRequest(), 
			(_) => {
				GD.Print("Exiting...");
				OnPreDelete();
				GetTree().Quit();
			}
		);
		
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) OnPreDelete();
		else if (what == NotificationWMCloseRequest) OnCloseRequest();
	}

	#region //// Model

	// Assume to be { get; private init; }
	// Need to be private set; because they are when _Ready is called

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

		// Send request to init further
		SpeechDaemon.DispatchRequest(
			new GetVoicesRequest(),
			(resp) => {

				// Init voices
				if (resp is not AllVoicesResponse voicesResponse) {
					GD.PushError($"{nameof(GetVoicesRequest)} did not give a {nameof(AllVoicesResponse)}");
					return;
				}
				
				UserSettings.Voice.SetOptions(voicesResponse.Voices.Select(v => v.Name), voicesResponse.DefaultVoice.Name);
				SpeechDaemon.StoreVoiceInfos(voicesResponse);

				// Update settings
				UserSettingsLoader.Save(UserSettings);
			}
		);

	}

	public void ReapplySettings() {
		AudioManager.SetOutputDeviceFromSettings();
		AudioManager.EnsureConcurrentNodeMax();
		MainWindow.historyTracker.TrimToSize(UserSettings.HistorySlots);

		if (UserSettings.EnableErrorMonitoring) LogMonitor.CheckLog();
	}

	#endregion

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready
	public MainWindow MainWindow { get; private set; } = null!; // Set in _Ready
	public AudioManagerNode AudioManager { get; private set; } = null!; // Set in _Ready

	void FindNodes() {
		WindowManager = this.GetNodeNotNull<WindowManager>("%WindowManager");
		AudioManager = this.GetNodeNotNull<AudioManagerNode>("%AudioManager");

		WindowManager.RecieveCoreReference(this);
		AudioManager.RecieveCoreReference(this);
	}

	#endregion

}
