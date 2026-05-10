using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores ahead-of-time known enum values.</summary>
public class FieldOptionsEnum<[MustBeVariant] TEnum> : Field<TEnum> where TEnum : struct, Enum {

	public override Variant ToSavableVariant() => Value.ToString();

	public override void SetFromVariant(Variant value) {
		
		switch (value.VariantType) {
			
			case Variant.Type.String: {
				
				if (
					Enum.TryParse(value.AsString(), out TEnum enumValue) || 
					Enum.TryParse(value.AsString().ToPascalCase(), out enumValue)
				) {
					Value = enumValue;
					return;
				}

				goto default;
			}
				
			case Variant.Type.Int:
			case Variant.Type.Float:
				Value = ReturnValid(value.As<TEnum>());
				break;
			
			default:
				Value = DefaultValue;
				return;
		}
		
	}

	protected override TEnum ReturnValid(TEnum value) {
		return Enum.IsDefined(value) ? value : DefaultValue;
	}

	public FieldOptionsEnum(TEnum defaultValue) : base(defaultValue) {
		if (!Enum.IsDefined(DefaultValue)) {
			throw new ArgumentException("Default value is not defined in the enum.");
		}
	}

}