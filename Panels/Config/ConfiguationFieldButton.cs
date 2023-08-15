using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Kodipher.TypeToSqaud.Modules.Configuration;


namespace Kodipher.TypeToSqaud.Panels.Config;


[GlobalClass]
public partial class ConfiguationFieldButton : OptionButton {

	#region //// Field reference

	public FieldOptions<string>? FieldReference { get; private set; } = null;

	public void SetFieldReference(FieldOptions<string> fieldRef) {

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot receive field reference: Node is not ready.");
		}

		// Set field reference
		FieldReference = fieldRef;

		// Set options
		Clear();

		string[] options = FieldReference!.GetOptions().ToArray();
		foreach (string option in options) {
			AddItem(option);
		}

		// Select current
		SelectFieldItem(options);
	}

	private void SelectFieldItem(string[] options) {

		int currentItemIndex = Array.IndexOf(options, FieldReference!.Value);
		if (currentItemIndex < 0) {
			Select(-1); // Deselects
		} else {
			Select(currentItemIndex);
		}

	}

	private void SelectFieldItem() {
		SelectFieldItem(FieldReference!.GetOptions().ToArray());
	}

	#endregion

	#region //// Selection event

	public override void _Ready() {
		// Connect
		ItemSelected += ConfiguationFieldButton_ItemSelected;
	}

	private void ConfiguationFieldButton_ItemSelected(long index) {

		// Don't do anythign without a reference
		if (FieldReference is null) return;

		// Set value, and select current in case validation fails
		FieldReference.Set(GetItemText((int)index));
		SelectFieldItem();

	}

	#endregion

}
