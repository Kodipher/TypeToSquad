using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using WinRTSpeechSynthServer.Protocol;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace WinRTSpeechSynthServer;


public class Program {

	async static Task Main(string[] args) {

		// Argument Guard
		if (args.Length < 1) {
			Console.WriteLine("This server serves via named pipe. Pass in the name of the pipe as the first argument.");
			return;
		}

		// Setup data and syntehsizer 
		string pipeName = args[0];
		bool terminateRequestFlag = false;

		using SpeechSynthesisWrapper speechSynth = new();

		// Setup request mapper
		var requestHandler = new RequestMapper()
			.Register<SynthesizeRequest>(speechSynth.SynthesizeFromRequest)
			.Register<GetVoicesRequest>(_ => new AllVoicesResponse() { Voices = SpeechSynthesisWrapper.GetVoices(), DefaultVoice = SpeechSynthesisWrapper.GetDefaultVoice() })
			.Register<TerminateRequest>(_ => { terminateRequestFlag = true; return new TerminateAcceptedResponse(); })
			.Register<HeartbeatRequest>(req => new HeartbeatEchoResponse() { EchoByte = req.EchoByte });

		requestHandler.OnRequestReadStart += reqType => Console.WriteLine($"Processing request of type {reqType}.");

		// Pipe
		try {
			Console.WriteLine($"Creating pipe \"{pipeName}\"...");

			using NamedPipeServerStream pipeServer = new(
													pipeName,
													PipeDirection.InOut,
													maxNumberOfServerInstances: 1,
													PipeTransmissionMode.Byte,
													PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous
												);

			using BinaryReader reader = new BinaryReader(pipeServer);
			using BinaryWriter writer = new BinaryWriter(pipeServer);

			Console.WriteLine($"Serving pipe \"{pipeName}\" with buffer sizes in={pipeServer.InBufferSize} out={pipeServer.OutBufferSize} (0:=allocated as needed)");
			while (!terminateRequestFlag) {

				Console.WriteLine("Waiting for request...");
				await pipeServer.WaitForConnectionAsync();

				Console.WriteLine("Connected.");
				await Task.Run(() => requestHandler.HandleSingleRequest(reader, writer));
				writer.Flush();

				Console.WriteLine("Response sent. Waiting for drain.");
				pipeServer.WaitForPipeDrain();

				Console.WriteLine("Pipe drained. Disconnecting.");
				if (pipeServer.IsConnected) pipeServer.Disconnect();
			}

			Console.WriteLine("Closing...");
			pipeServer.Close();

		} catch (Exception ex) {
			if (
				ex is IOException || 
				ex is NotSupportedException || 
				ex is ArgumentException || 
				ex is ObjectDisposedException ||
				ex is InvalidOperationException
			) {
				Console.Out.WriteLine("An exception has occurred. See stderr.");
				Console.Error.WriteLine($"Exception occurred:{Console.Error.NewLine}{ex}");
			} else throw;
		}

		Console.WriteLine("Terminating.");
	}

}
