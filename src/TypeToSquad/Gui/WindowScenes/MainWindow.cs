using Godot;
using System;
using System.Linq;

using TypeToSquad.Model;
using TypeToSquad.Utils;
using Rephidock.GeneralUtilities.Collections;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Setup

	// Misc. state
	public readonly HistoryTracker historyTracker = new();

	// Nodes
	BaseButton speakButton = null!;
	BaseButton shutButton = null!;
	TextEdit messageTextEdit = null!;

	BaseButton settingsButton = null!;

	BaseButton errorIndicator = null!;


	public override void _Ready() {
		base._Ready();

		// Find nodes
		speakButton = this.GetNodeNotNull<BaseButton>("%SpeakButton");
		shutButton = this.GetNodeNotNull<BaseButton>("%ShutButton");
		messageTextEdit = this.GetNodeNotNull<TextEdit>("%MessageTextEdit");

		settingsButton = this.GetNodeNotNull<BaseButton>("%SettingsButton");

		errorIndicator = this.GetNodeNotNull<BaseButton>("%ErrorIndicator");

		// Init history
		if (CoreNode is not null) {
			historyTracker.MaxHistorySize = CoreNode.UserSettings.HistorySlots;
		};

		// Init error indicator
		errorIndicator.Hide();
		if (CoreNode is not null) {
			CoreNode.LogMonitor.OnErrorFound += errorIndicator.Show;
			if (CoreNode.UserSettings.EnableErrorMonitoring) CoreNode.LogMonitor.CheckLog();
		}

		// Connect button signals
		speakButton.Pressed += OnSpeakPressed;
		shutButton.Pressed += OnShutPressed;
		settingsButton.Pressed += OnSettingsPressed;
		errorIndicator.Pressed += OnErrorIndicatorPressed;

		// Connect focus
		this.FocusEntered += messageTextEdit.GrabFocus;

		// Core check
		if (CoreNode is null) GD.PushError("Main Window has not been provided with CoreNode.");
	}

	public override void _Input(InputEvent @event) {

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
		if (CoreNode is null) return;

		var windowType = CoreNode.UserSettings.UseAdvancedSettings ? WindowType.AdvancedSettings : WindowType.Settings;
		CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
	}

	public void OnErrorIndicatorPressed() {
		if (CoreNode is null) return;

		GD.Print("Opening log file.");
		errorIndicator.Hide();

		CoreNode.LogMonitor.SeekToLogEnd();
		CoreNode.LogMonitor.BlockChecks = false;
		OS.ShellOpen(LogMonitor.GetLogfilePath());
	}

	public void OnSpeakPressed() {
		if (CoreNode is null) return;

		if (CoreNode.UserSettings.EnableErrorMonitoring) CoreNode.LogMonitor.CheckLog();

		// Skip empty messages
		if (string.IsNullOrWhiteSpace(messageTextEdit.Text)) return;

		// Add to history and speak
		GD.Print("Speaking.");
		historyTracker.AddHistoryEntry(messageTextEdit.Text, CoreNode.UserSettings.HistorySlots);
		historyTracker.NavigateReset();
		messageTextEdit.Clear();

		messageTextEdit.GrabFocus();
	}

	public void OnShutPressed() {
		if (CoreNode is null) return;

		GD.Print("Shutting.");
		CoreNode.AudioManager.StopAll();
		messageTextEdit.GrabFocus();
	}

	public void OnHistoryPrevRequest() {
		if (historyTracker.TryNavigatePrevious(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult;
		}
	}

	public void OnHistoryNextRequest() {
		if (historyTracker.TryNavigateNext(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult;
		}
	}

}
