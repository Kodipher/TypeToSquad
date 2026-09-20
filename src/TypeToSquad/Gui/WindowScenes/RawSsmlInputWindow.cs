using Godot;
using System;

using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes;


public partial class RawSsmlInputWindow : Window {
	
	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;

		var closeButton = this.GetNodeNotNull<BaseButton>("%CloseButton");
		closeButton.Pressed += OnClose;
	}
	
	public void OnClose() {
		GD.Print("Closing Raw SSML Input");
		this.QueueFree();
	}
	
}
