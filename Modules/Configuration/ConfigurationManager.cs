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
	/// Any changed to the object are to be reflected on the spot.
	/// Set only one time on init due to sharing by reference.
	/// Saving the obejct to disk is separate. See <see cref="SaveCurrentConfig"/>
	/// </summary>
	public Configuration CurrentConfig { get; init; } = new Configuration();

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

	protected readonly IReadOnlyList<FieldInfo> CurrentConfigFields;

	const string fieldGetVariantMethodName = "GetVariant";
	const string fieldSetVariantMethodName = "SetVariant";

	const string configFilepath = "config.json";

	public void SaveCurrentConfig() {

		// Find saveable data
		Dictionary<string, Variant> configStrings = new();

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

			configStrings[fieldInfo.Name] = saveValue.Value;
		}

		// Save to disk
		string configJson = Json.Stringify(configStrings.ToGodotDictionary(), indent:"\t");
		string configPath = Path.Combine(OS.GetUserDataDir(), configFilepath);
		File.WriteAllText(configPath, configJson);

	}

	#endregion

}
