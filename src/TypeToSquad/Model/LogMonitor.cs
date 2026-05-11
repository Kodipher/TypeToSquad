using Godot;
using System;
using System.IO;

using FileAccess = System.IO.FileAccess;


namespace TypeToSquad.Model;


public partial class LogMonitor : Node {

	#region /--- Singleton ---/

	public static LogMonitor Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion

	public override void _Ready() {
		StageSingletonInstance();
	}


	/*
	const string ProjectSettingNameLoggingEnabled = @"debug/file_logging/enable_file_logging";

	public void ToggleLogging(bool enabled) {
		ProjectSettings.SetSetting(ProjectSettingNameLoggingEnabled, enabled);
	}
	*/

	const string ProjectSettingNameLoggingPath = @"debug/file_logging/log_path";

	/// <summary>Returns absolute location of the log file on disk.</summary>
	public static string GetLogfilePath() {
		string userLocalPath = ProjectSettings.GetSetting(ProjectSettingNameLoggingPath).AsString();
		return ProjectSettings.GlobalizePath(userLocalPath);
	}

	
	long lastCheckEndPosition = 0;

	/// <summary>
	/// Reads the log for errors since the position of last read.
	/// If an error is found then <see cref="OnErrorFound"/> is invoked
	/// and further checks are blocked.
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
					EmitSignalOnErrorFound();
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
				EmitSignalOnErrorFound();
			}

			throw;
		}	
	}

	/// <summary>
	/// Forces the next read in <see cref="CheckLog"/>
	/// at the point where it is currently the end.
	/// This effectively skips all errors already present in the log.
	/// </summary>
	public void SeekToLogEnd() {

		try {
			string path = GetLogfilePath();
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var sr = new StreamReader(fs);
			
			fs.Seek(0, SeekOrigin.End);
			lastCheckEndPosition = fs.Position;

		} catch (Exception ex) {

			if (
				ex is IOException ||
				ex is ArgumentException ||
				ex is System.Security.SecurityException ||
				ex is UnauthorizedAccessException
			) {
				GD.PushError($"Error in the LogMonitor: {ex}");
			}
			
			throw;
		}
	}

	/// <summary>Emitted when <see cref="CheckLog"/> finds errors.</summary>
	[Signal] public delegate void OnErrorFoundEventHandler();

	/// <summary>
	/// Whether to block checks. 
	/// Automatically set when an error is found.
	/// Not readonly.
	/// </summary>
	public bool BlockChecks { get; set; } = false;

}
