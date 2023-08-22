using System;


namespace Kodipher.TypeToSquad.Utils;


public static class ReflectionExtensions {

	/// <summary>
	/// Returns true if the given derived type is base type or a subclas of it.
	/// Supports generics. Does not support interfaces.
	/// </summary>
	// Based on https://stackoverflow.com/questions/982487/testing-if-object-is-of-generic-type-in-c-sharp
	public static bool IsSubcalssOrSelfOf(this Type? derivedType, Type baseType) {

		while (derivedType is not null) {

			if (derivedType == baseType) {
				return true;
			}

			if (derivedType.IsGenericType && derivedType.GetGenericTypeDefinition() == baseType) {
				return true;
			}

			derivedType = derivedType.BaseType;
		}

		return false;

	}

}
