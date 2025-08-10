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
/// Only <see cref="ValueTuple"/>s of <see cref="Variant"/> comparible values
/// are supported.
/// </summary>
public class Table<TRowTuple> : IVariantSavable, IList<TRowTuple>, IReadOnlyList<TRowTuple> 
where TRowTuple: struct, ITuple 
{

	#region //// Storage, IList

	readonly List<TRowTuple> rows = new();

	public TRowTuple this[int index] {
		get => rows[index];
		set => rows[index] = RowForceValid(value);
	}

	public int Count => rows.Count;

	public void Add(TRowTuple row) => rows.Add(RowForceValid(row));
	public void Insert(int index, TRowTuple row) => rows.Insert(index, RowForceValid(row));

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

	public Variant ValueAsSavable {
		get {
			Godot.Collections.Array savableArray = new();

			foreach (var row in rows) {
				savableArray.Add(TupleToArray(row));
			}

			return savableArray;
		}
		set {
			foreach (var rowSource in value.AsGodotArray()) {
				this.Add(ArrayToTuple(rowSource.AsGodotArray()));
			}
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
	public static TRowTuple ArrayToTuple(Godot.Collections.Array array) {

		// Get values
		Variant[] tupleValuesVaraint = new Variant[tupleTypes.Value.Length];

		int n = Math.Min(array.Count, tupleValuesVaraint.Length);
		for (int i = 0; i < n; i++) {
			tupleValuesVaraint[i] = array[i];
		}

		// Convert values to objects
		object?[] tupleValues = new object?[tupleTypes.Value.Length];
		for (int i = 0; i < tupleValues.Length; i++) {
			tupleValues[i] = tupleValuesVaraint[i].AsUnsafe(tupleTypes.Value[i]);
		}

		// Create tuple
		return ArrayToTuple(tupleValues);
	}

	protected static TRowTuple ArrayToTuple(object?[] tupleValues) {
		return (TRowTuple)(
					tupleCreateMethod
					.Value
					.Invoke(null, tupleValues)
					?? default(TRowTuple)
				);
	}

	/// <summary>Converts a tuple into a variant array.</summary>
	static Godot.Collections.Array TupleToArray(TRowTuple tuple) {
		Godot.Collections.Array rowArray = new();

		for (int i = 0; i < tuple.Length; i++) {
			var tupleItem = tuple[i];
			rowArray.Add(GodotExtensions.VariantFromUnsafe(tupleItem));
		}

		return rowArray;
	}

	#endregion



	public virtual TRowTuple RowForceValid(TRowTuple value) => value;

}
