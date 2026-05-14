using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Rephidock.GeneralUtilities.Collections;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A string <see cref="Field{T}"/> that disallows characters
/// that would make a tag invalid syntactically.
/// </summary>
public class FieldPath : Field<string> {
	
	static readonly char[] invalidChars = Path
									.GetInvalidPathChars()
									.Concat(Path.GetInvalidFileNameChars())
									.Distinct()
									.ToArray();
	
	protected override string ReturnValid(string value) {
		IEnumerable<char> charsFiltered = value.Where(chr => !invalidChars.Contains(chr));
		return charsFiltered.JoinString();
	}

	public FieldPath() : base("") { }

}
