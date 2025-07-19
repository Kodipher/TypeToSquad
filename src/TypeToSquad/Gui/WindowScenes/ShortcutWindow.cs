using Godot;
using System;
using System.Linq;

using Rephidock.GeneralUtilities.Collections;

using TypeToSquad.Gui;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes;


public partial class ShortcutWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

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

		foreach (var actionName in DisplayedShortcuts) {

			string displayName = actionName
									.Replace("shortcut", "")
									.Capitalize()
									.Replace("Prev", "Previous");

			string inputEvent = InputMap
									.ActionGetEvents(actionName)
									.Select(ev => ev.AsText())
									.JoinString("; ");

			shortcutGrid.AddChild(new Label() { Text = displayName, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
			shortcutGrid.AddChild(new Label() { Text = inputEvent, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		}
	}

	public void OnClose() {
		GD.Print("Closing shortcuts");
		this.QueueFree();
	}

}

