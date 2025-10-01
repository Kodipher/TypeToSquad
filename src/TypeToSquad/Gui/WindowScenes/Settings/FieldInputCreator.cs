using System;
using Godot;

using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public static class FieldInputCreator {

	/// <summary>
	/// <para>
	/// Creates a <see cref="Control"/> node that serves
	/// as an input to a <see cref="Field"/>.
	/// </para>
	/// <para>
	/// If <paramref name="isUnlinked"/> is set then the node
	/// is created but will not set its <see cref="Field{T}.Value"/> automatically.
	/// </para>
	/// </summary>
	/// <remarks>
	/// Does not set <see cref="Control.SizeFlagsHorizontal"/>, 
	/// <see cref="Control.SizeFlagsVertical"/>,
	/// <see cref="Control.SizeFlagsStretchRatio"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException"/>
	public static Control CreateFor(Field field, bool isUnlinked = false) {
	
		if (field is FieldOptionsRuntime fieldOptions) {
			return CreateForOptions(fieldOptions, isUnlinked);
		}

		if (field is FieldNumericRange<int> fieldRangeInt) {
			return CreateForInt(fieldRangeInt, isUnlinked);
		}

		if (field is FieldNumericRange<double> fieldRangeDouble) {
			return CreateForDouble(fieldRangeDouble, isUnlinked);
		}

		if (field is Field<bool> fieldBool) {
			return CreateForBool(fieldBool, isUnlinked);
		}

		if (field is Field<string> fieldString) {
			return CreateForString(fieldString, isUnlinked);
		}

		throw new NotSupportedException();
	}


	/// <summary>
	/// Returns the user input from any <see cref="Control"/> node
	/// created by <see cref="CreateFor(Field, bool)"/>.
	/// </summary>
	/// <exception cref="NotSupportedException"/>
	public static Variant GetControlInputValue(Control node) {
		
		if (node is OptionButton optionButton) {
			return optionButton.GetItemMetadata(optionButton.Selected);
		}
		if (node is CheckBox checkBox) return checkBox.ButtonPressed;
		if (node is LineEdit lineEdit) return lineEdit.Text;
		if (node is SpinBox spinBox) return spinBox.Value;

		throw new NotSupportedException();
	}

	/// <exception cref="NotSupportedException"/>
	public static void SetControlInputValue(Control node, Variant value) {

		if (node is OptionButton optionButton) {

			for (int i = 0; i < optionButton.ItemCount; i++) {
				if (optionButton.GetItemMetadata(i).AsString() == value.AsString()) {
					optionButton.Selected = i;
					return;
				}
			}

			GD.PushError($"Could not select current option. Could not find {value} among options.");
			optionButton.Selected = -1;
			return;
		}

		if (node is CheckBox checkBox) {
			checkBox.SetPressedNoSignal(value.AsBool());
			return;
		}

		if (node is LineEdit lineEdit) {
			lineEdit.Text = value.AsString();
			return;
		}

		if (node is SpinBox spinBox) {
			spinBox.SetValueNoSignal(value.AsDouble());
			return;
		}

		throw new NotSupportedException();
	}

	/// <summary>
	/// Connects a submission handler to the approprate
	/// node signal of a node created by <see cref="CreateFor(Field, bool)"/>.
	/// </summary>
	/// <exception cref="NotSupportedException"></exception>
	public static void ConnectOnControlSubmit(Control node, Action<Variant> onSubmit) {

		if (node is OptionButton optionButton) {
			optionButton.ItemSelected += index => onSubmit(optionButton.GetItemMetadata((int)index).AsString());
			return;
		}

		if (node is CheckBox checkBox) {
			checkBox.Toggled += newValue => onSubmit(newValue);
			return;
		}

		if (node is LineEdit lineEdit) {
			lineEdit.TextSubmitted += (text) => onSubmit(text);
			lineEdit.FocusExited += () => onSubmit(lineEdit.Text);
			return;
		}

		if (node is SpinBox spinBox) {
			spinBox.ValueChanged += newValue => onSubmit(newValue);
			return;
		}

		throw new NotSupportedException();
	}

	#region //// Case-specific creation

	public static LineEdit CreateForAnyUnlinked() {
		return new LineEdit();
	}


	public static LineEdit CreateForString(Field<string> field, bool isUnlinked = false) {

		// Create
		var textInput = new LineEdit();

		if (isUnlinked) return textInput;

		// Link
		textInput.Text = field.Value.ToString();

		void OnTextSubmit(string text) {
			field.Value = text;
			textInput.Text = field.Value;
		}
		textInput.TextSubmitted += OnTextSubmit;
		textInput.FocusExited += () => OnTextSubmit(textInput.Text);

		return textInput;
	}


	public static CheckBox CreateForBool(Field<bool> field, bool isUnlinked = false) {
		
		// Create
		var inputToggle = new CheckBox();

		if (isUnlinked) return inputToggle;

		// Link
		if (field.Value) inputToggle.ButtonPressed = true;
		inputToggle.Toggled += newValue => field.Value = newValue;

		return inputToggle;
	}

	public static SpinBox CreateForInt(FieldNumericRange<int> field, bool isUnlinked = false) {

		// Create
		var fieldInput = new SpinBox() {
			MinValue = field.MinInclusive,
			MaxValue = field.MaxInclusive,
			AllowLesser = false,
			AllowGreater = false,

			Rounded = true,
			Step = 1
		};

		if (isUnlinked) return fieldInput;

		// Link
		fieldInput.Value = field.Value;
		fieldInput.ValueChanged += newValue => field.Value = (int)newValue;

		return fieldInput;
	}


	public static SpinBox CreateForDouble(FieldNumericRange<double> field, bool isUnlinked = false, double valueStep = 0.1) {

		// Create
		var fieldInput = new SpinBox() {
			MinValue = field.MinInclusive,
			MaxValue = field.MaxInclusive,
			AllowLesser = false,
			AllowGreater = false,

			Rounded = false,
			Step = valueStep
		};

		if (isUnlinked) return fieldInput;

		// Link
		fieldInput.Value = field.Value;
		fieldInput.ValueChanged += newValue => field.Value = newValue;

		return fieldInput;
	}


	/// <remarks>The actual value set to the field is stored in items' metadata.</remarks>
	public static OptionButton CreateForOptions(FieldOptionsRuntime field, bool isUnlinked = false) {

		// Create
		var optionsInput = new OptionButton {
			AllowReselect = true,
			FitToLongestItem = true
		};

		if (field.Options is null) {
			GD.PushWarning($"Given {nameof(FieldOptionsRuntime)} has no options set yet.");
			return optionsInput; // also skips linking
		}

		foreach (string option in field.Options) {
			optionsInput.AddItem(option);
			optionsInput.SetItemMetadata(optionsInput.ItemCount - 1, option);
		}

		if (isUnlinked) return optionsInput;

		// Link
		int currentIndex = field.Options.IndexOf(field.Value);
		if (currentIndex < 0) {
			GD.PushError($"Could not select current option. Could not find {field.Value} in {field.Options}.");
		} else {
			optionsInput.Select(currentIndex);
		}
		
		optionsInput.ItemSelected += index => field.Value = optionsInput.GetItemMetadata((int)index).AsString();
		
		return optionsInput;
	}

	#endregion

}
