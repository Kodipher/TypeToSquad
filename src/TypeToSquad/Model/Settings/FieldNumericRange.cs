using Godot;
using System;
using System.Numerics;
using System.Reflection;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores numbers in inclusive range.</summary>
public class FieldNumericRange<[MustBeVariant] T> : Field<T> where T: struct, INumber<T> {

	public T MinInclusive { get; }
	public T MaxInclusive { get; }
	public int DigitsPrecision { get; }

	/// <summary>
	/// The <c>Round(T, int)</c> method of <typeparamref name="T"/>, if exists.
	/// (typically <see cref="IFloatingPoint{TSelf}.Round(TSelf, int)"/>, but not guaranteed)
	/// </summary>
	public static readonly MethodInfo? RoundPrecisionMethod = typeof(T)
														.GetMethod(
															nameof(float.Round),
															BindingFlags.Public | BindingFlags.Static,
															[typeof(T), typeof(int)]
														);
	
	protected override T ReturnValid(T value) {
		
		if (RoundPrecisionMethod is not null) {
			object? roundCallResult = RoundPrecisionMethod.Invoke(null, [value, DigitsPrecision]);
			value = (T)(roundCallResult ?? throw new NullReferenceException("Round method returned null."));
		}
		
		if (T.IsNaN(value)) value = T.Zero;
		if (value < MinInclusive) return MinInclusive;
		if (value > MaxInclusive) return MaxInclusive;
		return value;
	}
	
	public FieldNumericRange(T minInclusive, T maxInclusive, T defaultValue, int digitsPrecision = 0) : base(defaultValue) {
		MinInclusive = minInclusive;
		MaxInclusive = maxInclusive;
		DigitsPrecision = digitsPrecision;
	}

}
