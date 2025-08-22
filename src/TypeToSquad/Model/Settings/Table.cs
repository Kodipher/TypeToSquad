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
/// <para>
/// A savable array of value tuples.
/// Only <see cref="ValueTuple"/>s of <see cref="Variant"/> compatible values
/// are supported.
/// </para>
/// <para>
/// Can convert between tuples and <see cref="Variant"/> arrays.
/// Corrects length automatically.
/// </para>
/// <para>
/// See <see cref="Table{TRowTuple}"/> for implementation.
/// </para>
/// </summary>
public abstract class Table : IVariantSavable {

	public abstract Variant ToSavableVariant();
	public abstract void SetFromVariant(Variant value);

	public abstract Variant[] GetAtAsArray(int index);
	public abstract void SetAtAsArray(int index, Variant[] array);
	public abstract int Count { get; }

	public abstract void AddAsArray(Variant[] array);
	public abstract void InsertAsArray(int index, Variant[] array);
	public abstract void RemoveAt(int index);
	public abstract void Clear();

	public abstract Field?[] GetValidationProxies();
	public abstract void RevalidateAllRows();

	public abstract int ColumnCount { get; }
}


/// <inheritdoc cref="Table"/>
public class Table<TRowTuple> : Table, IList<TRowTuple>, IReadOnlyList<TRowTuple> 
where TRowTuple: struct, ITuple 
{

	#region //// Storage, IList

	readonly List<TRowTuple> rows = new();

	public TRowTuple this[int index] {
		get => rows[index];
		set => rows[index] = ReturnValidRow(value);
	}

	public override Variant[] GetAtAsArray(int index) => TupleToArray(this[index]);
	public override void SetAtAsArray(int index, Variant[] array) => rows[index] = ReturnValidRow(array);

	public override int Count => rows.Count;

	public void Add(TRowTuple row) => rows.Add(ReturnValidRow(row));
	public override void AddAsArray(Variant[] array) => rows.Add(ReturnValidRow(array));
	public void Insert(int index, TRowTuple row) => rows.Insert(index, ReturnValidRow(row));
	public override void InsertAsArray(int index, Variant[] array) => rows.Insert(index, ReturnValidRow(array));

	public override void RemoveAt(int index) => rows.RemoveAt(index);
	public bool Remove(TRowTuple row) => rows.Remove(row);
	public override void Clear() => rows.Clear();

	public int IndexOf(TRowTuple row) => rows.IndexOf(row);
	public bool Contains(TRowTuple row) => rows.Contains(row);

	public void CopyTo(TRowTuple[] array, int arrayIndex) => rows.CopyTo(array, arrayIndex);

	public IEnumerator<TRowTuple> GetEnumerator() => rows.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();

	public bool IsReadOnly => false;

	#endregion

	public override Variant ToSavableVariant() {
		Godot.Collections.Array savableArray = new();

		foreach (var row in rows) {
			savableArray.Add(new Godot.Collections.Array(TupleToArray(row)));
		}

		return savableArray;
	}

	public override void SetFromVariant(Variant value) {
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
	protected static TRowTuple ArrayToTuple(Variant[] array) {

		// Force correct size
		if (array.Length != tupleTypes.Value.Length) {

			Variant[] legnthCheckedArray = new Variant[tupleTypes.Value.Length];

			int n = Math.Min(array.Length, legnthCheckedArray.Length);
			for (int i = 0; i < n; i++) {
				legnthCheckedArray[i] = array[i];
			}

			array = legnthCheckedArray;
		}

		// Convert values to objects
		object?[] tupleValues = new object?[tupleTypes.Value.Length];
		for (int i = 0; i < tupleValues.Length; i++) {
			tupleValues[i] = array[i].AsUnsafe(tupleTypes.Value[i]);
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
	protected static TRowTuple ArrayToTuple(Godot.Collections.Array array) {
		return ArrayToTuple(array.ToArray());
	}

	/// <summary>Converts a tuple into a variant array.</summary>
	protected static Variant[] TupleToArray(TRowTuple tuple) {
		Variant[] rowArray = new Variant[tuple.Length];

		for (int i = 0; i < tuple.Length; i++) {
			var tupleItem = tuple[i];
			rowArray[i] = GodotExtensions.VariantFromUnsafe(tupleItem);
		}

		return rowArray;
	}

	public override int ColumnCount => tupleTypes.Value.Length;

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
		
		if (validators.Length != ColumnCount) {
			throw new ArgumentException($"Validators length mismatch. Expected length of {ColumnCount}, got {validators.Length}");
		}

		// Set validators (copy array)
		this.validators = validators.ToArray();

		// Ensure rows are valid
		RevalidateAllRows();
	}

	/// <remarks>If proxies were not set, returns an array of nulls.</remarks>
	public override Field?[] GetValidationProxies() {
		if (this.validators is null) return new Field?[this.ColumnCount];
		return this.validators.ToArray();
	}

	public virtual TRowTuple ReturnValidRow(TRowTuple row) {

		// No validators sets
		if (validators is null) return row;

		// Validate as array
		Variant[] rowValues = TupleToArray(row);
		return ReturnValidRow(rowValues);
	}

	/// <remarks>Input array is not mutated.</remarks>
	public virtual TRowTuple ReturnValidRow(params Variant[] values) {

		// No validators sets
		if (validators is null) return ArrayToTuple(values);

		// Copy row as array
		Variant[] rowValues = new Variant[ColumnCount];

		// Validate each item
		for (int i = 0; i < rowValues.Length; i++) {

			if (validators[i] is null) {
				rowValues[i] = values[i];
				continue;
			}

			rowValues[i] = validators[i]!.ReturnValid(values[i]);
		}

		return ArrayToTuple(rowValues);
	}

	/// <summary>Forces all current rows through <see cref="ReturnValidRow(TRowTuple)"/>.</summary>
	public override void RevalidateAllRows() {
		for (int i = 0; i < this.Count; i++) {
			this[i] = ReturnValidRow(this[i]);
		}
	}

	#endregion

}
