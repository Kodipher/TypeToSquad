using Godot;
using Kodipher.TypeToSquad.Modules.Configuration;
using Kodipher.TypeToSquad.Panels.Message;


namespace Kodipher.TypeToSquad.Modules;


public partial class MainController : Node {

	public override void _Ready() {

		var messagePanel = GetNode<MessagePanel>("%MessagePanel");

		// Load config
		var configManager = GetNode<ConfigurationManager>("%ConfigurationManager");
		configManager.LoadConfigToCurrent();

		// Connect history
		var historyTracker = GetNode<HistoryTracker>("%HistoryTracker");

		messagePanel.HistoryPreviousRequested += delegate () {
			var (historyEntry, requiresChange) = historyTracker.NavigatePrevious(messagePanel.GetMessageText());
			if (requiresChange) messagePanel.SetMessageText(historyEntry);
		};

		messagePanel.HistoryNextRequested += delegate () {
			var (historyEntry, requiresChange) = historyTracker.NavigateNext(messagePanel.GetMessageText());
			if (requiresChange) messagePanel.SetMessageText(historyEntry);
		};

		messagePanel.SpeakRequested += delegate (string message) {
			historyTracker.NavigateReset();
			historyTracker.AddHistoryEntry(message);
		};

		// Connect window creation
		var windowManager = GetNode<WindowManager>("%WindowManager");
		messagePanel.ConfigRequested += delegate () { windowManager.CreateWindow(WindowManager.Windows.Config); };

	}

}
