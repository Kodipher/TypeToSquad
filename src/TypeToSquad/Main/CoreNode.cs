using Godot;
using System.Linq;

using WinRTSpeechSynthServer.Protocol.Messages;

using TypeToSquad.Gui;
using TypeToSquad.Gui.WindowScenes;
using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Model.Markup;
using TypeToSquad.Utils;


namespace TypeToSquad;


public partial class CoreNode : Node {

	#region //// Components and Parts

	// Assume to be { get; private init; }
	// Need to be private set; because they are set when _Ready is called

	// All of these are set in _Ready

	public LogMonitor LogMonitor { get; private set; } = null!;

	public MessageProsessor MessageProsessor { get; private set; } = null!;
	public HistoryTracker HistoryTracker { get; private set; } = null!;

	public WindowManager WindowManager { get; private set; } = null!;
	public MainWindow MainWindow { get; private set; } = null!;

	#endregion

	public override void _Ready() {

		// Misc. parts
		LogMonitor = new LogMonitor();

		// Init message stuff
		HistoryTracker = new HistoryTracker();
		HistoryTracker.MaxHistorySize = UserSettings.HistorySlots;

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

	protected virtual void OnPreDelete() {
		SpeechDaemon.Instance.CloseAndDisposeDaemon();
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

		Window? maybeSettingsWindow = WindowManager.GetExistingWindowAtSelf(WindowType.Settings);
		if (maybeSettingsWindow is TypeToSquad.Gui.WindowScenes.Settings.SettingsWindow settingsWindow) {
			settingsWindow.OnClose(); // Fake close request to save settings
		}

		maybeSettingsWindow = WindowManager.GetExistingWindowAtSelf(WindowType.SimpleSettings);
		if (maybeSettingsWindow is TypeToSquad.Gui.WindowScenes.Settings.SimpleSettingsWindow simpleSettingsWindow) {
			simpleSettingsWindow.OnClose(); // Fake close request to save settings
		}

		GD.Print("Gracefuly terminating daemon...");
		SpeechDaemon.Instance.DispatchRequest(
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

}
