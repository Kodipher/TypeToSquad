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
