using System;


namespace TypeToSquad.Model.Settings;


/// <summary>A <see cref="Field{T}"/> that stores integers in inclusive range.</summary>
public class FieldIntRange : Field<int> {

	public int MinInclusive { get; private init; }
	public int MaxInclusive { get; private init; }

	public override bool IsValid(int value) {
		return MinInclusive <= value && value <= MaxInclusive;
	}

	public override int ValueForceValid(int value) {
		if (value < MinInclusive) return MinInclusive;
		if (value > MaxInclusive) return MaxInclusive;
		return value;
	}

	public FieldIntRange(int minInclusive, int maxInclusive, int defaultValue) : base(defaultValue) {
		MinInclusive = minInclusive;
		MaxInclusive = maxInclusive;
	}

}