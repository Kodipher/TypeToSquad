using Godot;
using System;


namespace Scenes.MessageInput;


public partial class MessageInput : Control {

	
	public override void _Ready() {

		MessageEdit messageEdit = GetNode<MessageEdit>("%MessageEdit");

		GetNode<Button>("%ButtonSpeak").Pressed += delegate { EmitSignal(SignalName.SpeakPressed, messageEdit.Text); };
		GetNode<Button>("%ButtonShut").Pressed += delegate { EmitSignal(SignalName.ShutPressed); };
		GetNode<Button>("%ButtonConfig").Pressed += delegate { EmitSignal(SignalName.ConfigPressed); };
		GetNode<Button>("%ButtonInfo").Pressed += delegate { EmitSignal(SignalName.InfoPressed); }; ;

	}

	[Signal]
	public delegate void SpeakPressedEventHandler(string inputText);

	[Signal]
	public delegate void ShutPressedEventHandler();

	[Signal]
	public delegate void InfoPressedEventHandler();

	[Signal]
	public delegate void ConfigPressedEventHandler();

}
