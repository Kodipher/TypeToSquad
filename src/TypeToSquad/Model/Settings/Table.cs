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
/// Stores data as arrays of <see cref="Field"/>s,
/// ensuring all values are always valid.
/// </para>
/// <para>
/// See <see cref="Table{TRowTuple}"/> for implementation.
/// </para>
/// </summary>
public abstract class Table : IVariantSavable {

	public abstract Variant ToSavableVariant();
	public abstract void SetFromVariant(Variant value);
	
	public abstract int Count { get; }
	public abstract int ColumnCount { get; }

	public abstract IReadOnlyList<Field> GetFieldsAt(int index);
	
	public abstract void AddEmpty();
	public abstract void InsertEmpty(int index);

	public abstract void MoveRow(int indexFrom, int indexTo);
	
	public abstract void RemoveAt(int index);
	public abstract void Clear();
}


/// <inheritdoc cref="Table"/>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "StaticMemberInGenericType")] 
public class Table<TRowTuple> : Table, IList<TRowTuple>, IReadOnlyList<TRowTuple> where TRowTuple: struct, ITuple {

	public Table(params Func<Field>[] rowPrototype) {

		if (rowPrototype.Length != ColumnCount) {
			throw new ArgumentException($"Column count mismatch. Tuple has {ColumnCount} columns but {rowPrototype.Length} delegates were given", nameof(rowPrototype));
		}

		this.rowPrototype = rowPrototype;
	}

	#region /--- Prototype row ---/

	readonly Func<Field>[] rowPrototype;

	/// <summary>
	/// Changes the type of <see cref="Field"/> for a particular column
	/// and transfers the values to new instances of the prototype.
	/// </summary>
	public void ChangePrototypeForColumn(int columnIndex, Func<Field> prototype) {
		rowPrototype[columnIndex] = prototype;

		for (int i = 0; i < Count; i++) {
			Variant savedValue = rows[i][columnIndex].ValueVariant;
			rows[i][columnIndex] = prototype();
			rows[i][columnIndex].ValueVariant = savedValue;
		}
	}
	
	Field[] CreateEmptyRow() {
		
		Field[] newRow = new Field[ColumnCount];
		for (int i = 0; i < ColumnCount; i++) {
			newRow[i] = rowPrototype[i]();
		}

		return newRow;
	}

	#endregion

	readonly List<Field[]> rows = new();
	
	/// <summary>Number of rows in this table.</summary>
	public override int Count => rows.Count;
	
	public sealed override int ColumnCount => tupleTypes.Length;

	public TRowTuple this[int index] {
		get {
			Variant[] variantArray = new Variant[ColumnCount];
			for (int j = 0; j < ColumnCount; j++) {
				variantArray[j] = rows[index][j].ValueVariant;
			}
			
			return ArrayToTuple(variantArray);
		}
		set {
			Variant[] values = TupleToArray(value);
			for (int j = 0; j < ColumnCount; j++) {
				rows[index][j].ValueVariant = values[j];
			}
		}
	}
	
	public override IReadOnlyList<Field> GetFieldsAt(int index) => rows[index].AsReadOnly();

	public override void AddEmpty() => rows.Add(CreateEmptyRow());
	public override void InsertEmpty(int index) => rows.Insert(index, CreateEmptyRow());
	
	public void Add(TRowTuple row) {
		AddEmpty();
		this[Count - 1] = row;
	}
	
	public void Insert(int index, TRowTuple row) {
		InsertEmpty(index);
		this[index] = row;
	}

	public override void MoveRow(int indexFrom, int indexTo) {
		var row = rows[indexFrom];
		rows.RemoveAt(indexFrom);
		rows.Insert(indexTo, row);
	}

	public bool Remove(TRowTuple row) => throw new NotImplementedException();
	public override void RemoveAt(int index) => rows.RemoveAt(index);
	public override void Clear() => rows.Clear();
	
	public int IndexOf(TRowTuple row) => throw new NotImplementedException();
	public bool Contains(TRowTuple row) => throw new NotImplementedException();

	public void CopyTo(TRowTuple[] array, int arrayIndex) {
		TRowTuple[] rowsOfTuples = this.ToArray();
		rowsOfTuples.CopyTo(array, arrayIndex);
	}

	public IEnumerator<TRowTuple> GetEnumerator() {
		for (int i = 0; i < Count; i++) {
			yield return this[i];
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();

	public bool IsReadOnly => false;
	
	public override Variant ToSavableVariant() {
		Godot.Collections.Array savableArray = new();

		foreach (var tupleRow in this) {
			savableArray.Add(new Godot.Collections.Array(TupleToArray(tupleRow)));
		}

		return savableArray;
	}

	public override void SetFromVariant(Variant value) {
		foreach (var rowSource in value.AsGodotArray()) {
			Add(ArrayToTuple(rowSource.AsGodotArray()));
		}
	}
	
	#region /--- Array Conversion ---/
	
	static readonly Type[] tupleTypes = typeof(TRowTuple).IsGenericType ?
											typeof(TRowTuple).GenericTypeArguments : 
											Array.Empty<Type>();
	
	static readonly MethodInfo tupleCreateMethod = 
										typeof(ValueTuple)
											.GetMethods(BindingFlags.Public | BindingFlags.Static)
											.Where(method => method.Name == nameof(ValueTuple.Create))
											.Single(method => method.GetGenericArguments().Length == tupleTypes.Length)
											.MakeGenericMethod(tupleTypes);
	
	/// <summary>Converts a variant array into a tuple row of this table.</summary>
	static TRowTuple ArrayToTuple(Variant[] array) {

		// Force correct size
		if (array.Length != tupleTypes.Length) {

			Variant[] lengthCheckedArray = new Variant[tupleTypes.Length];

			int n = Math.Min(array.Length, lengthCheckedArray.Length);
			for (int i = 0; i < n; i++) {
				lengthCheckedArray[i] = array[i];
			}

			array = lengthCheckedArray;
		}

		// Convert values to objects
		object?[] tupleValues = new object?[tupleTypes.Length];
		for (int i = 0; i < tupleValues.Length; i++) {
			tupleValues[i] = array[i].AsUnsafe(tupleTypes[i]);
		}

		// Create tuple
		return (TRowTuple)(tupleCreateMethod.Invoke(null, tupleValues) ?? default(TRowTuple));
	}

	/// <inheritdoc cref="ArrayToTuple(Variant[])"/>
	static TRowTuple ArrayToTuple(Godot.Collections.Array array) {
		return ArrayToTuple(array.ToArray());
	}

	/// <summary>Converts a tuple into a variant array.</summary>
	static Variant[] TupleToArray(TRowTuple tuple) {
		Variant[] rowArray = new Variant[tuple.Length];

		for (int i = 0; i < tuple.Length; i++) {
			var tupleItem = tuple[i];
			rowArray[i] = GodotExtensions.VariantFromUnsafe(tupleItem);
		}

		return rowArray;
	}
	
	#endregion
	
}
