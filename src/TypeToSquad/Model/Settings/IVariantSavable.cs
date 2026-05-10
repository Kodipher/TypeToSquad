using Godot;


namespace TypeToSquad.Model.Settings;


// A non-generic inheritance root
// to avoid getting data via reflection
internal interface IVariantSavable {

	/// <summary>Gets content as <see cref="Variant"/> for purposes of saving.</summary>
	Variant ToSavableVariant();

	/// <summary>Sets content from <see cref="Variant"/>. Used for loading.</summary>
	void SetFromVariant(Variant value);
}
