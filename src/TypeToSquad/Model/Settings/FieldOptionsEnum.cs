using Godot;
using System;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores ahead-of-time known enum values.</summary>
public class FieldOptionsEnum<[MustBeVariant] TEnum> : Field<TEnum> where TEnum : struct, Enum {

	public override Variant ValueAsSavable { 
		get => this.value.ToString(); 
		set {
			
			if (value.VariantType == Variant.Type.String) {

				if (Enum.TryParse(value.AsString(), out TEnum enumValue)) {
					Value = enumValue;
					return;
				}

				this.value = DefaultValue;
				return;

			} 
			
			if (value.VariantType == Variant.Type.Int) {
				Value = value.As<TEnum>();
				return;
			}

			Value = DefaultValue;
		}
	}

	public override bool IsValid(TEnum value) => Enum.IsDefined(typeof(TEnum), value);

	public FieldOptionsEnum(TEnum defaultValue) : base(defaultValue) { }

}