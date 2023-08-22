using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace Kodipher.TypeToSquad.Utils;


public static class CollectionExtensions {

    /// <summary>
    /// Calls a constructor of ReadOnlyDictionary using fluent syntax
    /// </summary>
    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	where TKey : notnull	
	{
        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Calls a constructor of ReadOnlyCollection using fluent syntax
    /// </summary>
    public static ReadOnlyCollection<T> AsReadOnly<T>(this T[] array) {
        return new ReadOnlyCollection<T>(array);
    }

	/// <summary>
	/// Calls a constructor of Godot.Collections.Dictionary using fluent syntax
	/// </summary>
	public static Godot.Collections.Dictionary<TKey, TValue> ToGodotDictionary<[Godot.MustBeVariant] TKey, [Godot.MustBeVariant] TValue>(this IDictionary<TKey, TValue> dict) {
		return new Godot.Collections.Dictionary<TKey, TValue>(dict);
	}

}
