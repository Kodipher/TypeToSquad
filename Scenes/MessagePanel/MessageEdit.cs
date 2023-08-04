using Godot;
using System.Collections.Generic;


namespace Scenes.MessagePanel;


public partial class MessageEdit : TextEdit {


	Button speakButton;
	Button shutButton;
	Button infoButton;
	Button configButton;

	Dictionary<Key, Button> interceptedKeysToButtons;


	public override void _Ready() {
		SetProcessInput(true);

		speakButton = GetNode<Button>("%ButtonSpeak");
		shutButton = GetNode<Button>("%ButtonShut");
		configButton = GetNode<Button>("%ButtonConfig");
		infoButton = GetNode<Button>("%ButtonInfo");

		interceptedKeysToButtons = new Dictionary<Key, Button>() {
			{ Key.Enter, speakButton },	// Has separate handling
			{ Key.Escape, shutButton },
			{ Key.F2, configButton },
			{ Key.F1, infoButton }
		};

	}

	public override void _GuiInput(InputEvent @event) {

		// Separate key pressed events fron other events
		if (@event is not InputEventKey) {
			base._GuiInput(@event);
			return;
		}

		InputEventKey eventKey = (@event as InputEventKey)!;

		if (!eventKey.Pressed) {
			base._GuiInput(eventKey);
			return;
		}


		// Handle enter
		if (eventKey.Keycode == Key.Enter || eventKey.Keycode == Key.KpEnter) {

			if (eventKey.ShiftPressed) {
				// Only type newline it shit is pressed
				InsertTextAtCaret("\n");

			} else {
				// Otherwise speak the message
				speakButton.EmitSignal(Button.SignalName.Pressed);

			}

			AcceptEvent();
			return;
		}

		// Handle other buttons
		if (interceptedKeysToButtons.TryGetValue(eventKey.Keycode, out Button buttonToPress)) {
			buttonToPress.EmitSignal(Button.SignalName.Pressed);
			AcceptEvent();
			return;
		}

		// Otherwise
		base._GuiInput(eventKey);

	}

}
