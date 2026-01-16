using Godot;
using WinRTSpeechSynthServer.Protocol.Messages;
using TypeToSquad.Gui;


namespace TypeToSquad.Model;


public partial class ExitHandler : Node {
	
	#region //// Singleton

	public static ExitHandler Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion
	
	public override void _Ready() {
		StageSingletonInstance();
	}
	
	public override void _Notification(int what) {
		if (what == NotificationWMCloseRequest) OnCloseRequest();
	}

	bool hasPressedQuit = false;
	
	protected virtual void OnCloseRequest() {
		
		if (hasPressedQuit) {
			// Force quit on second press
			GD.PushWarning("Second close request detected. Force quitting without graceful terminate.");
			GetTree().Quit();
			return;
		}

		// Try to quit gracefully
		hasPressedQuit = true;
		QuitGracefully();
	}

	void QuitGracefully() {
		
		GD.Print("Gracefully quitting...");
		
		// Close settings windows
		Window? maybeSettingsWindow = WindowManager.Instance.GetExistingWindowAtSelf(WindowType.Settings);
		if (maybeSettingsWindow is TypeToSquad.Gui.WindowScenes.Settings.SettingsWindow settingsWindow) {
			settingsWindow.OnClose(); // Fake close request to save settings
		}

		maybeSettingsWindow = WindowManager.Instance.GetExistingWindowAtSelf(WindowType.SimpleSettings);
		if (maybeSettingsWindow is TypeToSquad.Gui.WindowScenes.Settings.SimpleSettingsWindow simpleSettingsWindow) {
			simpleSettingsWindow.OnClose(); // Fake close request to save settings
		}

		GD.Print("Gracefully terminating daemon...");
		SpeechDaemon.Instance.DispatchRequest(
			new TerminateRequest(), 
			(_) => {
				GD.Print("Daemon terminated. Disposing...");
				SpeechDaemon.Instance.CloseAndDisposeDaemon();
				GD.Print("Exiting...");
				GetTree().Quit();
			}
		);
		
	}
	
}
