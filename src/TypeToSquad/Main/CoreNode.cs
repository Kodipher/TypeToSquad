using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using WinRTSpeechSynthServer.Protocol.Messages;

using TypeToSquad.Gui;
using TypeToSquad.Gui.WindowScenes;
using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Model.Markup;
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

	#region //// Components and Parts

	// Assume to be { get; private init; }
	// Need to be private set; because they are set when _Ready is called

	// All of these are set in _Ready

	public UserSettings UserSettings { get; private set; } = null!;
	public LogMonitor LogMonitor { get; private set; } = null!;

	public MessageSender MessageSender { get; private set; } = null!;
	public MessageParser MessageParser { get; private set; } = null!;
	public HistoryTracker HistoryTracker { get; private set; } = null!;
	public SpeechDaemon SpeechDaemon { get; private set; } = null!;

	public AudioManagerNode AudioManager { get; private set; } = null!;

	#endregion

	#region //// UI (window) Components and parts

	public WindowManager WindowManager { get; private set; } = null!;
	public MainWindow MainWindow { get; private set; } = null!;

	#endregion

	public override void _Ready() {

		// Misc. parts
		UserSettings = UserSettingsLoader.Load();
		LogMonitor = new LogMonitor();

		// Init Audio 
		AudioManager = this.GetNodeNotNull<AudioManagerNode>("%AudioManager");
		AudioManager.RecieveCoreReference(this);
		UserSettings.Device.SetOptions(AudioServer.GetOutputDeviceList());
		AudioManager.SetOutputDeviceFromSettings();

		// Init message stuff
		HistoryTracker = new HistoryTracker();
		HistoryTracker.MaxHistorySize = UserSettings.HistorySlots;

		MessageParser = new MessageParser();
		MessageParser.RecieveCoreReference(this);

		MessageSender = new MessageSender();
		MessageSender.RecieveCoreReference(this);

		// Start Daemon
		SpeechDaemon = new SpeechDaemon();
		SpeechDaemon.StartDaemon();
		SpeechDaemon.DispatchRequest( // find voices
			new GetVoicesRequest(),
			(resp) => {

				if (resp is not AllVoicesResponse voicesResponse) {
					GD.PushError($"{nameof(GetVoicesRequest)} did not give a {nameof(AllVoicesResponse)}");
					return;
				}

				UserSettings.Voice.SetOptions(voicesResponse.Voices.Select(v => v.Name), voicesResponse.DefaultVoice.Name);
				UserSettings.VoiceChanges.RevalidateAllRows(); // Because Voices validator changed state
				
				SpeechDaemon.StoreVoiceInfos(voicesResponse);
			}
		);

		// Init WindowManager and
		// instantiate main window after ready
		WindowManager = this.GetNodeNotNull<WindowManager>("%WindowManager");
		WindowManager.RecieveCoreReference(this);
		CallDeferred(CoreNode.MethodName.PostReady);
	}

	public void PostReady() {
		// Instantiate main window
		MainWindow = (MainWindow)WindowManager.CreateWindowIntoRoot(WindowType.Main);
		MainWindow._Ready(); // Call ready again manually after the new script is attached

		// Update settings
		//UserSettingsLoader.Save(UserSettings); // Disable automatic resaving to prevent data loss
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

	public void ReapplySettings() {

		AudioManager.SetOutputDeviceFromSettings();
		AudioManager.EnsureConcurrentNodeMax();

		HistoryTracker.MaxHistorySize = UserSettings.HistorySlots;
		HistoryTracker.EnforceHistoryCountMax();

		MainWindow.ClearHighlighterCache();

		if (UserSettings.EnableErrorMonitoring) LogMonitor.CheckLog();
	}

}
