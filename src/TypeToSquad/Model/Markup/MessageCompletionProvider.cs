using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model.Markup;


public static class MessageCompletionProvider {
	
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

		bool isCurrentlyOpen = SearchForTagOpeningAt(textEdit, currentLine, currentColumn);
			
		string insertText = isCurrentlyOpen ? MessageLexer.TagCloseAsString : MessageLexer.TagOpenAsString;
		textEdit.InsertTextAtCaret(insertText, caretIndex);
	}
	
	/// <returns>
	/// Whether given position is after a tag opening and 
	/// the position of the last opening or closing character.
	/// </returns>
	public static bool SearchForTagOpeningAt(TextEdit textEdit, int line, int column) {

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
			return false;
		}

		// Close is latest
		// (or close is found and open is not)
		if (lastClose.Y > lastOpen.Y || (lastOpen.Y == lastClose.Y && lastClose.X > lastOpen.X)) {
			return false;
		}

		// Otherwise open
		return true;
	}
	
}
