using Godot;


namespace TypeToSquad.Model;


/// <summary>Checks the logging pipeline and emits a signal when an error or warning is logged.</summary>
public partial class LogMonitor : Node {

	#region /--- Singleton ---/

	public static LogMonitor Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion
	
	public partial class SignalingLogger : Logger {

		public override void _LogError(
			string function, 
			string file, 
			int line, 
			string code, 
			string rationale, 
			bool editorNotify, 
			int errorType,
			Godot.Collections.Array<ScriptBacktrace> scriptBacktraces
		) {
			switch ((ErrorType)errorType) {
				case ErrorType.Script:
				case ErrorType.Error: 
				case ErrorType.Shader:
					EmitSignalOnErrorLogged();
					return;

				case ErrorType.Warning:
					EmitSignalOnWarningLogged();
					break;
				
				default:
					EmitSignalOnErrorLogged();
					throw new System.ArgumentOutOfRangeException(nameof(errorType), errorType, null);
			}
		}

		[Signal] public delegate void OnErrorLoggedEventHandler();
		
		[Signal] public delegate void OnWarningLoggedEventHandler();
		
	}

	SignalingLogger Logger { get; set; } = null!; // Set in _Ready;
	
	[Signal] public delegate void LoggerNotificationEventHandler();
	
	public override void _Ready() {
		StageSingletonInstance();

		Logger = new SignalingLogger();

		Logger.OnErrorLogged += () => {
			if (!UserSettingsManager.Instance.Settings.EnableErrorNotifications) return;
			EmitSignalLoggerNotification();
		};
		
		Logger.OnWarningLogged += () => {
			if (!UserSettingsManager.Instance.Settings.EnableWarningNotifications) return;
			EmitSignalLoggerNotification();
		};
		
		OS.AddLogger(Logger);
	}

	
	const string ProjectSettingNameLoggingPath = @"debug/file_logging/log_path";

	/// <summary>Returns absolute location of the log file on disk.</summary>
	public static string GetLogfilePath() {
		string userLocalPath = ProjectSettings.GetSetting(ProjectSettingNameLoggingPath).AsString();
		return ProjectSettings.GlobalizePath(userLocalPath);
	}
	
}
