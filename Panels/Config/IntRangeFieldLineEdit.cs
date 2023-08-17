using Godot;
using System;
using Kodipher.TypeToSqaud.Modules.Configuration;


namespace Kodipher.TypeToSqaud.Panels.Config;


[GlobalClass]
public partial class IntRangeFieldLineEdit : LineEdit {

	#region //// Field reference

	public FieldIntRange? FieldReference { get; private set; } = null;

	public void SetFieldReference(FieldIntRange fieldRef) {

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot receive field reference: Node is not ready.");
		}

		// Set field reference
		FieldReference = fieldRef;

		// Select current value
		SelectFieldValue();
	}

	private void SelectFieldValue() {
		Text = FieldReference!.Value.ToString();
	}

	#endregion

	#region //// Selection event

	public override void _Ready() {
		// Connect
		TextSubmitted += IntRangeFieldLineEdit_TextSubmitted;
		FocusExited += () => IntRangeFieldLineEdit_TextSubmitted(Text);
	}

	private void IntRangeFieldLineEdit_TextSubmitted(string text) {

		// Don't do anythign without a reference
		if (FieldReference is null) return;

		// Try convert
		if(int.TryParse(text, out int newValue)) {
			FieldReference.Set(newValue);
			// Just in case reselect the value
			SelectFieldValue();
		} else {
			// Reject value
			SelectFieldValue();
		}

		// Defocus
		ReleaseFocus();

	}

	#endregion

}
