using Godot;
using System;
using System.IO;

using FileAccess = System.IO.FileAccess;


namespace TypeToSquad.Model;


public class LogMonitor {

	/*
	const string settingNameLoggingEnabled = @"debug/file_logging/enable_file_logging";

	public void ToggleLogging(bool enabled) {
		ProjectSettings.SetSetting(settingNameLoggingEnabled, enabled);
	}
	*/

	const string settingNameLoggingPath = @"debug/file_logging/log_path";

	/// <summary>Returns absolute location of the log file on disk.</summary>
	public static string GetLogfilePath() {
		string userLocalPath = ProjectSettings.GetSetting(settingNameLoggingPath).AsString();
		return ProjectSettings.GlobalizePath(userLocalPath);
	}

	
	long lastCheckEndPosition = 0;

	/// <summary>
	/// Reads the log for errors since the position of last read.
	/// If an error is found then <see cref="OnErrorFound"/> is invoked
	/// and futher checks are blocked.
	/// </summary>
	public void CheckLog() {
	
		if (BlockChecks) return;

		try {
			string path = GetLogfilePath();
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var sr = new StreamReader(fs);

			fs.Seek(lastCheckEndPosition, SeekOrigin.Begin);

			while (!sr.EndOfStream) {

				string line = sr.ReadLine() ?? "";
				lastCheckEndPosition = fs.Position;

				if (line.StartsWith("ERROR")) {
					BlockChecks = true;
					OnErrorFound();
					return;
				}
			}

		} catch (Exception ex) {

			if (
				ex is IOException ||
				ex is ArgumentException ||
				ex is System.Security.SecurityException ||
				ex is UnauthorizedAccessException
			) {
				GD.PushError($"Error in the LogMonitor: {ex}");
				BlockChecks = true;
				OnErrorFound();
			}
		}

		
	}

	/// <summary>Invoked when <see cref="CheckLog"/> finds errors.</summary>
	public event Action OnErrorFound = () => {};

	/// <summary>
	/// Wether to block checks. 
	/// Automatically set when an error is found.
	/// Not readonly.
	/// </summary>
	public bool BlockChecks { get; set; } = false;

}
