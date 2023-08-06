using Godot;
using System.Collections.Generic;


namespace Kodipher.TypeToSqaud.Panels.Message;


public partial class MessageEdit : TextEdit {

	#region //// Signals and Exports

	[Signal]
	public delegate void SpeakRequestedEventHandler();

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

	static readonly Dictionary<Key, StringName> keyToSignalMapping = new() {
		{ Key.Escape, SignalName.ShutRequested },
		{ Key.F2, SignalName.ConfigRequested },
		{ Key.F1, SignalName.InfoRequested },
		{ Key.Up, SignalName.HistoryPreviousRequested },
		{ Key.Down, SignalName.HistoryNextRequested },

		// Has separate handling but included for .Keys
		{ Key.Enter, SignalName.SpeakRequested },
		{ Key.KpEnter, SignalName.SpeakRequested },
	};

	#endregion

	#region //// Context Menu

	public void SetupContextMenu() {
		PopupMenu contextMenu = GetMenu();
		contextMenu.RemoveItem(contextMenu.GetItemIndex((int)MenuItems.DisplayUcc));
		contextMenu.RemoveItem(contextMenu.GetItemIndex((int)MenuItems.SubmenuInsertUcc));
		contextMenu.RemoveItem(contextMenu.GetItemIndex((int)MenuItems.SubmenuTextDir));
	}

	#endregion

	public override void _Ready() {
		SetupContextMenu();
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
				EmitSignal(SignalName.SpeakRequested);

			}

			AcceptEvent();
			return;
		}

		// Handle other buttons
		if (keyToSignalMapping.TryGetValue(eventKey.Keycode, out StringName signalToEmit)) {
			EmitSignal(signalToEmit);
			AcceptEvent();
			return;
		}

		// Otherwise
		base._GuiInput(eventKey);

	}

}
