using Godot;
using System;

using TypeToSquad.Model;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	#region //// Setup

	// Nodes
	BaseButton speakButton = null!;
	BaseButton shutButton = null!;
	TextEdit messageTextEdit = null!;

	BaseButton settingsButton = null!;

	BaseButton errorIndicator = null!;


	public override void _Ready() {
		base._Ready();

		// Find main text edit
		messageTextEdit = this.GetNodeNotNull<TextEdit>("%MessageTextEdit");

		// Init syntax highlighter
		var highlighter = new TypeToSquad.Model.Markup.MessageSyntaxHighligher();
		highlighter.RecieveCoreReference(this.CoreNode);
		messageTextEdit.SyntaxHighlighter = highlighter;
		
		// Init error indicator
		errorIndicator = this.GetNodeNotNull<BaseButton>("%ErrorIndicator");

		errorIndicator.Hide();
		errorIndicator.Pressed += OnErrorIndicatorPressed;

		CoreNode.LogMonitor.OnErrorFound += errorIndicator.Show;
		if (CoreNode.UserSettings.EnableErrorMonitoring) CoreNode.LogMonitor.CheckLog();

		// Init buttons
		speakButton = this.GetNodeNotNull<BaseButton>("%SpeakButton");
		shutButton = this.GetNodeNotNull<BaseButton>("%ShutButton");
		settingsButton = this.GetNodeNotNull<BaseButton>("%SettingsButton");

		speakButton.Pressed += OnSpeakPressed;
		shutButton.Pressed += OnShutPressed;
		settingsButton.Pressed += OnSettingsPressed;

		// Connect focus
		this.FocusEntered += messageTextEdit.GrabFocus;
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);

		// Handle some shortcuts manually
		if (@event is not InputEventKey inputEventKey) return;
		if (!inputEventKey.Pressed) return;

		if (inputEventKey.IsActionPressed("shortcut_speak", exactMatch: true)) {
			PushInput(new InputEventShortcut() { Shortcut = speakButton.Shortcut });
			SetInputAsHandled(); // intercept to not print newline
			return;
		}

		if (inputEventKey.IsActionPressed("print_newline", exactMatch: true)) {
			messageTextEdit.InsertTextAtCaret("\n");
			SetInputAsHandled();
			return;
		}

		if (inputEventKey.IsActionPressed("history_prev", exactMatch: true)) {
			OnHistoryPrevRequest();
			SetInputAsHandled();
			return;
		}

		if (inputEventKey.IsActionPressed("history_next", exactMatch: true)) {
			OnHistoryNextRequest();
			SetInputAsHandled();
			return;
		}

		if (inputEventKey.IsActionPressed("insert_tag")) {
			bool setAsHandled = OnInsertTagPressed();
			if (setAsHandled) SetInputAsHandled();
			return;
		}

	}

	#endregion

	#region //// Tag autocomplete, Tab handling

	/// <remarks>
	/// Assuming partial valid tags.
	/// Only works with the main caret.
	/// </remarks>
	/// <returns>true if the tab press was consumed</returns>
	public bool OnInsertTagPressed() {

		// Disabled from settings
		if (!CoreNode.UserSettings.TabToInsertTag) return false;

		int currentLine = messageTextEdit.GetCaretLine(caretIndex: 0);
		int currentColumn = messageTextEdit.GetCaretColumn(caretIndex: 0);

		// Find latest open and closing tag characters
		var searchFlags = (uint)TextEdit.SearchFlags.Backwards;
		Vector2I lastOpen = messageTextEdit.Search("[", searchFlags, currentLine, currentColumn);
		Vector2I lastClose = messageTextEdit.Search("]", searchFlags, currentLine, currentColumn);

		bool isInsertingOpen = false;

		do {
			// x is the column, y is the line	

			// Nothing found
			if (lastOpen.Y == -1 && lastClose.Y == -1) {
				isInsertingOpen = true;
				break;
			}

			// Close is latest
			// (or close is found and open is not)
			if (
				lastOpen.Y < lastClose.Y ||
				(lastOpen.Y == lastClose.Y && lastOpen.X < lastClose.X)	
			) {
				isInsertingOpen = true;
				break;
			}

		} while (false);

		// Insert
		messageTextEdit.InsertText(isInsertingOpen ? "[" : "]", currentLine, currentColumn, beforeSelectionBegin: true);

		return true;
	}

	#endregion


	public void OnSettingsPressed() {
		var windowType = CoreNode.UserSettings.UseAdvancedSettings ? WindowType.Settings : WindowType.SimpleSettings;
		CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
	}

	public void OnErrorIndicatorPressed() {
		GD.Print("Opening log file.");
		errorIndicator.Hide();

		CoreNode.LogMonitor.SeekToLogEnd();
		CoreNode.LogMonitor.BlockChecks = false;
		OS.ShellOpen(LogMonitor.GetLogfilePath());
	}

	public void OnSpeakPressed() {

		if (CoreNode.UserSettings.EnableErrorMonitoring) CoreNode.LogMonitor.CheckLog();

		// Skip empty messages
		if (string.IsNullOrWhiteSpace(messageTextEdit.Text)) return;

		// Add to history
		GD.Print("Speaking.");
		CoreNode.HistoryTracker.AddHistoryEntry(messageTextEdit.Text);
		CoreNode.HistoryTracker.NavigateReset();

		// Speak
		CoreNode.MessageSender.SendMessage(messageTextEdit.Text);

		// Reset textbox
		messageTextEdit.Clear();
		messageTextEdit.GrabFocus();
	}

	public void OnShutPressed() {
		GD.Print("Shutting.");
		CoreNode.AudioManager.StopAll();
		messageTextEdit.GrabFocus();
	}

	public void OnHistoryPrevRequest() {
		if (CoreNode.HistoryTracker.TryNavigatePrevious(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult; // also clears carets
			messageTextEdit.SetCaretPositionToEnd();
		}
	}

	public void OnHistoryNextRequest() {
		if (CoreNode.HistoryTracker.TryNavigateNext(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult; // also clears carets
			messageTextEdit.SetCaretPositionToEnd();
		}
	}

}
