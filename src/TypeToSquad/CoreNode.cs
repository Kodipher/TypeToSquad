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

	}

	#region //// Children

	public WindowManager WindowManager { get; private set; } = null!; // Set in _Ready

	#endregion

}
