using Godot;
using System;
using System.Collections.Generic;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui;


public interface IRefrencesCore {
	public void RecieveCoreReference(CoreNode core);
}


public partial class CoreNode : Node {

	public override void _Ready() {
		base._Ready();

		// Find Children
		WindowManager = this.GetNodeNotNull<WindowManager>("%WindowManager");

		// Give core
		WindowManager.RecieveCoreReference(this);

		// Instantiate main window
		Node mainWindowCoreParent = this.GetNodeNotNull<Node>("%MainWindowUnpackParent");
		WindowManager.CreateWindowUnpacked(WindowType.Main, mainWindowCoreParent);
		MainWindow = mainWindowCoreParent.GetChild<WindowScenes.MainWindowCore>(0);
	}

	protected virtual void OnPreDelete() {
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) OnPreDelete();
	}

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready
	public WindowScenes.MainWindowCore MainWindow { get; private set; } = null!; // Set in _Ready

	#endregion

}
