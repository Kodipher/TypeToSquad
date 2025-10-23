using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
	TextEditEx messageTextEdit = null!;

	BaseButton settingsButton = null!;

	BaseButton errorIndicator = null!;


	public override void _Ready() {
		base._Ready();

		// Find main text edit
		messageTextEdit = this.GetNodeNotNull<TextEditEx>("%MessageTextEdit");
		messageTextEdit.OnUnicodeInput += OnCharacterTyped;

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

	/// <remarks>Assumes partial valid tags.</remarks>
	public void OnCharacterTyped(char typedChar, int caretIndex) {

		// Autocomplete disabled
		if (!CoreNode.UserSettings.AutocompleteTags) return;

		// Find latest opening
		caretIndex = caretIndex == -1 ? 0 : caretIndex;

		int currentLine = messageTextEdit.GetCaretLine(caretIndex);
		int currentColumn = messageTextEdit.GetCaretColumn(caretIndex);

		(bool isCurrentlyOpen, Vector2I openingPos) = SearchForTagOpeningAt(currentLine, currentColumn);

		if (!isCurrentlyOpen) return; // only act on open tags
	
		int tagOpeningStringIndex = messageTextEdit.GetLineStartIndex(openingPos.Y) + openingPos.X;
		int currentIndex = messageTextEdit.GetLineStartIndex(currentLine) + currentColumn;

		// Find currently typed context name
		string currentName = messageTextEdit.Text[(tagOpeningStringIndex + 1)..currentIndex];
		currentName = currentName.TrimStart();

		if (currentName.Length == 0) return; // do not act on empty names

		// Find all context names
		var contentHints = TypeToSquad.Model.Markup.MessageLexer.contextHintStrings.Keys;

		string[] contextHints = Enumerable.Concat(
									CoreNode
										.UserSettings
										.VoiceChanges
										.Select(row => row.hint),
									CoreNode
										.UserSettings
										.TextReplacements
										.Select(row => row.context)
								)
								.Distinct()
								.ToArray();

		// Find matches
		static string? GetSingleOrNullNoThrow(IEnumerable<string> seq) {
			string[] possible = seq.Take(2).ToArray();
			if (possible.Length == 1) return possible[0];
			return null;
		}

		string? possibleContext = GetSingleOrNullNoThrow(contextHints.Where(s => s.StartsWith(currentName)));
		string? possibleContent = GetSingleOrNullNoThrow(contentHints.Where(s => s.StartsWith(currentName, StringComparison.OrdinalIgnoreCase)));

		string? autoCompleteText = null;

		if (possibleContext is not null && possibleContent is null) {
			// Context only
			autoCompleteText = possibleContext[currentName.Length..] + "]";

		} else if (possibleContext is null && possibleContent is not null) {
			// Content only
			autoCompleteText = possibleContent[currentName.Length..] + " ";

		} else if (possibleContext is not null && possibleContent is not null) {
			// Both possible
			if (possibleContent.Equals(possibleContext, StringComparison.OrdinalIgnoreCase)) {
				// Prefer one where case matters.
				autoCompleteText = possibleContext[currentName.Length..];
			}
		}

		// Insert
		if (autoCompleteText is null) return;
		messageTextEdit.InsertTextAtCaret(autoCompleteText, caretIndex);
	}

	/// <remarks>
	/// Assumes partial valid tags.
	/// Only works with the main caret.
	/// </remarks>
	/// <returns>true if the tab press was consumed</returns>
	public bool OnInsertTagPressed() {

		// Disabled from settings
		if (!CoreNode.UserSettings.TabToInsertTag) return false;

		// Find latest opening
		int currentLine = messageTextEdit.GetCaretLine(caretIndex: 0);
		int currentColumn = messageTextEdit.GetCaretColumn(caretIndex: 0);

		(bool isCurrentlyOpen, _) = SearchForTagOpeningAt(currentLine, currentColumn);

		// Insert
		messageTextEdit.InsertText(isCurrentlyOpen ? "]" : "[", currentLine, currentColumn, beforeSelectionBegin: true);

		return true;
	}


	/// <returns>
	/// Whether given position is after a tag opening and 
	/// the position of the last opening or closing character.
	/// </returns>
	(bool isOpen, Vector2I position) SearchForTagOpeningAt(int line, int column) {

		var searchFlags = (uint)TextEdit.SearchFlags.Backwards;
		Vector2I lastOpen = messageTextEdit.Search("[", searchFlags, line, column);
		Vector2I lastClose = messageTextEdit.Search("]", searchFlags, line, column);

		// x is the column, y is the line	

		// Invalidate anything after caret position
		if (lastOpen.Y > line || (lastOpen.Y == line && lastOpen.X >= column)) {
			lastOpen = new Vector2I(-1, -1);
		}

		if (lastClose.Y > line || (lastClose.Y == line && lastClose.X >= column)) {
			lastClose = new Vector2I(-1, -1);
		}

		// Nothing found
		if (lastOpen.Y == -1 && lastClose.Y == -1) {
			return (false, lastClose);
		}

		// Close is latest
		// (or close is found and open is not)
		if (lastClose.Y > lastOpen.Y || (lastOpen.Y == lastClose.Y && lastClose.X > lastOpen.X)) {
			return (false, lastClose);
		}

		// Otherwise open
		return (true, lastOpen);
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
		(string requestString, bool isSsml) = CoreNode.MessageProsessor.ProcessMessage(messageTextEdit.Text);

		CoreNode.SpeechDaemon.SendMessage(
			requestString,
			isSsml,
			CoreNode.UserSettings,
			(response) => {

				// Voice does not exist
				if (!response.GivenVoiceExists) {
					GD.PushError("Selected voice does not exist.");
					var voiceField = CoreNode.UserSettings.Voice;
					voiceField.Value = voiceField.DefaultValue;
				}

				// Play resulting data
				CoreNode.AudioManager.PlayNew(response.SynthesizedData);
			}
		);

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
