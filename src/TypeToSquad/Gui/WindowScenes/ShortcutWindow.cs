using Godot;
using System;
using System.Linq;

using Rephidock.GeneralUtilities.Collections;

using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes;


public partial class ShortcutWindow : WindowEx {

	#region //// Displayed Shortcuts

	/// <summary>A list of shortcuts to display.</summary>
	[Export]
	public string[] DisplayedShortcuts { get; set; } = Array.Empty<string>();

	#endregion

	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;

		var closeButton = this.GetNodeNotNull<BaseButton>("%CloseButton");
		closeButton.Pressed += OnClose;

		// Add shortcuts
		var shortcutGrid = this.GetNodeNotNull<GridContainer>("%ShortcutGrid");

		void PushLabel(string text) {
			shortcutGrid.AddChild(
				new Label() { 
					Text = text, 
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, 
					SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
				}
			);
		}

		foreach (var actionName in DisplayedShortcuts) {

			string displayName = actionName
									.Replace("shortcut", "")
									.Capitalize()
									.Replace("Prev", "Previous");

			string inputEventString = InputMap
									.ActionGetEvents(actionName)
									.Select(ev => ev.AsText())
									.JoinString("\nor ");

			PushLabel(displayName);
			PushLabel(inputEventString);
		}
	}

	public void OnClose() {
		GD.Print("Closing shortcuts");
		this.QueueFree();
	}

}

