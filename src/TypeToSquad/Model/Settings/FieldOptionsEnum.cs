using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores ahead-of-time known enum values.</summary>
public class FieldOptionsEnum<[MustBeVariant] TEnum> : Field<TEnum> where TEnum : struct, Enum {

	public override Variant ToSavableVariant() => this.value.ToString();

	public override void SetFromVariant(Variant value) {
		Value = ReturnValid(value).As<TEnum>();
	}

	public override TEnum ReturnValid(TEnum value) {
		if (!Enum.IsDefined(value)) return DefaultValue;
		return value;
	}

	public override Variant ReturnValid(Variant value) {

		if (value.VariantType == Variant.Type.String) {

			if (Enum.TryParse(value.AsString(), out TEnum enumValue)) {
				return Variant.From(enumValue);
			}

			if (Enum.TryParse(value.AsString().ToPascalCase(), out enumValue)) {
				return Variant.From(enumValue);
			}

			return Variant.From(DefaultValue);
		}

		if (value.VariantType == Variant.Type.Int || value.VariantType == Variant.Type.Float) {
			if (!Enum.IsDefined(value.As<TEnum>())) return Variant.From(DefaultValue);
			return value;
		}

		return Variant.From(DefaultValue);
	}

	public FieldOptionsEnum(TEnum defaultValue) : base(defaultValue) {
		if (!Enum.IsDefined(DefaultValue)) {
			throw new ArgumentException("Default value is not defined in the enum.");
		}
	}

}