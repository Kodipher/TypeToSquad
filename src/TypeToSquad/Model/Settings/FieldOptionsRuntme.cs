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
	/// Use this after SetOptions has been called, because
	/// the inherited <see cref="Field{T}.DefaultValue"/> is always an empty string.
	/// </summary>
	public string? DefaultOption { get; private set; } = null;

	/// <remarks>Options are copied.</remarks>
	public void SetOptions(IEnumerable<string> options, string defaultOption) {
		// Enumerate options
		string[] optionsArr = options.ToArray();

		// Find index
		int defaultIndex = Array.IndexOf(optionsArr, defaultOption);
		if (defaultIndex < 0) {
			throw new ArgumentException($"Default option \"{defaultOption}\" is not among options.", nameof(defaultOption));
		}

		SetOptions(optionsArr, defaultIndex);
	}

	/// <inheritdoc cref="SetOptions(IEnumerable{string}, string)"/>
	public void SetOptions(IEnumerable<string> options, int defaultOptionIndex = 0) {

		// Enumerate options
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
		if (!IsValid(value)) value = ReturnValid(value);
	}

	public bool IsValid(string value) {
		if (value is null) return false;
		if (Options is null) return true;
		return Options.Contains(value);
	}

	public override string ReturnValid(string value) {
		return IsValid(value) ? value : (DefaultOption ?? DefaultValue);
	}

	public FieldOptionsRuntime() : base("") { }

}