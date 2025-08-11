using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;


using TypeToSquad.Utils;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A savable array of value tuples.
/// Only <see cref="ValueTuple"/>s of <see cref="Variant"/> compatible values
/// are supported.
/// </summary>
public class Table<TRowTuple> : IVariantSavable, IList<TRowTuple>, IReadOnlyList<TRowTuple> 
where TRowTuple: struct, ITuple 
{

	#region //// Storage, IList

	readonly List<TRowTuple> rows = new();

	public TRowTuple this[int index] {
		get => rows[index];
		set => rows[index] = ReturnValidRow(value);
	}

	public int Count => rows.Count;

	public void Add(TRowTuple row) => rows.Add(ReturnValidRow(row));
	public void Insert(int index, TRowTuple row) => rows.Insert(index, ReturnValidRow(row));

	public void RemoveAt(int index) => rows.RemoveAt(index);
	public bool Remove(TRowTuple row) => rows.Remove(row);
	public void Clear() => rows.Clear();

	public int IndexOf(TRowTuple row) => rows.IndexOf(row);
	public bool Contains(TRowTuple row) => rows.Contains(row);

	public void CopyTo(TRowTuple[] array, int arrayIndex) => rows.CopyTo(array, arrayIndex);

	public IEnumerator<TRowTuple> GetEnumerator() => rows.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();

	public bool IsReadOnly => false;

	#endregion

	public Variant ToSavableVariant() {
		Godot.Collections.Array savableArray = new();

		foreach (var row in rows) {
			savableArray.Add(new Godot.Collections.Array(TupleToArray(row)));
		}

		return savableArray;
	}

	public void SetFromVariant(Variant value) {
		foreach (var rowSource in value.AsGodotArray()) {
			this.Add(ArrayToTuple(rowSource.AsGodotArray()));
		}
	}

	#region //// Array Conversion

	readonly static Lazy<Type[]> tupleTypes = new(() => {
		if (typeof(TRowTuple).IsGenericType) return typeof(TRowTuple).GenericTypeArguments;
		return Array.Empty<Type>();
	});

	readonly static Lazy<MethodInfo> tupleCreateMethod = new(() => {
		return typeof(ValueTuple)
			.GetMethods()
			.Where(method => method.Name == nameof(ValueTuple.Create))
			.Where(method => method.IsStatic)
			.Where(method => method.GetGenericArguments().Length == tupleTypes.Value.Length)
			.Single()
			.MakeGenericMethod(tupleTypes.Value);
	});

	/// <summary>Converts a variant array into a tupple row of this table.</summary>
	public static TRowTuple ArrayToTuple(Variant[] array) {

		// Get values
		Variant[] legnthCheckedArray = new Variant[tupleTypes.Value.Length];

		int n = Math.Min(array.Length, legnthCheckedArray.Length);
		for (int i = 0; i < n; i++) {
			legnthCheckedArray[i] = array[i];
		}

		// Convert values to objects
		object?[] tupleValues = new object?[tupleTypes.Value.Length];
		for (int i = 0; i < tupleValues.Length; i++) {
			tupleValues[i] = legnthCheckedArray[i].AsUnsafe(tupleTypes.Value[i]);
		}

		// Create tuple
		return (TRowTuple)(
					tupleCreateMethod
					.Value
					.Invoke(null, tupleValues)
					?? default(TRowTuple)
				);
	}

	/// <inheritdoc cref="ArrayToTuple(Variant[])"/>
	public static TRowTuple ArrayToTuple(Godot.Collections.Array array) {
		return ArrayToTuple(array.ToArray());
	}

	/// <summary>Converts a tuple into a variant array.</summary>
	public static Variant[] TupleToArray(TRowTuple tuple) {
		Variant[] rowArray = new Variant[tuple.Length];

		for (int i = 0; i < tuple.Length; i++) {
			var tupleItem = tuple[i];
			rowArray[i] = GodotExtensions.VariantFromUnsafe(tupleItem);
		}

		return rowArray;
	}

	#endregion

	#region //// Validation

	Field?[]? validators = null; // is same length as tuple, checked in SetValidationProxies

	public void ClearValidationProxies() => validators = null;

	/// <summary>
	/// Sets validators to be used for specific columns.
	/// A <see langword="null"/> means the column is not validated.
	/// </summary>
	/// <remarks>
	/// Only <see cref="Field.ReturnValid(Variant)"/> is used.
	/// Values inside <see cref="Field"/>s are not written to or read.
	///	</remarks>
	public void SetValidationProxies(params Field?[] validators) {

		// Guards
		ArgumentNullException.ThrowIfNull(validators);
		
		if (validators.Length != tupleTypes.Value.Length) {
			throw new ArgumentException($"Validators length mismatch. Expected length of {tupleTypes.Value.Length}, got {validators.Length}");
		}

		// Set validators (copy array)
		this.validators = validators.ToArray();

		// Ensure rows are valid
		RevalidateAllRows();
	}

	public virtual TRowTuple ReturnValidRow(TRowTuple value) {

		// No validators sets
		if (validators is null) return value;

		// Define simple equality test for later
		// No built-in == exists for Variants
		// so this is a simple implementation for most common types.
		//
		// Returns true if variants are known to equal
		// Returns false if variants are known to not equal or equality is unknown		
		static bool VariantsKnownEqual(in Variant v1, in Variant v2) {
			
			if (v1.VariantType != v2.VariantType) return false;

			return v1.VariantType switch {
				Variant.Type.Nil => true,
				Variant.Type.Bool => v1.AsBool() == v2.AsBool(),
				Variant.Type.Int => v1.AsInt64() == v2.AsInt64(),
				Variant.Type.Float => v1.AsDouble() == v2.AsDouble(),
				Variant.Type.String => v1.AsString() == v2.AsString(),
				_ => false
			};
		}

		// Perform validation
		bool anyChanged = false;
		Variant[] rowValues = TupleToArray(value);

		for (int i = 0; i < rowValues.Length; i++) {
			if (validators[i] is null) continue;

			Variant oldValue = rowValues[i];
			Variant newValue = validators[i]!.ReturnValid(oldValue);

			// basic ref comparison
			// because there is no == between Variants
			anyChanged = anyChanged || !VariantsKnownEqual(in oldValue, in newValue);

			rowValues[i] = newValue;
		}

		if (anyChanged) return ArrayToTuple(rowValues);
		return value;
	}

	/// <summary>Forces all current rows through <see cref="ReturnValidRow(TRowTuple)"/>.</summary>
	public void RevalidateAllRows() {
		for (int i = 0; i < this.Count; i++) {
			this[i] = ReturnValidRow(this[i]);
		}
	}

	#endregion

}
