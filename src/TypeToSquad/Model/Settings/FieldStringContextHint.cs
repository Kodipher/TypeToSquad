using System.Linq;
using Rephidock.GeneralUtilities.Collections;


namespace TypeToSquad.Model.Settings;


public class FieldStringContextHint : Field<string> {

	protected override string ReturnValid(string value) {
		return value
				.Trim()
				.Where(c => !(char.IsWhiteSpace(c) || c == '[' || c == ']'))
				.JoinString();
	}

	public FieldStringContextHint(string defaultValue) : base(defaultValue) { }

}
