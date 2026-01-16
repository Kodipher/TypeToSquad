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

	// Assume to be { get; private init; }
	// Need to be private set; because they are set when _Ready is called

	// All of these are set in _Ready

	public WindowManager WindowManager { get; private set; } = null!;
	public MainWindow MainWindow { get; private set; } = null!;


	public override void _Ready() {

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
	
}
