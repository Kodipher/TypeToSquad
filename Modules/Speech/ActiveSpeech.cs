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
	public MemoryStream AudioStream { get; private set; }
	public RawSourceWaveStream? OutputAudioProvider { get; private set; }

	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	public ActiveSpeech(MMDevice device) {
		OutputDevice = device;

		OutputPlayer = new(OutputDevice, AudioClientShareMode.Shared, true, 200);

		Synthesizer = new SpeechSynthesizer();
		SpeechApiReflectionHelper.InjectOneCoreVoices(Synthesizer);

		AudioStream = new MemoryStream();
	}

	#endregion

	#region //// Playing and stopping

	public void Speak(string text) {

		// Link events
		OutputPlayer.PlaybackStopped += (object? _sender, StoppedEventArgs _args) => { IsReadyForDisposal = true; };

		// Setup format
		int _samplesPerSecond = 44100;
		int _channelCount = 1;
		int _bitsPerSample = 16; // 8 or 16, errors on 24, 32 or anything else

		int _blockAlign = _channelCount * (_bitsPerSample / 8);			// Copied from the constructor
		int _averageBytesPerSecond = _samplesPerSecond * _blockAlign;

		var speechFormat = new SpeechAudioFormatInfo(EncodingFormat.Pcm, _samplesPerSecond, _bitsPerSample, _channelCount, _averageBytesPerSecond, _blockAlign, null);
		var audioProviderFormat = new WaveFormat(_samplesPerSecond, _bitsPerSample, _channelCount);

		// Syntehsize speech into a stream
		Synthesizer.SetOutputToAudioStream(AudioStream, speechFormat);
		Synthesizer.Speak(text);
		AudioStream.Flush();
		AudioStream.Seek(0, SeekOrigin.Begin);

		// Play the stream
		OutputAudioProvider = new RawSourceWaveStream(AudioStream, audioProviderFormat);
		OutputPlayer.Init(OutputAudioProvider!);
		OutputPlayer.Play();

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
			AudioStream.Dispose();
			OutputAudioProvider?.Dispose();
		}

		OutputDevice = null!;
		OutputPlayer = null!;
		Synthesizer = null!;
		AudioStream = null!;
		OutputAudioProvider = null;

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

