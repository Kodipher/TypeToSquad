using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A storage for a value, together with a validator.
/// The stored value is always valid.
/// The validator is independent of the stored value.
/// </summary>
public abstract class Field : IVariantSavable {
	
	public abstract Variant ToSavableVariant();
	public abstract void SetFromVariant(Variant value);

	/// <summary>
	/// Checks if the input is valid.
	/// Returns the input if it is, returns a different valid value otherwise.
	/// </summary>
	/// <remarks>
	/// Assumes <paramref name="value"/> is immutable.
	/// </remarks>
	public abstract Variant ReturnValid(Variant value);
}


/// <inheritdoc cref="Field"/>
/// <remarks>
/// <para>Default <see cref="ToSavableVariant()"/> looses information for more complex times.</para>
/// <para>Default validation is passthrough.</para>
/// </remarks>
public class Field<[MustBeVariant] T> : Field where T : notnull {

	#region /--- Value ---/
	
	T valueBacking;

	/// <summary>
	/// The value of this <see cref="Field{T}"/>.
	/// When setting, values are forced to become valid if aren't.
	/// </summary>
	public T Value { 
		get => valueBacking;
		set => valueBacking = ReturnValid(value);
	}

	public override Variant ToSavableVariant() => Variant.From(in valueBacking);
	public override void SetFromVariant(Variant value) => Value = value.As<T>();

	public static implicit operator T(Field<T> field) => field.Value;

	#endregion

	#region /--- Validation ---/

	public T DefaultValue { get; private init; }

	/// <summary>
	/// Equivalent of <see cref="ReturnValid(Variant)"/> 
	/// but for the specific type <typeparamref name="T"/> of this <see cref="Field{T}"/>.
	/// </summary>
	public virtual T ReturnValid(T value) => value;
	
	public override Variant ReturnValid(Variant value) => Variant.From(ReturnValid(value.As<T>()));

	#endregion

	/// <remarks>
	/// <paramref name="defaultValue"/> is assumed to be valid.
	/// </remarks>
	public Field(T defaultValue) {
		DefaultValue = defaultValue;
		valueBacking = DefaultValue;
	}

}