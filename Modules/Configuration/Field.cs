using System;


namespace Kodipher.TypeToSqaud.Modules.Configuration;


/// <summary>
/// A storage, setter, getter and (when overwriten) validator.
/// If validation fails, sets the default value, which bypasses validation.
/// Base class has no validation.
/// </summary>
public class Field<T> {

	#region //// Value

	protected T value;

	public virtual void Set(T value) => this.value = ValidateOrDefault(value);
	public virtual T Get() => value;
	public T Value { get => Get(); set => Set(value); }

	#endregion

	#region //// Validation

	public Func<T> GetDefault { get; protected set; }

	protected T ValidateOrDefault(T value) => IsValid(value) ? value : GetDefault();

	public virtual bool IsValid(T value) => true;

	#endregion

	public Field(Func<T> getDefault) {
		GetDefault = getDefault;
		value = GetDefault();
	}

	public Field(T defaultValue) : this(() => defaultValue) { }

}
