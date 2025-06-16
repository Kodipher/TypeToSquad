using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using WinRTSpeechSynthServer.Protocol;


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
		bool terminateRquestFlag = false;
		// todo

		// Setup request mapper
		var requestHandler = new RequestMapper()
			.Register<TerminateRequest>((_) => { terminateRquestFlag = true; return new TerminateAcceptedResponse(); })
			.Register<HeartbeatRequest>((req) => new HeartbeatEchoResponse() { EchoByte = req.EchoByte });

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
			while (!terminateRquestFlag) {

				Console.WriteLine("Waiting for request...");
				await pipeServer.WaitForConnectionAsync();

				Console.WriteLine("Connected. Processing...");
				await Task.Run(() => requestHandler.HandleSingleRequest(reader, writer));
				writer.Flush();

				Console.WriteLine("Reponse sent. Waiting for drain.");
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
				Console.Out.WriteLine("An exception has occured. See stderr.");
				Console.Error.WriteLine($"Exception occured:\n{ex}");
			} else throw;
		}

		Console.WriteLine("Terminating.");
	}

}
