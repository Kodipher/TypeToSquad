using Godot;


namespace TypeToSquad.Model.Settings;


/// <summary>
/// A <see cref="Field{T}"/> that stores key-value pairs 
/// with keys being strings.
/// </summary>
/// <remarks>
/// Avoid accessing <see cref="Field{T}.Value"/> and mutating it directly 
/// as that bypasses automatic validation.
/// </remarks>
public class FieldDictionary<[MustBeVariant] TValue> : Field<Godot.Collections.Dictionary<string, TValue>> where TValue: notnull {
	
	public override Variant ValueAsSavable {
		get => base.ValueAsSavable;
		set {
			this.value = new();
			var untypedSource = value.AsGodotDictionary();

			foreach (var key in untypedSource.Keys) {
				this.value[key.AsString()] = ValueForceValidIndividual(untypedSource[key].As<TValue>());
			}
		}
	}

	#region //// Dictionary Proxy

	public TValue this[string key] {
		get => this.value[key];
		set	=> this.value[key] = ValueForceValidIndividual(value);
	}

	public bool Remove(string key) => Value.Remove(key);

	public System.Collections.Generic.ICollection<string> Keys => Value.Keys;

	public int Count => Value.Count;

	#endregion

	#region //// Validation By Proxy

	Field<TValue>? validatorFieldProxy = null;

	public Field<TValue>? ValueValidator { 
		get => validatorFieldProxy;
		set {
			validatorFieldProxy = value;
			EveryValueForceValid();
		}
	}

	protected virtual TValue ValueForceValidIndividual(TValue value) {
		if (validatorFieldProxy is null) return value;

		// Validate using proxy
		validatorFieldProxy.Value = value;
		TValue ret = validatorFieldProxy.Value;

		// Clear teporary storage
		validatorFieldProxy.Value = validatorFieldProxy.DefaultValue;

		return ret;
	}

	/// <summary>Pass each dictionary value through the validator.</summary>
	public void EveryValueForceValid() {
		if (validatorFieldProxy is null) return;
		foreach (var key in this.value.Keys) {
			this.value[key] = ValueForceValidIndividual(this.value[key]);
		}
	}

	#endregion


	///<remarks>Default value is passed in by refernce and is assumed to not mutate.</remarks>
	public FieldDictionary(Godot.Collections.Dictionary<string, TValue> defaultValue) : base(defaultValue) {
		value = DefaultValue.Duplicate(deep: true); // properly copy default into current
	}

	public FieldDictionary() : this(new()) { }

}
