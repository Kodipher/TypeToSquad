using Godot;


namespace Scenes.MessagePanel;


public partial class MessagePanel : Control {

	
	public override void _Ready() {

		// Connect and expose speak button
		MessageEdit messageEdit = GetNode<MessageEdit>("%MessageEdit");

		void OnSpeakPress() {
			EmitSignal(SignalName.SpeakPressed, messageEdit.Text);
			messageEdit.Clear();
		}

		GetNode<Button>("%ButtonSpeak").Pressed += OnSpeakPress;

		// Connect and expose other buttons
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
