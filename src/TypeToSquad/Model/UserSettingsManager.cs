using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Reflection;

using TypeToSquad.Model.Settings;


namespace TypeToSquad.Model;


public partial class UserSettingsManager : Node {

	/// <summary>
	/// Current settings.
	/// When settings are loaded or update this instance is changed
	/// instead of a new instance being created.
	/// </summary>
	public UserSettings Settings { get; private init; } = new();

	public override void _Ready() {
		StageSingletonInstance();
		Load();
	}

	#region //// Singleton

	public static UserSettingsManager Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion

	#region //// Saving and Loading

	public const string SettingsFilepath = "user_settings.json";

	readonly static ReadOnlyCollection<FieldInfo> UserSettingsSavableFields = 
									typeof(UserSettings)
									.GetFields(BindingFlags.Instance | BindingFlags.Public)
									.Where(fi => fi.FieldType.IsAssignableTo(typeof(IVariantSavable)))
									.ToArray()
									.AsReadOnly();

	/// <summary>Saves <see cref="Settings"/> to disk.</summary>
	public void Save() {

		// Find savable data
		Godot.Collections.Dictionary<string, Variant> settingsDict = new();

		foreach (FieldInfo fieldInfo in UserSettingsSavableFields) {

			IVariantSavable? savable = fieldInfo.GetValue(Settings) as IVariantSavable;
			Variant saveValue = savable?.ToSavableVariant() ?? new Variant();

			if (saveValue.VariantType == Variant.Type.Nil) {
				GD.PushWarning($"Settings field \"{fieldInfo.Name}\" gave null when saving");
				continue;
			}

			settingsDict[fieldInfo.Name] = saveValue;
		}

		// Save to disk
		string settingsJson = Json.Stringify(settingsDict, indent: "\t");
		string settingsPath = Path.Combine(OS.GetUserDataDir(), SettingsFilepath);
		File.WriteAllText(settingsPath, settingsJson);
	}

	/// <summary>Loads settings from disk into <see cref="Settings"/>.</summary>
	/// <remarks>On load error no changes are made.</remarks>
	public void Load() {

		// Read from disk
		string settingsPath = Path.Combine(OS.GetUserDataDir(), SettingsFilepath);
		if (!File.Exists(settingsPath)) return;

		string settingsJson = File.ReadAllText(settingsPath);

		// Parse json
		Json jsonReader = new();
		var result = jsonReader.Parse(settingsJson);

		if (result != Error.Ok) {
			GD.PushError($"Could not load settings: file is malformed.\n{jsonReader.GetErrorMessage()}");
			return;
		}

		Variant parsedJson = jsonReader.Data;

		if (parsedJson.VariantType != Variant.Type.Dictionary) {
			GD.PushError("Could not load settings: root value is not an object.");
			return;
		}

		Godot.Collections.Dictionary<string, Variant> settingsDict = parsedJson.AsGodotDictionary<string, Variant>();

		// Apply data
		foreach (FieldInfo fieldInfo in UserSettingsSavableFields) {
			
			if (!settingsDict.TryGetValue(fieldInfo.Name, out Variant fieldSaveValue)) continue;
			
			if (fieldInfo.GetValue(Settings) is IVariantSavable savable) {
				savable.SetFromVariant(fieldSaveValue);
			}
			
		}
	}

	#endregion

}
