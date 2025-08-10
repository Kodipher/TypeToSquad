using Godot;
using System;


namespace TypeToSquad.Model.Settings;


// A non-generic inhertiance root
// to avoid getting data via reflection
interface IVariantSavable {
	public Variant ValueAsSavable { get; set; }
}


public abstract class Field : IVariantSavable { 
	public abstract Variant ValueAsSavable { get; set; }
	public abstract Variant ValueForceValid(Variant value);
}


/// <summary>
/// A storage for a value, together with a validator.
/// The stored value should always be valid.
/// </summary>
/// <remarks>
/// <para>Default <see cref="ValueAsSavable"/> looses information for more complex times.</para>
/// <para>Default validation check only blocks <see langword="null"/>.</para>
/// </remarks>
public class Field<[MustBeVariant] T> : Field where T : notnull {

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
	public override Variant ValueAsSavable {
		get => Variant.From(in this.value);
		set => Value = value.As<T>();
	}


	public static implicit operator T(Field<T> field) => field.Value;

	#endregion

	#region //// Validation

	public T DefaultValue { get; private init; }

	/// <summary>
	/// Returns given value if it is valid.
	/// Otherwise returns some valid value, usually the default.
	/// </summary>
	/// <remarks>
	/// Does not set <see cref="Value"/> on its own.
	/// </remarks>
	public virtual T ValueForceValid(T value) => value is not null ? value : DefaultValue;
	
	/// <summary>
	/// Equivalent of <see cref="ValueForceValid(T)"/> 
	/// but for <see cref="Variant"/> values.
	/// </summary>
	public override Variant ValueForceValid(Variant value) => Variant.From(ValueForceValid(value.As<T>()));

	#endregion

	/// <remarks>
	/// <paramref name="defaultValue"/> is assumed to be valid.
	/// </remarks>
	public Field(T defaultValue) {
		DefaultValue = defaultValue;
		value = DefaultValue;
	}

}