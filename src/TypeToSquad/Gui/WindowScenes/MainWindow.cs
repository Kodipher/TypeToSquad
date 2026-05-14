using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using TypeToSquad.Model;
using TypeToSquad.Utils;
using TypeToSquad.Model.Markup;

using SynthesizeRequest = WinRTSpeechSynthServer.Protocol.Messages.SynthesizeRequest;
using SynthesisResultResponse = WinRTSpeechSynthServer.Protocol.Messages.SynthesisResultResponse;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindow : WindowEx {

	#region /--- Setup ---/

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
		
		// Init error indicator
		errorIndicator = this.GetNodeNotNull<BaseButton>("%ErrorIndicator");

		errorIndicator.Hide();
		errorIndicator.Pressed += OnErrorIndicatorPressed;

		LogMonitor.Instance.LoggerNotification += errorIndicator.Show;

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
			if (UserSettingsManager.Instance.Settings.TabToInsertTag) {
				OnInsertTagPressed();
				SetInputAsHandled();
			}
		}

	}

	#endregion
	
	public void OnCharacterTyped(int typedCharUnicode, int caretIndex) {
		
		var settingsInstance = UserSettingsManager.Instance.Settings;
		
		// Autocomplete disabled
		if (!settingsInstance.AutocompleteTags) return;
		
		// todo
	}

	public void OnSettingsPressed() {
		bool useAdvanceSettings = UserSettingsManager.Instance.Settings.ShowAdvancedSettings;
		var windowType = useAdvanceSettings ? WindowType.Settings : WindowType.SimpleSettings;
		WindowManager.Instance.CreateWindowAtSelfUnique(windowType);
	}

	public void OnErrorIndicatorPressed() {
		GD.Print("Opening log file.");
		errorIndicator.Hide();
		OS.ShellOpen(LogMonitor.GetLogfilePath());
	}

	public void OnSpeakPressed() {
		
		var settingsInstance = UserSettingsManager.Instance.Settings;

		// Skip empty messages
		if (string.IsNullOrWhiteSpace(messageTextEdit.Text)) return;
		
		// Add to history
		GD.Print("Speaking.");
		HistoryTracker.Instance.AddHistoryEntry(messageTextEdit.Text);
		HistoryTracker.Instance.NavigateReset();

		// Speak
		(string requestString, bool isSsml) = MessageProcessor.ProcessMessage(messageTextEdit.Text);

		SynthesizeRequest synthRequest = new SynthesizeRequest() {
			InputString = requestString,
			IsSsml = isSsml,
			VoiceName = DaemonVoiceStorage.Instance.GetVoiceByKey(settingsInstance.VoiceKey).Name,
			Pitch = settingsInstance.VoicePitch,
			Rate = settingsInstance.VoiceRate,
			Volume = settingsInstance.SynthesisVolumePercent / 100.0
		};

		SpeechDaemon.Instance.DispatchRequest<SynthesisResultResponse>(
			synthRequest,
			(response) => {

				// Voice does not exist
				if (!response.GivenVoiceExists) {
					GD.PushError("Selected voice does not exist.");
					var voiceField = settingsInstance.VoiceKey;
					voiceField.Value = voiceField.DefaultValue;
				}

				// Play resulting data
				AudioManager.Instance.PlayNew(response.SynthesizedData);
			}
		);

		// Reset textbox
		messageTextEdit.Clear();
		messageTextEdit.GrabFocus();
	}

	public void OnShutPressed() {
		GD.Print("Shutting.");
		AudioManager.Instance.StopAll();
		messageTextEdit.GrabFocus();
	}

	public void OnHistoryPrevRequest() {
		if (HistoryTracker.Instance.TryNavigatePrevious(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult; // also clears carets
			messageTextEdit.SetCaretPositionToEnd();
		}
	}

	public void OnHistoryNextRequest() {
		if (HistoryTracker.Instance.TryNavigateNext(messageTextEdit.Text, out string queryResult)) {
			messageTextEdit.Text = queryResult; // also clears carets
			messageTextEdit.SetCaretPositionToEnd();
		}
	}
	
	public void OnInsertTagPressed() {
		MessageCompletionProvider.OpenOrCompleteTagAtAllCarets(messageTextEdit);
	}

}
