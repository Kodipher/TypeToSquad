using Godot;
using Kodipher.TypeToSqaud.Panels.Message;
using System;


namespace Kodipher.TypeToSqaud.Modules;


public partial class MainController : Node {

	public override void _Ready() {

		var messagePanel = GetNode<MessagePanel>("%MessagePanel");

		// Connect window creation
		var windowManager = GetNode<WindowManager>("%WindowManager");
		messagePanel.ConfigRequested += delegate () { windowManager.CreateWindow(WindowManager.Windows.Config); };

	}

}
