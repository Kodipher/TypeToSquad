using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;


namespace WinRTSpeechSynthServer;


public class SpeechSynthesisWrapper : IDisposable {

	readonly SpeechSynthesizer synth = new();

	#region //// Voices

	/// <summary>
	/// Sets a voice by their <see cref="VoiceInformation.DisplayName"/>.
	/// If any voice matches then it is set and <see langword="true"/> is returned.
	/// Returns <see langword="false"/> if no voice matches.
	/// </summary>
	public bool SetVoice(string name) {

		VoiceInformation? voice = SpeechSynthesizer
									.AllVoices
									.Where(voice => voice.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
									.FirstOrDefault((VoiceInformation?)null);

		if (voice is null) return false;

		synth.Voice = voice;
		return true;
	}

	/// <summary>Sets the voice to default and returns the default voice.</summary>
	public Protocol.VoiceInfo SetVoiceToDefault() {
		var defaultVoice = SpeechSynthesizer.DefaultVoice;
		synth.Voice = defaultVoice;
		return ConvertVoiceInfo(defaultVoice);
	}

	/// <summary>Returns all installed voices.</summary>
	public static Protocol.VoiceInfo[] GetVoices() {
		return SpeechSynthesizer.AllVoices.Select(ConvertVoiceInfo).ToArray();
	}

	/// <summary>Returns the default voice.</summary>
	public static Protocol.VoiceInfo GetDefaultVoice() => ConvertVoiceInfo(SpeechSynthesizer.DefaultVoice);

	/// <summary>
	/// Converts a <see cref="VoiceInformation"/> from the windows runtime
	/// to a <see cref="Protocol.VoiceInfo"/> usable to transmit data
	/// </summary>
	public static Protocol.VoiceInfo ConvertVoiceInfo(VoiceInformation info) {

		var convertedGender = info.Gender switch {
			VoiceGender.Male => Protocol.VoiceGender.Male,
			VoiceGender.Female => Protocol.VoiceGender.Female,
			_ => Protocol.VoiceGender.Unknown,
		};

		return new Protocol.VoiceInfo() {
			Id = info.Id,
			Name = info.DisplayName,
			Language = info.Language,
			Gender = convertedGender
		};
	}

	#endregion

	#region //// Syntehsis

	public async Task<byte[]> SynthesizeTextAsync(string input) {
		using SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(input);
		return await GetSpeechStreamContents(stream);
	}

	public async Task<byte[]> SynthesizeSsmlAsync(string input) {
		using SpeechSynthesisStream stream = await synth.SynthesizeSsmlToStreamAsync(input);
		return await GetSpeechStreamContents(stream);
	}

	public byte[] SynthesizeText(string input) {
		return SynthesizeTextAsync(input).GetAwaiter().GetResult();
	}

	public byte[] SynthesizeSsml(string input) {
		return SynthesizeSsmlAsync(input).GetAwaiter().GetResult();
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
