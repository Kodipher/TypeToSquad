using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using TypeToSquad.Utils;


namespace TypeToSquad.Model.Markup;


public static class MessageCompletionProvider {

	/// <summary>Autocomplete tag type if only 1 possibility is valid.</summary>
	public static void TryAutocompleteTag(TextEdit textEdit, int caretIndex) {
		
		// Find last opening
		caretIndex = caretIndex == -1 ? 0 : caretIndex;
		
		int currentLine = textEdit.GetCaretLine(caretIndex);
		int currentColumn = textEdit.GetCaretColumn(caretIndex);
		
		bool isCurrentlyOpen = SearchForTagOpeningAt(textEdit, currentLine, currentColumn, out Vector2I position);
		if (!isCurrentlyOpen) return; // only act on unclosed tags
		
		// Find current tag type
		int tagOpeningStringIndex = textEdit.GetLineStartIndex(position.Y) + position.X;
		int caretPositionInString = (tagOpeningStringIndex - position.X) + currentColumn;
		
		string currentName = textEdit.Text[(tagOpeningStringIndex + 1)..caretPositionInString];
		currentName = currentName.TrimStart();
		
		if (currentName.Length == 0) return; // do not act on empty names
		if (currentName.Any(char.IsWhiteSpace)) return; // type was typed
		
		// Find possibilities
		IEnumerable<string> allTagTypes = MessageLexer.BuildInTagTypes.Concat(MessageLexer.GetUserTags());

		string[] possibilities = allTagTypes.Where(s => s!.StartsWith(currentName)).ToArray();
		if (possibilities.Length != 1) return; // not a single possibility
		
		// Insert
		string autoCompleteText = possibilities[0][currentName.Length..] + " ";
		textEdit.InsertTextAtCaret(autoCompleteText, caretIndex);
	}
	
	/// <summary>
	/// For each caret:
	/// If it is in the unclosed tag -- complete the tag by closing it,
	/// otherwise open a tag.
	/// </summary>
	public static void OpenOrCompleteTagAtAllCarets(TextEdit textEdit) {

		textEdit.BeginComplexOperation();
		
		foreach (var caretIndex in textEdit.GetSortedCarets()) {
			OpenOrCompleteTagAtCaret(textEdit, caretIndex);
		}
		
		textEdit.EndComplexOperation();
	}
	
	public static void OpenOrCompleteTagAtCaret(TextEdit textEdit, int caretIndex) {

		int currentLine = textEdit.GetCaretLine(caretIndex);
		int currentColumn = textEdit.GetCaretColumn(caretIndex);

		bool isCurrentlyOpen = SearchForTagOpeningAt(textEdit, currentLine, currentColumn, out _);
			
		string insertText = isCurrentlyOpen ? MessageLexer.TagCloseAsString : MessageLexer.TagOpenAsString;
		textEdit.InsertTextAtCaret(insertText, caretIndex);
	}

	/// <returns>
	/// Whether given position is after a tag opening and 
	/// the position of the last opening or closing character.
	/// <paramref name="position"/> will have the position of the last
	/// opening or closing tag character, or (-1, -1) if neither is found.
	/// </returns>
	public static bool SearchForTagOpeningAt(TextEdit textEdit, int line, int column, out Vector2I position) {

		var searchFlags = (uint)TextEdit.SearchFlags.Backwards;
		Vector2I lastOpen = textEdit.Search(MessageLexer.TagOpen.ToString(), searchFlags, line, column);
		Vector2I lastClose = textEdit.Search(MessageLexer.TagClose.ToString(), searchFlags, line, column);

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
			position = lastClose; 
			return false;
		}

		// Close is latest
		// (or close is found and open is not)
		if (lastClose.Y > lastOpen.Y || (lastOpen.Y == lastClose.Y && lastClose.X > lastOpen.X)) {
			position = lastClose;
			return false;
		}

		// Otherwise open
		position = lastOpen;
		return true;
	}
	
}
