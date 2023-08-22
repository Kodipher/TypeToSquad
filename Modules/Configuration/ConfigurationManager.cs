using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kodipher.TypeToSquad.Utils;

using Path = System.IO.Path;


namespace Kodipher.TypeToSquad.Modules.Configuration;


public partial class ConfigurationManager : Node {

	/// <summary>
	/// Current configuration used by pretty much everything.
	/// Any changed to the object are to be reflected live.
	/// Set only one time on init due to sharing by reference.
	/// Saving the object to disk is separate. See <see cref="SaveCurrentConfig"/> and <see cref="LoadConfigToCurrent"/>
	/// </summary>
	public Configuration CurrentConfig { get; init; }

	protected readonly IReadOnlyList<FieldInfo> CurrentConfigFields;

	public ConfigurationManager() {
		CurrentConfig = new Configuration();
		CurrentConfigFields =
			CurrentConfig
			.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public)
			.Where(fi => fi.FieldType.IsSubcalssOrSelfOf(typeof(Field<>)))
			.ToArray()
			.AsReadOnly();
	}

	#region //// Saving / Loading

	const string fieldGetVariantMethodName = "GetVariant";
	const string fieldSetVariantMethodName = "SetVariant";

	public const string configFilepath = "config.json";

	/// <summary>Saves current configuration to disk</summary>
	public void SaveCurrentConfig() {

		// Find saveable data
		Godot.Collections.Dictionary<string, Variant> configDict = new();

		foreach (FieldInfo fieldInfo in CurrentConfigFields) {

			Variant? saveValue =
				fieldInfo
				.FieldType
				.GetMethod(fieldGetVariantMethodName)
				?.Invoke(fieldInfo.GetValue(CurrentConfig), Array.Empty<object>())
				as Variant?;

			if (!saveValue.HasValue) {
				GD.PushWarning($"Configuration field \"{fieldInfo.Name}\" gave null when saving");
				continue;
			}

			configDict[fieldInfo.Name] = saveValue.Value;
		}

		// Save to disk
		string configJson = Json.Stringify(configDict, indent: "\t");
		string configPath = Path.Combine(OS.GetUserDataDir(), configFilepath);
		File.WriteAllText(configPath, configJson);

	}

	/// <summary>Loads configuration to disk into current configuration</summary>
	/// <returns>True if the configuration was loaded</returns>
	public bool LoadConfigToCurrent() {

		// Load form disk
		string configPath = Path.Combine(OS.GetUserDataDir(), configFilepath);
		string configJson = File.ReadAllText(configPath);

		Variant parsedJson = Json.ParseString(configJson);

		if (parsedJson.VariantType == Variant.Type.Nil) {
			// Error
			GD.PushError($"Cannot load configuration: file is malformed");
			return false;
		} else if (parsedJson.VariantType != Variant.Type.Dictionary) {
			// Error
			GD.PushError("Cannot load configuration: root value is not an object");
			return false;
		}

		Godot.Collections.Dictionary<string, Variant> configDict = parsedJson.AsGodotDictionary<string, Variant>();

		// Apply data into current config
		foreach (FieldInfo fieldInfo in CurrentConfigFields) {

			if (!configDict.TryGetValue(fieldInfo.Name, out Variant saveValue)) {
				GD.PushWarning($"Configuration field \"{fieldInfo.Name}\" is missing on disk. Skipping.");
				continue;
			}

			fieldInfo
			.FieldType
			.GetMethod(fieldSetVariantMethodName)
			?.Invoke(fieldInfo.GetValue(CurrentConfig), new object[] { saveValue });
		}

		return true;

	}


	#endregion

}
