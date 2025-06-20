using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad;


public interface IRefrencesCore {
	public void RecieveCoreReference(CoreNode core);
}


public partial class CoreNode : Node {

	public override void _Ready() {
		base._Ready();

		// Find Children
		WindowManager = GetNode<WindowManager>("%WindowManager");

		// Give core
		WindowManager.RecieveCoreReference(this);

		// Instantiate main window
		Node mainWindowCoreParent = GetNode<Node>("%MainWindowUnpackParent");
		WindowManager.CreateWindowUnpacked(WindowType.Main, mainWindowCoreParent);
		MainWindow = mainWindowCoreParent.GetChild<WindowScenes.MainWindowCore>(0);
	}

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready
	public WindowScenes.MainWindowCore MainWindow { get; private set; } = null!; // Set in _Ready

	#endregion

}
