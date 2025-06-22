using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace TypeToSquad.Model;


public class SpeechDaemon : IDisposable {

	#region //// Daemon Process

	const string relativeExecutablePath = @"WinRTSpeechDaemon\WinRTSpeechSynthServer.exe";

	static string GetDaemonExecutablePath() {

		string projectRootPath;
		if (OS.HasFeature("editor")) {
			projectRootPath = ProjectSettings.GlobalizePath(@"res://");
		} else {
			projectRootPath = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		}

		return Path.Combine(projectRootPath, relativeExecutablePath);
	}

	#endregion

	#region //// Disposable

	private bool isDisposed;

	protected virtual void Dispose(bool disposingManaged) {

		if (isDisposed) return;

		if (disposingManaged) {
			// Dispose managed
		}

		// Dispose unmanaged
		

		// Set flag
		isDisposed = true;
	}

	~SpeechDaemon() {
		// Do not add disposing here
		Dispose(disposingManaged: false);
	}

	public void Dispose() {
		// Do not add disposing here
		Dispose(disposingManaged: true);
		GC.SuppressFinalize(this);
	}

	#endregion

}
