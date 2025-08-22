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
			SetInputAsHandled(); // intercepnt to not print newline
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
