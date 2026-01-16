using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model;


public partial class HistoryTracker : Node {

	#region //// Singleton

	public static HistoryTracker Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion

	public override void _Ready() {
		StageSingletonInstance();
	}

	/// <summary>History of input, recent input first.</summary>
	readonly LinkedList<string> history = new();

	/// <summary>
	/// Returns all stored previous inputs, ordered recent first.
	/// Does not return the present (currently typed) entry.
	/// </summary>
	public string[] GetFullHistory() => history.ToArray();

	/// <summary>Adds an entry to the history</summary>
	public void AddHistoryEntry(string text) {

		// Reset navigation
		NavigateReset();

		// Add input
		history.AddFirst(text);
		EnforceHistoryCountMax();
	}

	/// <summary>
	/// Removes older entries, ensuring no more than 
	/// a given number of entries is stored.
	/// </summary>
	public void EnforceHistoryCountMax() {

		int historySlots = UserSettingsManager.Instance.Settings.HistorySlots;
		if (historySlots < 0) historySlots = 0;

		while (history.Count > historySlots) history.RemoveLast();

		if (currentHistoryNode is not null && currentHistoryNode.List is null) {
			GD.Print("Current history slot was trimmed. Resetting navigation as a failsafe.");
			NavigateReset();
		}
	}

	#region //// Navigation

	/// <summary>Current navigation node and </summary>
	LinkedListNode<string>? currentHistoryNode = null;

	/// <summary>
	/// When navigating history, this is the entry that would be added
	/// as most recent if history were not being navigated.
	/// </summary>
	string? presentEntry = null;

	/// <summary>
	/// Resets history navigation:
	/// Places navigation point at present
	/// and forgets what present is (see <see cref="TryNavigatePrevious"/>).
	/// </summary>
	public void NavigateReset() {
		currentHistoryNode = null;
		presentEntry = null;
	}

	/// <summary>
	/// Navigates furhter into the past. Returns true if navigation was successful. 
	/// Remembers the present during the first navigation into the past.
	/// </summary>
	public bool TryNavigatePrevious(string currentText, out string queryResult) {

		// No history
		if (currentHistoryNode == null && history.Count == 0) {
			queryResult = currentText;
			return false;
		}

		// Start of navigation: Store present
		if (currentHistoryNode == null) {
			presentEntry = currentText;
			currentHistoryNode = history.First;
			queryResult = currentHistoryNode!.Value;
			return true;
		}

		// Do not look beyond oldest entry
		if (currentHistoryNode == history.Last) {
			queryResult = currentText;
			return false;
		}

		// Select next entry
		currentHistoryNode = currentHistoryNode.Next;
		queryResult = currentHistoryNode!.Value;
		return true;
	}

	/// <summary>
	/// Navigates further towards present. Returns true if navigation was successful.
	/// Can return present.
	/// </summary>
	public bool TryNavigateNext(string currentText, out string queryResult) {

		// Do not look beyond present
		if (currentHistoryNode == null) {
			queryResult = currentText;
			return false;
		}

		// Switch from history to present
		if (currentHistoryNode == history.First) {
			string present = presentEntry ?? "(null)";
			NavigateReset();
			queryResult = present;
			return true;
		}

		// Select previous entry
		currentHistoryNode = currentHistoryNode.Previous;
		queryResult = currentHistoryNode!.Value;
		return true;
	}

	#endregion

}
