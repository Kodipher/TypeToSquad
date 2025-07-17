using Godot;
using System;
using TypeToSquad.Gui;
using TypeToSquad.Utils;


public partial class ShortcutWindow : Window, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Nodes

	// All set in FindNodes, which is called in _Ready
	BaseButton closeButton = null!;

	void FindNodes() {
		closeButton = this.GetNodeNotNull<BaseButton>("%CloseButton");
	}

	#endregion

	public override void _Ready() {
		base._Ready();

		FindNodes();

		// Closing
		this.CloseRequested += OnClose;
		closeButton.Pressed += OnClose;
	}

	public void OnClose() {
		GD.Print("Closing shortcuts");
		this.QueueFree();
	}

}

