using Godot;
using System;


namespace Kodipher.TypeToSquad.Modules.Configuration;


/// <summary>
/// A storage, setter, getter and (when overwriten) validator.
/// If validation fails, sets the default value, which bypasses validation.
/// Base class has no validation.
/// </summary>
public class Field<[MustBeVariant] T> {

	#region //// Value

	protected T value;

	public virtual void Set(T value) => this.value = ValueForceValid(value);
	public virtual T Get() => value;
	public T Value { get => Get(); set => Set(value); }

	public Variant GetVariant() {
		T value = Value;
		return Variant.From(in value);
	}

	public void SetVariant(Variant value) => Set(value.As<T>());

	#endregion

	#region //// Validation

	public Func<T> GetDefault { get; private init; }

	/// <summary>
	/// Returns given value if it is valid.
	/// Otherwise returns a different, usually default value.
	/// </summary>
	public virtual T ValueForceValid(T value) => IsValid(value) ? value : GetDefault();

	public virtual bool IsValid(T value) => true;

	#endregion

	public Field(Func<T> getDefault) {
		GetDefault = getDefault;
		value = GetDefault();
	}

	public Field(T defaultValue) : this(() => defaultValue) { }

}
