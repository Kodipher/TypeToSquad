using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;


namespace WinRTSpeechSynthServer;


public class SpeechSynthesisWrapper : IDisposable {

	SpeechSynthesizer synth = new();

	public async Task<byte[]> SynthesizeTextAsync(string input) {

		// Synthesize
		using SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(input);

		// Read as buffer
		if (stream.Size >= int.MaxValue) {
			throw new ArgumentException($"Input string produced an output of length {stream.Size}, is or longer than int.MaxValue", nameof(input));
		}

		using var dataReader = new DataReader(stream);
		uint streamSize = (uint)stream.Size;
		await dataReader.LoadAsync(streamSize);

		// Return as array
		return dataReader.ReadBuffer(streamSize).ToArray();
	}

	public byte[] SynthesizeText(string input) { 
		return SynthesizeTextAsync(input).GetAwaiter().GetResult();
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
