using Godot;
using System;
using System.Numerics;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores integers in inclusive range.</summary>
public class FieldNumericRange<[MustBeVariant] T> : Field<T> where T: INumber<T> {

	public T MinInclusive { get; private init; }
	public T MaxInclusive { get; private init; }

	public override bool IsValid(T value) {
		return MinInclusive <= value && value <= MaxInclusive;
	}

	public override T ValueForceValid(T value) {
		if (T.IsNaN(value)) value = T.Zero;
		if (value < MinInclusive) return MinInclusive;
		if (value > MaxInclusive) return MaxInclusive;
		return value;
	}

	public FieldNumericRange(T minInclusive, T maxInclusive, T defaultValue) : base(defaultValue) {
		MinInclusive = minInclusive;
		MaxInclusive = maxInclusive;
	}

}
