using System;
using System.Collections.Generic;
using System.Linq;

using Godot;
using Kodipher.TypeToSquad.Modules.Configuration;


namespace Kodipher.TypeToSquad.Modules;


public partial class HistoryTracker : Node {

	// Configuration
	ConfigurationManager configManager = null!;

	#region //// Circular storage

	/// <summary>
	/// Internal history storage. Circular.
	/// Use <see cref="historySlotsInUse"/> and <see cref="currentLastHistoryIndex"/>
	/// when addressing the buffer directly.
	/// </summary>
	string?[] history = Array.Empty<string>();

	int historySlotsInUse = 0;
	int currentLastHistoryIndex = -1;

	/// <summary>
	/// Resizes internal circular storage.
	/// Keeps as many enteries as fit in the new storage.
	/// </summary>
	public void HistoryResizeIfRequired() {

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot set history: Node is not ready.");
		}

		// Skip resizing is size matches
		int historySlots = configManager.CurrentConfig.HistorySlots.Value;
		if (history.Length == historySlots) return;

		// Keep old history in mind
		IList<string> oldHistory = GetOrderedHistory();
		
		// Create a new buffer
		history = new string?[historySlots];

		// Store all possible old history in order of oldest to newest at the start of the buffer
		int oldHistoryIndexesToCopy = Mathf.Min(historySlots, oldHistory.Count);

		int oldHistoryIndex = 0;
		int newHistoryIndex = oldHistoryIndexesToCopy - 1;

		while (oldHistoryIndex < oldHistoryIndexesToCopy) {
			history[newHistoryIndex] = oldHistory[oldHistoryIndex];
			oldHistoryIndex++;
			newHistoryIndex--;
		}

		historySlotsInUse = oldHistoryIndexesToCopy;
		currentLastHistoryIndex = newHistoryIndex;

	}

	/// <summary>
	/// Returns the nth most recent history entry,
	/// starting at index 0.
	/// Does not return the present (currently typed) entry.
	/// </summary>
	public string GetHistoryEntry(int index) {

		if (index < 0 || index >= historySlotsInUse) {
			throw new IndexOutOfRangeException(nameof(index));
		}

		return history[Mathf.PosMod(currentLastHistoryIndex - index, history.Length)]!;

	}


	/// <summary>
	/// Returns all stored previous inputs, ordered recent first.
	/// Does not return the present (currently typed) entry.
	/// </summary>
	/// <returns>A new list with items being previous inputs.</returns>
	public List<string> GetOrderedHistory() {

		if (history.Length == 0) return new List<string>();

		return
			Enumerable
			.Range(0, historySlotsInUse - 1)
			.Select(i => GetHistoryEntry(i))
			.ToList();
	}

	/// <summary>
	/// Adds an entry to the history
	/// </summary>
	public void AddHistoryEntry(string text) {

		HistoryResizeIfRequired();

		// Skip if history isnt tracked
		if (history.Length == 0) return;

		// Move index
		historySlotsInUse = Mathf.Min(historySlotsInUse + 1, history.Length);
		currentLastHistoryIndex = (currentLastHistoryIndex + 1) % history.Length;

		// Set
		history[currentLastHistoryIndex] = text;
	}

	#endregion

	#region //// Navigation

	/// <summary>
	/// Current index in history
	/// or -1 for present (newly typed entry).
	/// </summary>
	int currentNavigationEntryI = -1;

	string? presentEntry = null;

	/// <summary>
	/// Resets history navigation:
	/// Places navigation point at present
	/// and forgets what present is (see <see cref="NavigatePrevious"/>).
	/// </summary>
	public void NavigateReset() {
		currentNavigationEntryI = -1;
		presentEntry = null;
	}

	/// <summary>
	/// Navigates furhter into the past, returning the next older entry if it exists.
	/// Remembers the present during the first navigation into the past.
	/// </summary>
	public (string historyEntry, bool requiresChange) NavigatePrevious(string currentText) {

		// Do not look beyond oldest entry
		if (currentNavigationEntryI == historySlotsInUse - 1) return (currentText, false);

		// Store present
		if (currentNavigationEntryI == -1) {
			presentEntry = currentText;
		}

		// Select previous entry
		currentNavigationEntryI++;
		return (GetHistoryEntry(currentNavigationEntryI), true);

	}

	/// <summary>
	/// Navigates to a point further towards present,
	/// returning the newer entry if it exists.
	/// Can return present.
	/// </summary>
	public (string historyEntry, bool requiresChange) NavigateNext(string currentText) {

		// Do not look to the future of present
		if (currentNavigationEntryI == -1) return (currentText, false);

		// Switch from history to present
		if (currentNavigationEntryI == 0) {
			string present = presentEntry ?? throw new InvalidOperationException();
			NavigateReset();
			return (present , true);
		}

		// Select previous entry
		currentNavigationEntryI--;
		return (GetHistoryEntry(currentNavigationEntryI), true);

	}

	#endregion

	public override void _Ready() {

		// Find related nodes
		configManager = GetNode<ConfigurationManager>("%ConfigurationManager");

	}

}
