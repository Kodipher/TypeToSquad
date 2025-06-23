using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Rephidock.GeneralUtilities.Randomness;



namespace TypeToSquad.Model;


public class SpeechDaemon : IDisposable {

	#region //// Daemon Process

	Process? daemonProcess = null;
	string currentPipeName = "";

	const string relativeExecutablePath = @"WinRTSpeechDaemon\WinRTSpeechSynthServer.exe";
	const string pipeNameFormat = @"TTSSpeechDaemon_{0:x8}";

	static string GetDaemonExecutablePath() {

		string projectRootPath;
		if (OS.HasFeature("editor")) {
			projectRootPath = ProjectSettings.GlobalizePath(@"res://");
		} else {
			projectRootPath = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		}

		return Path.Combine(projectRootPath, relativeExecutablePath);
	}

	static string CreateUniquePipeName() {
		int disambiguator = Random.Shared.NextUInt31();
		return string.Format(pipeNameFormat, disambiguator);
	}

	public void StartDaemon() {

		ObjectDisposedException.ThrowIf(isDisposed, this);

		string pipeName = CreateUniquePipeName();
		GD.Print($"Starting/restarting the daemon with pipe {pipeName}.");

		// Kill existing
		daemonProcess?.Kill();
		daemonProcess?.Dispose();
		daemonProcess = null;
		currentPipeName = "";

		// Try start new
		var daemonStartInfo = new ProcessStartInfo(GetDaemonExecutablePath(), [pipeName]) {
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		daemonProcess = Process.Start(daemonStartInfo);
		currentPipeName = pipeName;

		if (daemonProcess is null) {
			GD.PushError("Daemon could not be started.");
			currentPipeName = "";
			return;
		} else if (daemonProcess.HasExited) {
			GD.PushError("Daemon processes unexpectedly instantly exited.");
			daemonProcess.Dispose();
			daemonProcess = null;
			currentPipeName = "";
			return;
		}

		// Hook output and error
		static void HandleProcessStandardOutput(object _, DataReceivedEventArgs eventArgs) {
			if (eventArgs.Data is not null) GD.Print($"[DAEMON] {eventArgs.Data}");
		}

		static async Task ReadProcessStandardErrorBatched(Process daemon) {

			StringBuilder errStringBuilder = new();

			while (!daemon.HasExited) {

				// Wait for first line
				string? firstLine = await daemon.StandardError.ReadLineAsync();
				if (firstLine == null) return; // Stream ended. That means processes terminated.

				errStringBuilder.AppendLine(firstLine);

				// Append every other line
				while (!daemon.StandardError.EndOfStream) {
					errStringBuilder.AppendLine(await daemon.StandardError.ReadLineAsync());
				}

				// Print and clear
				GD.PushError($"[DAEMON ERROR] {errStringBuilder}");
				errStringBuilder.Clear();
			}

		}

		daemonProcess.OutputDataReceived += HandleProcessStandardOutput;
		daemonProcess.BeginOutputReadLine();

		_ = ReadProcessStandardErrorBatched(daemonProcess);
	}

	#endregion

	#region //// Disposable

	private bool isDisposed;

	protected virtual void Dispose(bool disposingManaged) {

		if (isDisposed) return;

		if (disposingManaged) {
			// Dispose managed
			daemonProcess?.Kill();
			daemonProcess?.Dispose();
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
