using Godot;
using Kodipher.TypeToSqaud.Utils.Godot;


namespace Kodipher.TypeToSqaud.Panels.Message;


public partial class MessagePanel : Control {

	#region //// Signals and Exports

	[Signal]
	public delegate void SpeakRequestedEventHandler(string inputText);

	[Signal]
	public delegate void ShutRequestedEventHandler();

	[Signal]
	public delegate void ConfigRequestedEventHandler();

	[Signal]
	public delegate void InfoRequestedEventHandler();

	[Signal]
	public delegate void HistoryPreviousRequestedEventHandler();

	[Signal]
	public delegate void HistoryNextRequestedEventHandler();

	[Export]
	bool ClearMessageOnSpeak { get; set; } = true;

	#endregion

	#region //// Message

	public MessageEdit MessageEditNode { get; private set; }

	void OnSpeakPress() {
		EmitSignal(SignalName.SpeakRequested, MessageEditNode.Text);
		if (ClearMessageOnSpeak) MessageEditNode.Clear();
	}

	/// <summary>
	/// Sets the MessageEdit of the scene to have specified text.
	/// Also moves carets to the end of the message
	/// </summary>
	/// <param name="text">Text to set message to</param>
	public void SetMessageText(string text) {

		// Make sure we are ready
		if (!IsNodeReady()) return;

		// Set message
		MessageEditNode.Text = text;
		MessageEditNode.SetCaretPositionToEnd();
		MessageEditNode.MergeOverlappingCarets();
	}

	#endregion


	public override void _Ready() {

		// Find message and connect message edit
		MessageEditNode = GetNode<MessageEdit>("%MessageEdit");
		MessageEditNode.SpeakRequested += OnSpeakPress;
		MessageEditNode.ShutRequested += delegate { EmitSignal(SignalName.ShutRequested); };
		MessageEditNode.ConfigRequested += delegate { EmitSignal(SignalName.ConfigRequested); };
		MessageEditNode.InfoRequested += delegate { EmitSignal(SignalName.InfoRequested); };
		MessageEditNode.HistoryPreviousRequested += delegate { EmitSignal(SignalName.HistoryPreviousRequested); };
		MessageEditNode.HistoryNextRequested += delegate { EmitSignal(SignalName.HistoryNextRequested); };

		// Connect buttons
		GetNode<Button>("%ButtonSpeak").Pressed += OnSpeakPress;
		GetNode<Button>("%ButtonShut").Pressed += delegate { EmitSignal(SignalName.ShutRequested); };
		GetNode<Button>("%ButtonConfig").Pressed += delegate { EmitSignal(SignalName.ConfigRequested); };
		GetNode<Button>("%ButtonInfo").Pressed += delegate { EmitSignal(SignalName.InfoRequested); };

	}

}
