using Godot;
using System;


namespace TypeToSquad.Model.Settings;


interface IVariantSavable {
	// To avoid getting properties via reflection
	public Variant ValueAsSavable { get; set; }
}


/// <summary>
/// A storage for a value, together with a validator.
/// The stored value should always be valid.
/// </summary>
/// <remarks>
/// <para>Default <see cref="ValueAsSavable"/> looses information for more complex times.</para>
/// <para>Default validation check only blocks <see langword="null"/>.</para>
/// </remarks>
public class Field<[MustBeVariant] T> : IVariantSavable where T : notnull {

	#region //// Value

	/// <summary>
	/// The raw internal value of the <see cref="Field{T}"/>. 
	/// Bypasses validation.
	/// </summary>
	protected T value;

	/// <summary>
	/// Gets or sets the value of this <see cref="Field{T}"/>.
	/// When setting, values are forced to become valid if aren't.
	/// </summary>
	public T Value { 
		get => this.value;
		set => this.value = ValueForceValid(value);
	}

	/// <summary>
	/// Gets or sets <see cref="Value"/> as if it was <see cref="Variant"/>.
	/// Used for saving and loading.
	/// </summary>
	public virtual Variant ValueAsSavable {
		get => Variant.From(in this.value);
		set => Value = value.As<T>();
	}

	#endregion

	#region //// Validation

	public T DefaultValue { get; private init; }

	/// <summary>
	/// Returns given value if it is valid.
	/// Otherwise returns some valid value, usually the default.
	/// </summary>
	public virtual T ValueForceValid(T value) => IsValid(value) ? value : DefaultValue;

	public virtual bool IsValid(T value) => value is not null;

	#endregion

	/// <remarks>
	/// <paramref name="defaultValue"/> must be valid. Checked using <see cref="IsValid(T)"/>.
	/// </remarks>
	public Field(T defaultValue) {
		DefaultValue = defaultValue;
		value = DefaultValue;
	}

}