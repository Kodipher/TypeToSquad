using System;
using System.IO;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using NAudio.CoreAudioApi;
using NAudio.Wave;


namespace Kodipher.TypeToSquad.Modules.Speech;


/// <summary>
/// A class that creates or takes ownership of
/// references needed to synthesize and play speech
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class ActiveSpeech : IDisposable {

	#region //// Ownership

	public MMDevice OutputDevice { get; private set; }
	public WasapiOut OutputPlayer { get; private set; }
	public SpeechSynthesizer Synthesizer { get; private set; }

	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	public ActiveSpeech(MMDevice device) {
		OutputDevice = device;

		OutputPlayer = new(OutputDevice, AudioClientShareMode.Shared, true, 200);

		Synthesizer = new SpeechSynthesizer();
		SpeechApiReflectionHelper.InjectOneCoreVoices(Synthesizer);
	}

	#endregion

	#region //// Playing and stopping

	public void Speak() {

		// Link events
		OutputPlayer.PlaybackStopped += (object? _sender, StoppedEventArgs _args) => { IsReadyForDisposal = true; };

		// Link synthesier to output player
		// todo

		// Perfom speech
		// todo

	}

	public void Shut() {
		if (!readyForDisposal) OutputPlayer.Stop();
	}

	#endregion

	#region //// Disposing

	private bool readyForDisposal;
	public bool IsReadyForDisposal {
		get => readyForDisposal;
		protected set {
			readyForDisposal = value;
			if (value) OnReadyForDisposal?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? OnReadyForDisposal;


	private bool hasDisposed;

	protected virtual void Dispose(bool disposeManaged) {

		if (hasDisposed) return;

		if (disposeManaged) {
			OutputDevice.Dispose();
			OutputPlayer.Dispose();
			Synthesizer.Dispose();
		}

		OutputDevice = null!;
		OutputPlayer = null!;
		Synthesizer = null!;

		hasDisposed = true;
	}

	~ActiveSpeech() {
		Dispose(disposeManaged: false);
	}

	public void Dispose() {
		Dispose(disposeManaged: true);
		GC.SuppressFinalize(this);
	}

	#endregion

}

