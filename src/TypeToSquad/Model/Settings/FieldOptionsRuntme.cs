using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A <see cref="Field{T}"/> that stores an option. 
/// A full list of options is set at runtime.
/// Internally stored as a string. Default is an empty string, before options are set.
/// </summary>
public class FieldOptionsRuntime : Field<string> {

	public ReadOnlyCollection<string>? Options { get; private set; } = null;
	
	/// <summary>
	/// The default value among <see cref="Options"/>.
	/// Use this after <see cref="SetOptions"/> has been called, because
	/// inherited <see cref="Field{T}.DefaultValue"/> is always an empty string.
	/// </summary>
	public string? DefaultOption { get; private set; } = null;

	/// <remarks>Options are copied.</remarks>
	public void SetOptions(IEnumerable<string> options, int defaultOptionIndex = 0) {

		// Convert options
		string[] optionsArr = options.ToArray();

		// Guards
		if (optionsArr.Length < 1) {
			throw new ArgumentException("There must be at least 1 option.", nameof(options));
		}

		ArgumentOutOfRangeException.ThrowIfLessThan(defaultOptionIndex, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(defaultOptionIndex, optionsArr.Length);

		// Set options
		Options = optionsArr.AsReadOnly();
		DefaultOption = Options[defaultOptionIndex];

		// Force validity under the new condition
		if (!IsValid(value)) value = ValueForceValid(value);
	}

	public override bool IsValid(string value) {
		if (Options is null) return base.IsValid(value);
		if (value is null) return false;
		return Options.Contains(value);
	}

	public override string ValueForceValid(string value) {
		return IsValid(value) ? value : (DefaultOption ?? DefaultValue);
	}

	public FieldOptionsRuntime() : base("") { }

}