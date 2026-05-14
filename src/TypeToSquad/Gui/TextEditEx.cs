using Godot;
using System;


namespace TypeToSquad.Gui;

/// <summary>
/// A general extension of the <see cref="TextEdit"/>
/// to expose things <see cref="TextEdit"/> normally doesn't.
/// </summary>
[GlobalClass]
public partial class TextEditEx : TextEdit {

	#region /--- OnUnicodeInput Signal ---/

	public override void _HandleUnicodeInput(int unicodeChar, int caretIndex) {

		// Keep original behavior
		// (calling base does nothing apparently)
		var charAsString = char.ConvertFromUtf32(unicodeChar);
		//StartAction(EditAction.Typing);
		InsertTextAtCaret(charAsString);
		//EndAction();

		// but also emit signal
		EmitSignalOnUnicodeInput(unicodeChar, caretIndex);
	}

	/// <summary>Emitted after a character has been typed.</summary>
	[Signal]
	public delegate void OnUnicodeInputEventHandler(int unicodeChar, int caretIndex);

	#endregion

}
