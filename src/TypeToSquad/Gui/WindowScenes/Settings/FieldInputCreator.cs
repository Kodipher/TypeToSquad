using Godot;
using System;
using System.Numerics;

using TypeToSquad.Model.Settings;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public static class FieldInputCreator {

	#region /--- General (switched by type) ---/
	
	/// <summary>
	/// <para>
	/// Creates a <see cref="Control"/> node that serves
	/// as an input to a <see cref="Field"/>.
	/// </para>
	/// </summary>
	/// <remarks>
	/// Does not set <see cref="Control.SizeFlagsHorizontal"/>, 
	/// <see cref="Control.SizeFlagsVertical"/>,
	/// <see cref="Control.SizeFlagsStretchRatio"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">Unsupported <see cref="Field"/> type.</exception>
	public static Control CreateFor(Field field) {
		return field switch {
			FieldOptionsRuntime fieldOptions => CreateForOptions(fieldOptions),
			FieldNumericRange<int> fieldRangeInt => CreateForNumeric(fieldRangeInt),
			FieldNumericRange<double> fieldRangeDouble => CreateForNumeric(fieldRangeDouble),
			FieldPath fieldPath => CreateForFilePath(fieldPath),
			Field<bool> fieldBool => CreateForBool(fieldBool),
			Field<string> fieldString => CreateForString(fieldString),
			_ => throw new NotSupportedException()
		};
	}

	/// <summary>
	/// Connects a submission handler to the appropriate
	/// node signal of a node created by <see cref="CreateFor(Field)"/>.
	/// </summary>
	/// <exception cref="NotSupportedException">Unsupported <see cref="Control"/> type.</exception>
	public static void ConnectOnControlSubmit(Control node, Action onSubmit) {

		switch (node) {
			
			case FileInput fileInput:
				fileInput.LineEdit.TextSubmitted += _ => onSubmit();
				fileInput.LineEdit.FocusExited += onSubmit;
				fileInput.FileSelectedThroughDialog += _ => onSubmit();
				break;
			
			case OptionButton optionButton:
				optionButton.ItemSelected += _ => onSubmit();
				return;
			
			case CheckBox checkBox:
				checkBox.Toggled += _ => onSubmit();
				return;
			
			case LineEdit lineEdit:
				lineEdit.TextSubmitted += _ => onSubmit();
				lineEdit.FocusExited += onSubmit;
				return;
			
			case SpinBox spinBox:
				spinBox.ValueChanged += _ => onSubmit();
				return;
			
			default:
				throw new NotSupportedException();
		}

	}

	#endregion
	
	public static LineEdit CreateForString(Field<string> field) {

		var textInput = new LineEdit() { Text = field.Value };
		
		void OnTextSubmit(string text) {
			field.Value = text;
			textInput.Text = field.Value;
		}
		textInput.TextSubmitted += OnTextSubmit;
		textInput.FocusExited += () => OnTextSubmit(textInput.Text);

		return textInput;
	}
	
	public static CheckBox CreateForBool(Field<bool> field) {
		
		var inputToggle = new CheckBox();
		
		if (field.Value) inputToggle.ButtonPressed = true;
		inputToggle.Toggled += newValue => field.Value = newValue;

		return inputToggle;
	}

	public static SpinBox CreateForNumeric<[MustBeVariant] T>(FieldNumericRange<T> field) where T : struct, INumber<T> {
		
		var fieldInput = new SpinBox() {
			MinValue = double.CreateChecked(field.MinInclusive),
			MaxValue = double.CreateChecked(field.MaxInclusive),
			AllowLesser = false,
			AllowGreater = false,

			Rounded = field.DigitsPrecision == 0,
			Step = 1d / Math.Pow(10, field.DigitsPrecision) // 0.1^digits has precision errors
		};
		
		fieldInput.Value = double.CreateChecked(field.Value);
		fieldInput.ValueChanged += newValue => field.Value = T.CreateChecked(newValue);

		return fieldInput;
	}

	/// <remarks>The actual value set to the field is stored in items' metadata.</remarks>
	public static OptionButton CreateForOptions(FieldOptionsRuntime field) {

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
	
	/// <remarks>The actual value set to the field is stored in items' metadata.</remarks>
	public static OptionButton CreateForEnum<[MustBeVariant] TEnum>(FieldOptionsEnum<TEnum> field) where TEnum : struct, Enum {

		// Create
		var optionsInput = new OptionButton {
			AllowReselect = true,
			FitToLongestItem = true
		};

		foreach (TEnum option in Enum.GetValues<TEnum>()) {
			optionsInput.AddItem(option.ToString());
			optionsInput.SetItemMetadata(optionsInput.ItemCount - 1, Variant.From(option));
		}
		
		// Link
		for (int i = 0; i < optionsInput.ItemCount; i++) {
			if (optionsInput.GetItemMetadata(i).As<TEnum>().Equals(field.Value)) {
				optionsInput.Selected = i;
				break;
			}
		}

		optionsInput.ItemSelected += index => field.Value = optionsInput.GetItemMetadata((int)index).As<TEnum>();

		return optionsInput;
	}

	static readonly PackedScene fileInputScene = GD.Load<PackedScene>("uid://dkpebb1i6gden");

	public static FileInput CreateForFilePath(FieldPath field) {
		
		FileInput fileInput = fileInputScene.Instantiate<FileInput>();
		fileInput.FindAndSetupChildren();

		fileInput.LineEdit.Text = field.Value;
		
		void OnTextSubmit(string text) {
			field.Value = text;
			fileInput.LineEdit.Text = field.Value;
		}
		
		fileInput.LineEdit.TextSubmitted += OnTextSubmit;
		fileInput.LineEdit.FocusExited += () => OnTextSubmit(fileInput.LineEdit.Text);
		fileInput.FileSelectedThroughDialog += OnTextSubmit;

		return fileInput;
	}

}
