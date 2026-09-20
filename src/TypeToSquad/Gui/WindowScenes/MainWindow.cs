using Godot;

using TypeToSquad.Model;
using TypeToSquad.Utils;
using TypeToSquad.Model.Markup;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindow : WindowEx {

	#region /--- Setup ---/

	// Nodes
	public TextEditEx MessageTextEdit { get; private set; } = null!;
	
	BaseButton speakButton = null!;
	BaseButton shutButton = null!;

	BaseButton settingsButton = null!;
	BaseButton toolsButton = null!;
	PopupMenu toolsSelectionPopup = null!;

	BaseButton errorIndicator = null!;


	public override void _Ready() {
		base._Ready();

		// Find main text edit
		MessageTextEdit = this.GetNodeNotNull<TextEditEx>("%MessageTextEdit");
		MessageTextEdit.OnUnicodeInput += OnCharacterTyped;
		
		// Init error indicator
		errorIndicator = this.GetNodeNotNull<BaseButton>("%ErrorIndicator");

		errorIndicator.Hide();
		errorIndicator.Pressed += OnErrorIndicatorPressed;

		LogMonitor.Instance.LoggerNotification += errorIndicator.Show;

		// Init buttons
		speakButton = this.GetNodeNotNull<BaseButton>("%SpeakButton");
		shutButton = this.GetNodeNotNull<BaseButton>("%ShutButton");
		settingsButton = this.GetNodeNotNull<BaseButton>("%SettingsButton");
		toolsButton = this.GetNodeNotNull<BaseButton>("%ToolsButton");

		speakButton.Pressed += OnSpeakPressed;
		shutButton.Pressed += OnShutPressed;
		settingsButton.Pressed += OnSettingsPressed;
		toolsButton.Pressed += OnToolsPressed;
		
		toolsSelectionPopup = this.GetNodeNotNull<PopupMenu>("%ToolsSelectionPopup");

		// Connect focus
		this.FocusEntered += MessageTextEdit.GrabFocus;
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
			MessageTextEdit.InsertTextAtCaret("\n");
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
		
		// Autocomplete
		if (settingsInstance.AutocompleteTags) {
			MessageCompletionProvider.TryAutocompleteTag(MessageTextEdit, caretIndex);
		}
		
	}

	public void OnSettingsPressed() {
		bool useAdvanceSettings = UserSettingsManager.Instance.Settings.ShowAdvancedSettings;
		var windowType = useAdvanceSettings ? WindowType.Settings : WindowType.SimpleSettings;
		WindowManager.Instance.CreateWindowAtSelfUnique(windowType);
	}

	public void OnToolsPressed() {
		Vector2 popupPosition = toolsButton.GetScreenPosition() + toolsButton.Size;
		popupPosition.Y -= toolsButton.Size.Y / 2f;
		popupPosition.Y -= toolsSelectionPopup.Size.Y / 2f;
		toolsSelectionPopup.Position = (Vector2I)popupPosition;
		toolsSelectionPopup.Popup();
	}

	public void OnErrorIndicatorPressed() {
		GD.Print("Opening log file.");
		errorIndicator.Hide();
		OS.ShellOpen(LogMonitor.GetLogfilePath());
	}

	public void OnSpeakPressed() {
		
		var settingsInstance = UserSettingsManager.Instance.Settings;

		// Skip empty messages
		if (string.IsNullOrWhiteSpace(MessageTextEdit.Text)) return;
		
		// Add to history
		HistoryTracker.Instance.AddHistoryEntry(MessageTextEdit.Text);
		HistoryTracker.Instance.NavigateReset();

		// Speak
		GD.Print("Processing...");
		var root = MessageProcessor.ProcessMessage(MessageTextEdit.Text);
		
		GD.Print("Synthesizing...");
		AudioProvider.Instance.CreateStream(root, stream => {
			GD.Print("Playing...");
			AudioManager.Instance.PlayNew(stream);
		});

		// Reset textbox
		MessageTextEdit.Clear();
		MessageTextEdit.GrabFocus();
		MessageTextEdit.ClearUndoHistory();
	}

	public void OnShutPressed() {
		GD.Print("Shutting.");
		AudioManager.Instance.StopAll();
		MessageTextEdit.GrabFocus();
	}

	public void OnHistoryPrevRequest() {
		if (HistoryTracker.Instance.TryNavigatePrevious(MessageTextEdit.Text, out string queryResult)) {
			MessageTextEdit.Text = queryResult; // also clears carets
			MessageTextEdit.SetCaretPositionToEnd();
		}
	}

	public void OnHistoryNextRequest() {
		if (HistoryTracker.Instance.TryNavigateNext(MessageTextEdit.Text, out string queryResult)) {
			MessageTextEdit.Text = queryResult; // also clears carets
			MessageTextEdit.SetCaretPositionToEnd();
		}
	}
	
	public void OnInsertTagPressed() {
		MessageCompletionProvider.OpenOrCompleteTagAtAllCarets(MessageTextEdit);
	}

}
