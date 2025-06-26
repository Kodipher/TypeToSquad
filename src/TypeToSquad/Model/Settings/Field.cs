using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A storage, setter, getter and (when implemented) validator.
/// If validation fails, sets the default value, which bypasses validation.
/// </summary>
public class Field<[MustBeVariant] T> where T : notnull {

	#region //// Value

	/// <summary>
	/// The raw internal value of the <see cref="Field{T}"/>. 
	/// Bypasses validation.
	/// </summary>
	protected T value;

	/// <summary>
	/// Gets or sets the value of this <see cref="Field{T}"/>.
	/// When setting, values are forced to be valid if aren't.
	/// </summary>
	public T Value { 
		get => this.value;
		set => this.value = ValueForceValid(value);
	}

	/// <summary>Gets or sets <see cref="Value"/> as if it was <see cref="Variant"/>.</summary>
	public Variant ValueAsVariant { 
		get => Variant.From(in this.value); 
		set => Value = value.As<T>(); 
	}

	#endregion

	#region //// Validation

	public T DefaultValue { get; private init; }

	/// <summary>
	/// Returns given value if it is valid.
	/// Otherwise returns a different, usually default value.
	/// </summary>
	public virtual T ValueForceValid(T value) => IsValid(value) ? value : DefaultValue;

	public virtual bool IsValid(T value) {
		if (typeof(T).IsValueType) return true;
		return value is not null;
	}

	#endregion

	/// <remarks>
	/// <paramref name="defaultValue"/> must be valid. Checked using <see cref="IsValid(T)"/>.
	/// </remarks>
	public Field(T defaultValue) {

		DefaultValue = defaultValue;
		value = DefaultValue;

		if (!IsValid(defaultValue)) {
			throw new ArgumentException($"Default value {defaultValue} is not valid", nameof(defaultValue));
		}

	}

}