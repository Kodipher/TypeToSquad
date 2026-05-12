using System.Collections.Generic;
using System.Linq;
using Rephidock.GeneralUtilities.Collections;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A string <see cref="Field{T}"/> that disallows characters
/// that would make a tag invalid syntactically.
/// </summary>
public class FieldTagContent : Field<string> {
	
	public bool DisallowWhitespace { get; }
	
	protected override string ReturnValid(string value) {

		IEnumerable<char> charsFiltered = value.Where(c => c != '[' && c != ']');
		
		if (DisallowWhitespace) {
			charsFiltered = charsFiltered.Where(c => !char.IsWhiteSpace(c));
		}
		
		return charsFiltered.JoinString();
	}

	public FieldTagContent(bool disallowWhitespace = false) : base("") {
		DisallowWhitespace = disallowWhitespace;
	}

}
