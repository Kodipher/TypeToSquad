using Godot;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

using Rephidock.GeneralUtilities.Randomness;

using WinRTSpeechSynthServer.Protocol;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Model;


//[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class SpeechDaemon : IDisposable {

	#region //// Daemon Process

	Process? daemonProcess = null;
	string currentPipeName = "";

	const string relativeExecutablePath = @"WinRTSpeechDaemon\WinRTSpeechSynthServer.exe";
	const string pipeNameFormat = @"TTSSpeechDaemon_{0:x8}";

	readonly static TimeSpan daemonKillTimeout = TimeSpan.FromSeconds(1);

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
		CloseAndDisposeDaemon();

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
			CloseAndDisposeDaemon();
			return;
		}

		GD.Print("Daemon started.");

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
		/*
			.ContinueWith((task) => {
				if (task.Exception is not null) {
					GD.PushError($"Exception(s) occurred during daemon error reading.\n{task.Exception}");
				}
			});
		*/
	}

	/// <summary>
	/// Checks if <see cref="daemonProcess"/> is alive.
	/// The check is simple and does not involve a <see cref="HeartbeatRequest"/>.
	/// </summary>
	public bool IsDaemonAliveNoHeartbeat() {

		ObjectDisposedException.ThrowIf(isDisposed, this);

		if (daemonProcess is null) return false;
		if (daemonProcess.HasExited) return false;
		return true;
	}

	/// <summary>
	/// Safely closes and disposes of the daemon process.
	/// Does nothing if <see cref="daemonProcess"/> is already <see langword="null"/>.
	/// </summary>
	void CloseAndDisposeDaemon() {

		if (daemonProcess is null) return;

		// Try asking nicely
		if (!daemonProcess.HasExited) {
			if (daemonProcess.CloseMainWindow()) {
				daemonProcess.WaitForExit(daemonKillTimeout);
			}
		}

		// Force exit
		if (!daemonProcess.HasExited) {
			daemonProcess.Kill();
			daemonProcess.WaitForExit(daemonKillTimeout);
		}

		if (!daemonProcess.HasExited) GD.PushError("Could not close daemon process.");

		// Dispose
		daemonProcess.Dispose();
		daemonProcess = null;
		currentPipeName = "";
	}

	#endregion

	#region //// Communication

	/*
		Request are sent and processed asynchronously,
		but the callbacks need to run on the main thread,
		so callbacks are bound with the reponses and queued.
	*/

	readonly static TimeSpan requestTimeout = TimeSpan.FromSeconds(5);

	readonly ResponseReader responseReader = ResponseReader.CreateWithStanardRegistered();
	readonly ConcurrentQueue<Action> responseConsumptionCallbackQueue = new();

	/// <summary>
	/// Sends a request and queues the callback with the response
	/// into the <see cref="responseConsumptionCallbackQueue"/>.
	/// </summary>
	public void DispatchRequest(Request request, Action<Response> callback) {
		ObjectDisposedException.ThrowIf(isDisposed, this);

		Task.Run(() => {
			// Send request
			Response? response = null;
			response = SendRequest(request);
			
			// Enqueue response (as consumption callback)
			if (response is null) return;
			responseConsumptionCallbackQueue.Enqueue(() => callback(response));

		}).ContinueWith((task) => {
			if (task.Exception is not null) {
				GD.PushError($"Exception(s) occurred during a request dispatch.\n{task.Exception}");
			}
		});
		
	}

	/// <summary>
	/// Sends a single <see cref="Request"/> <paramref name="req"/>
	/// and returns a response.
	/// </summary>
	Response SendRequest(Request req) {

		// Guards
		ObjectDisposedException.ThrowIf(isDisposed, this);
		
		if (!IsDaemonAliveNoHeartbeat()) {
			throw new InvalidOperationException("Daemon is not alive. Aborting request.");
		}

		// Perform request
		using NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(".", currentPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
		using BinaryReader reader = new BinaryReader(pipeClientStream);
		using BinaryWriter writer = new BinaryWriter(pipeClientStream);

		try {
			GD.Print("Connecting...");
			pipeClientStream.Connect(requestTimeout);
		} catch (Exception ex) {
			if (ex is TimeoutException || ex is IOException) {
				throw new IOException("Could not connect to the daemon", ex);
			}
			else throw;
		}

		GD.Print($"Connected. Sending request of type {req.Type}.");

		writer.Write(req.MessageType);
		req.WriteContents(writer);
		writer.Flush();

		GD.Print($"Waiting for response.");
		Response responce = responseReader.ReadResponse(reader);
		GD.Print($"Got response of type {responce.Type}.");
		return responce;
	}

	/// <summary>
	/// Executes all queued callbacks in
	/// <see cref="responseConsumptionCallbackQueue"/>.
	/// (Consumes the whole queue).
	/// </summary>
	public void ConsumeResponses() {
		// Guards
		ObjectDisposedException.ThrowIf(isDisposed, this);
		// Consume
		while (!responseConsumptionCallbackQueue.IsEmpty) {
			if (responseConsumptionCallbackQueue.TryDequeue(out Action? callback)) {
				callback();
			}
		}
	}

	#endregion

	#region //// Disposable

	private bool isDisposed;

	protected virtual void Dispose(bool disposingManaged) {

		if (isDisposed) return;

		if (disposingManaged) {
			// Dispose managed
			CloseAndDisposeDaemon();
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
