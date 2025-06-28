using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Rephidock.GeneralUtilities.Reflection;


namespace TypeToSquad.Model.Settings;


public static class UserSettingsLoader {

	public const string settingsFilepath = "user_settings.json";

	readonly static IReadOnlyList<FieldInfo> UserSettingsFields = 
									typeof(UserSettings)
									.GetFields(BindingFlags.Instance | BindingFlags.Public)
									.Where(fi => fi.FieldType.IsSubclassOrSelfOf(typeof(Field<>)))
									.ToArray()
									.AsReadOnly();

	/// <summary>Saves settings to disk</summary>
	public static void Save(UserSettings settings) {

		// Find saveable data
		Godot.Collections.Dictionary<string, Variant> settingsDict = new();

		foreach (FieldInfo fieldInfo in UserSettingsFields) {

			IVariantSavable? savable = fieldInfo.GetValue(settings) as IVariantSavable;
			Variant saveValue = savable?.ValueAsSavable ?? new Variant();

			if (saveValue.VariantType == Variant.Type.Nil) {
				GD.PushWarning($"Settings field \"{fieldInfo.Name}\" gave null when saving");
				continue;
			}

			settingsDict[fieldInfo.Name] = saveValue;
		}

		// Save to disk
		string settingsJson = Json.Stringify(settingsDict, indent: "\t");
		string settingsPath = Path.Combine(OS.GetUserDataDir(), settingsFilepath);
		File.WriteAllText(settingsPath, settingsJson);

	}

	/// <summary>Loads settings from disk</summary>
	public static UserSettings Load() {

		// Read from disk
		string settingsPath = Path.Combine(OS.GetUserDataDir(), settingsFilepath);
		if (!File.Exists(settingsPath)) return new UserSettings();

		string settingsJson = File.ReadAllText(settingsPath);

		// Parse json
		Json jsonReader = new();
		var result = jsonReader.Parse(settingsJson);

		if (result != Error.Ok) {
			GD.PushError($"Cannot load settings: file is malformed.\n{jsonReader.GetErrorMessage()}");
			return new UserSettings();
		}

		Variant parsedJson = jsonReader.Data;

		if (parsedJson.VariantType != Variant.Type.Dictionary) {
			GD.PushError("Cannot load settings: root value is not an object.");
			return new UserSettings();
		}

		Godot.Collections.Dictionary<string, Variant> settingsDict = parsedJson.AsGodotDictionary<string, Variant>();

		// Apply data
		var settings = new UserSettings();
		foreach (FieldInfo fieldInfo in UserSettingsFields) {
			if (settingsDict.TryGetValue(fieldInfo.Name, out Variant fieldSaveValue)) {
				IVariantSavable? savable = fieldInfo.GetValue(settings) as IVariantSavable;
				if (savable is not null) savable.ValueAsSavable = fieldSaveValue;
			}
		}

		return settings;
	}


	static UserSettingsLoader() {

		// (debug) check if there are properties
		if (
			typeof(UserSettings)
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(pi => pi.PropertyType.IsSubclassOrSelfOf(typeof(Field<>)))
			.Any()
		) {
			GD.PushWarning($"{nameof(UserSettings)} has a Field storing property. Properties are not supported by {nameof(UserSettingsLoader)}");
		}
	}

}
