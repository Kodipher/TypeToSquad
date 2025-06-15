using System;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;


namespace WinRTSpeechSynthServer;


public class Program {

	async static Task Main(string[] args) {

		// Argument Guard
		if (args.Length < 1) {
			Console.WriteLine("This server serves via named pipe. Pass in the name of the pipe as the first argument.");
			return;
		}

		string pipeName = args[0];

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

			using StreamReader reader = new StreamReader(pipeServer);
			using StreamWriter writer = new StreamWriter(pipeServer);

			Console.WriteLine($"Serving pipe \"{pipeName}\" with buffer sizes in={pipeServer.InBufferSize} out={pipeServer.OutBufferSize} (0:=allocated as needed)");
			while (true) {

				Console.WriteLine("Waiting...");
				await pipeServer.WaitForConnectionAsync();

				Console.WriteLine("Connected. Processing...");
				var line = await reader.ReadLineAsync() ?? "";
				await writer.WriteLineAsync(string.Join("", line.Reverse())); //TEMP DEBUG
				await writer.FlushAsync();

				Console.WriteLine("Disconnecting...");
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
