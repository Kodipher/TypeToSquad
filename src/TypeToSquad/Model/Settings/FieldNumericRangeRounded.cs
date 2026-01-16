using Godot;
using System;
using System.Numerics;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A <see cref="FieldNumericRange{T}"/> with a rounding precision option.
/// Made for and requires a <see cref="IFloatingPoint{TSelf}"/>.
/// </summary>
public class FieldNumericRangeRounded<[MustBeVariant] T> : FieldNumericRange<T> where T: struct, IFloatingPoint<T> {

	public int DigitsPrecision { get; private init; }

	public override T ReturnValid(T value) {
		value = T.Round(value, DigitsPrecision);
		return base.ReturnValid(value);
	}

	public FieldNumericRangeRounded(T minInclusive, T maxInclusive, T defaultValue, int digitsPrecision = 6) : 
	base(minInclusive, maxInclusive, defaultValue) 
	{
		DigitsPrecision = digitsPrecision;
	}

}
