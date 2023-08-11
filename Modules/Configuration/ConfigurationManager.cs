using Godot;
using System;
using System.IO;
using Path = System.IO.Path;


namespace Kodipher.TypeToSqaud.Modules.Configuration;


public partial class ConfigurationManager : Node {

	#region //// Config

	public class Configuration {
		public string Device { get; set; }
		public string Voice { get; set; }
	}

	/// <summary>
	/// Current configuration used by pretty much everything.
	/// Any changed to the object are to be reflected on the spot.
	/// Saving the obejct to disk is separate. See <see cref="SaveCurrentConfig"/>
	/// </summary>
	public Configuration CurrentConfig { get; private set; } = new Configuration();

	#endregion

	#region //// Saving / Loading

	const string configFilepath = "userdata/config.json";

	public void SaveCurrentConfig() {
		string configPath = Path.Combine(OS.GetExecutablePath().GetBaseDir(), configFilepath);
		GD.Print($"[TODO] Saving Configuration to \"{configPath}\"");
	}

	#endregion

}
