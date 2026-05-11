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

	/// <summary>Whether monitoring is done at all, regardless of if an error was found or not.</summary>
	public bool IsMonitoring => UserSettingsManager.Instance.Settings.EnableErrorMonitoring;

	/// <summary>
	/// Set when an error is found.
	/// Blocks further checks while set.
	/// Not readonly - clear this flag to continue monitoring.
	/// </summary>
	public bool IsErrorFound { get; private set; } = false;

	public void ContinueMonitoringPastError() {
		SeekToLogEnd();
		IsErrorFound = false;
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
	/// Reads the log for errors since the position of last read now.
	/// If the log checking setting is disabled, does nothing.
	/// If an error is found then <see cref="OnErrorFound"/> is invoked and further checks are blocked.
	/// </summary>
	public void CheckLog() {
	
		if (!IsMonitoring) return;
		if (IsErrorFound) return;

		try {
			string path = GetLogfilePath();
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var sr = new StreamReader(fs);

			fs.Seek(lastCheckEndPosition, SeekOrigin.Begin);

			while (!sr.EndOfStream) {

				string line = sr.ReadLine() ?? "";
				lastCheckEndPosition = fs.Position;

				if (line.StartsWith("ERROR")) {
					IsErrorFound = true;
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
				IsErrorFound = true;
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
				IsErrorFound = true;
				EmitSignalOnErrorFound();
			}
			
			throw;
		}
	}

	/// <summary>Emitted when <see cref="CheckLog"/> finds errors.</summary>
	[Signal] public delegate void OnErrorFoundEventHandler();

}
