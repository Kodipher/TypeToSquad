using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model;


public class HistoryTracker {

	/// <summary>History of input, recent input first.</summary>
	readonly LinkedList<string> history = new();

	/// <summary>The maximum number of recent inputs stored.</summary>
	/// <remarks>
	/// Changing this value does not trim history automaically. 
	/// Use <see cref="EnforceHistoryCountMax()"/>.
	/// </remarks>
	public int MaxHistorySize { get; set; } = 32;

	/// <summary>
	/// Returns all stored previous inputs, ordered recent first.
	/// Does not return the present (currently typed) entry.
	/// </summary>
	public string[] GetFullHistory() => history.ToArray();

	/// <summary>Adds an entry to the history</summary>
	public void AddHistoryEntry(string text) {

		// Reset naviation
		NavigateReset();

		// Add input
		history.AddFirst(text);
		EnforceHistoryCountMax();
	}

	/// <summary>
	/// Removes older entries, ensuring no more than a give number
	/// of enteries is stored.
	/// </summary>
	public void EnforceHistoryCountMax() {
		if (MaxHistorySize < 0) MaxHistorySize = 0;
		while (history.Count > MaxHistorySize) history.RemoveLast();

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
	/// and forgets what present is (see <see cref="NavigatePrevious"/>).
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
