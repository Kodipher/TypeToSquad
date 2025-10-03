using Godot;
using System;


namespace TypeToSquad.Gui;

/// <summary>
/// An general extension of the <see cref="TextEdit"/>
/// to expose things <see cref="TextEdit"/> normally doesnt.
/// </summary>
[GlobalClass]
public partial class TextEditEx : TextEdit {

	#region //// OnUnicodeInput Signal

	public override void _HandleUnicodeInput(int unicodeChar, int caretIndex) {

		// Keep original behaviour
		// (calling base does nothing apparently)
		var charAsString = new string((char)unicodeChar, 1);
		//StartAction(EditAction.Typing);
		InsertTextAtCaret(charAsString);
		//EndAction();

		// but also emit signal
		EmitSignalOnUnicodeInput((char)unicodeChar, caretIndex);
	}

	[Signal]
	public delegate void OnUnicodeInputEventHandler(char unicodeChar, int caretIndex);

	#endregion

}
