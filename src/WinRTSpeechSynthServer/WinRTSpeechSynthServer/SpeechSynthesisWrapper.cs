using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

using SynthesizeRequest = WinRTSpeechSynthServer.Protocol.Messages.SynthesizeRequest;
using SyntesisResultResponse = WinRTSpeechSynthServer.Protocol.Messages.SyntesisResultResponse;
using ProtocolVoiceInfo = WinRTSpeechSynthServer.Protocol.VoiceInfo;
using ProtocolVoiceGender = WinRTSpeechSynthServer.Protocol.VoiceGender;


namespace WinRTSpeechSynthServer;


public class SpeechSynthesisWrapper : IDisposable {

	readonly SpeechSynthesizer synth = new();

	#region //// Voices

	/// <summary>
	/// Sets a voice by <see cref="VoiceInformation.DisplayName"/>.
	/// If any voice matches then it is set and <see langword="true"/> is returned.
	/// Returns <see langword="false"/> if no voice matches.
	/// </summary>
	public bool TrySetVoice(string name) {

		VoiceInformation? voice = SpeechSynthesizer
									.AllVoices
									.Where(voice => voice.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
									.FirstOrDefault((VoiceInformation?)null);

		if (voice is null) return false;

		synth.Voice = voice;
		return true;
	}

	/// <summary>Returns all installed voices.</summary>
	public static ProtocolVoiceInfo[] GetVoices() {
		return SpeechSynthesizer.AllVoices.Select(ConvertVoiceInfo).ToArray();
	}

	/// <summary>Returns the default voice.</summary>
	public static ProtocolVoiceInfo GetDefaultVoice() => ConvertVoiceInfo(SpeechSynthesizer.DefaultVoice);

	/// <summary>
	/// Converts a <see cref="VoiceInformation"/> from the windows runtime
	/// to a <see cref="Protocol.VoiceInfo"/> usable to transmit data
	/// </summary>
	public static ProtocolVoiceInfo ConvertVoiceInfo(VoiceInformation info) {

		var convertedGender = info.Gender switch {
			VoiceGender.Male => ProtocolVoiceGender.Male,
			VoiceGender.Female => ProtocolVoiceGender.Female,
			_ => ProtocolVoiceGender.Unknown,
		};

		return new ProtocolVoiceInfo() {
			Id = info.Id,
			Name = info.DisplayName,
			Language = info.Language,
			Gender = convertedGender
		};
	}

	#endregion

	#region //// Synthesis

	public async Task<SyntesisResultResponse> SynthesizeFromRequestAsync(SynthesizeRequest request) {


		// Try set voice
		bool wasVoiceSet = TrySetVoice(request.VoiceName);
		if (!wasVoiceSet) synth.Voice = SpeechSynthesizer.DefaultVoice;

		// Set options
		synth.Options.AudioPitch = request.Pitch;
		synth.Options.SpeakingRate = request.Rate;
		synth.Options.AudioVolume = request.Volume;

		// Speak
		byte[] speechStreamContents;
		if (request.IsSsml) {
			using SpeechSynthesisStream stream = await synth.SynthesizeSsmlToStreamAsync(request.InputString);
			speechStreamContents = await GetSpeechStreamContents(stream);
		} else {
			using SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(request.InputString);
			speechStreamContents = await GetSpeechStreamContents(stream);
		}

		// Retrun
		return new SyntesisResultResponse() {
			SynthesizedData = speechStreamContents,
			GivenVoiceExists = wasVoiceSet
		};

	}

	public SyntesisResultResponse SynthesizeFromRequest(SynthesizeRequest request) {
		return Task.Run(() => SynthesizeFromRequest(request)).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Borrows a <see cref="SpeechSynthesisStream"/>
	/// to read it and return its data as a byte array.
	/// </summary>
	private static async Task<byte[]> GetSpeechStreamContents(SpeechSynthesisStream stream) {
	
		if (stream.Size >= int.MaxValue) {
			throw new ArgumentException($"Input string produced an output of length {stream.Size}, which is or longer than int.MaxValue");
		}

		using var dataReader = new DataReader(stream);
		uint streamSize = (uint)stream.Size;
		await dataReader.LoadAsync(streamSize);

		return dataReader.ReadBuffer(streamSize).ToArray();
	}

	#endregion

	#region //// Disposable

	private bool isDisposed;

	protected virtual void Dispose(bool disposeManaged) {

		if (isDisposed) return;

		if (disposeManaged) {
			synth.Dispose();
		}

		isDisposed = true;
	}

	// ~SpeechSynthesisWrapper()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose() {
		Dispose(disposeManaged: true);
		GC.SuppressFinalize(this);
	}

	#endregion

}
