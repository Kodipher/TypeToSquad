using System;
using System.Collections.Generic;
using System.Linq;


namespace Kodipher.TypeToSqaud.Modules.Configuration;


/// <summary>
/// A storage, setter, getter and validator
/// for a field that has options.
/// </summary>
public class FieldOptions<T> : Field<T> {

	public Func<IEnumerable<T>> GetOptions { get; protected set; }

	public override bool IsValid(T value) => GetOptions().Contains(value);

	public FieldOptions(Func<IEnumerable<T>> getOptions, Func<T> getDefault) : base(getDefault) {
		GetOptions = getOptions;
	}

	public FieldOptions(Func<IEnumerable<T>> getOptions, T defaultValue) : base(defaultValue) {
		GetOptions = getOptions;
	}

}
