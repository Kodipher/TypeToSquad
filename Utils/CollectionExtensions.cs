using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace Kodipher.TypeToSquad.Utils;


public static class CollectionExtensions {

	/// <summary>
	/// Calls a constructor of ReadOnlyDictionary with fluent syntax
	/// </summary>
	public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	where TKey : notnull	
	{
        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

	/// <summary>
	/// Calls a constructor of ReadOnlyCollection with fluent syntax
	/// </summary>
	public static ReadOnlyCollection<T> AsReadOnly<T>(this T[] array) {
        return new ReadOnlyCollection<T>(array);
    }

}
