using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A <see cref="Field{T}"/> that stores ahead-of-time known enum values.
/// Values are stored as strings internally.
/// </summary>
public class FieldOptionsEnum<[MustBeVariant] TEnum> : Field<string> where TEnum : struct, Enum {

	public TEnum ValueAsEnum {
		get {

			if (Enum.TryParse(Value, out TEnum enumValue)) return enumValue;

			this.value = DefaultValue;
			GD.PushWarning($"Value stored in {nameof(FieldOptionsEnum<TEnum>)} was invalid. Resetting value to default.");
			return Enum.Parse<TEnum>(Value);
		}
		set => Value = value.ToString();
	}

	public override bool IsValid(string value) => Enum.IsDefined(typeof(TEnum), value);

	public FieldOptionsEnum(TEnum defaultValue) : base(defaultValue.ToString()) { }

}