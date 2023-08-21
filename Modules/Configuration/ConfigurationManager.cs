using Godot;
using System;
using System.IO;
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

	#region //// Saving / Loading

	const string configFilepath = "userdata/config.json";

	public void SaveCurrentConfig() {
		string configPath = Path.Combine(OS.GetExecutablePath().GetBaseDir(), configFilepath);
		GD.Print($"[TODO] Saving Configuration to \"{configPath}\"");
	}

	#endregion

}
