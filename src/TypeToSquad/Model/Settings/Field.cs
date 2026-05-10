using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// <para>
/// A storage for a value, together with a validator.
/// The stored value is always valid.
/// The validator is independent of the stored value.
/// </para>
/// <para>
/// The store value is assumed to be immutable.
/// </para>
/// </summary>
public abstract class Field : IVariantSavable {
	
	/// <summary>
	/// The value of this <see cref="Field{T}"/>, as a <see cref="Variant"/>.
	/// When setting, values are forced to become valid if aren't.
	/// </summary>
	public abstract Variant ValueVariant { get; set; }
	
	public Variant ToSavableVariant() => ValueVariant;
	public void SetFromVariant(Variant value) => ValueVariant = value;
}


/// <inheritdoc cref="Field"/>
/// <remarks>
/// <para>Default <see cref="ToSavableVariant()"/> looses information for more complex times.</para>
/// <para>Default validation is passthrough.</para>
/// </remarks>
public class Field<[MustBeVariant] T> : Field where T : notnull {
	
	T valueBacking;

	/// <summary>
	/// The value of this <see cref="Field{T}"/>.
	/// When setting, values are forced to become valid if aren't.
	/// </summary>
	public T Value { 
		get => valueBacking;
		set => valueBacking = ReturnValid(value);
	}

	/// <inheritdoc/>
	/// <remarks>Converts to/from <see cref="T"/>.</remarks>
	public override Variant ValueVariant {
		get => Variant.From(in valueBacking);
		set => Value = value.As<T>();
	}
	
	protected virtual T ReturnValid(T value) => value;
	
	public static implicit operator T(Field<T> field) => field.Value;

	public T DefaultValue { get; }
	
	/// <remarks>
	/// <paramref name="defaultValue"/> is assumed to be valid.
	/// </remarks>
	public Field(T defaultValue) {
		DefaultValue = defaultValue;
		valueBacking = DefaultValue;
	}

}