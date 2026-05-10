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
	public abstract Variant ToSavableVariant();
	public abstract void SetFromVariant(Variant value);
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
	
	protected virtual T ReturnValid(T value) => value;
	
	public override Variant ToSavableVariant() => Variant.From(in valueBacking);
	
	public override void SetFromVariant(Variant value) => Value = value.As<T>();
	
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