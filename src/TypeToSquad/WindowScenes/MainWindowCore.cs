using Godot;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using WinRTSpeechSynthServer.Protocol;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.WindowScenes;


/// <remarks>
/// This contents of this window are to be unpacked,
/// thefore the main window has its programing in an immediate single child.
/// </remarks>
public partial class MainWindowCore : Node {

	Label labelHeart = null!;
	Label labelResponse = null!;
	TextEdit textEdit = null!;

	AudioStreamPlayer streamPlayer = null!;
	AudioStreamWav currentStream = null!;

	ResponseReader responseReader = new();

	public override void _Ready() {
		var terminateButton = GetNode<Button>("ButtonTerminate");
		var heatbeatButton = GetNode<Button>("ButtonHeartbeat");
		var speakButton = GetNode<Button>("ButtonSpeak");

		labelHeart = GetNode<Label>("LabelHeart");
		labelResponse = GetNode<Label>("LabelResponse");
		textEdit = GetNode<TextEdit>("TextEdit");

		streamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");

		terminateButton.Pressed += () => SendRequest(new TerminateRequest());
		heatbeatButton.Pressed += () => {
			byte value = (byte)Random.Shared.Next(0x100);
			labelHeart.Text = value.ToString("X");
			SendRequest(new HeartbeatRequest() { EchoByte = value });
		};
		speakButton.Pressed += () => {
			SendRequest(new SynthesizeTextRequest() { InputString = textEdit.Text });
		};

		responseReader.RegisterAll();
	}


	public override void _Process(double delta) {
	}

	public void SendRequest(Request req) {

		using NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(".", "TESTPIPE", PipeDirection.InOut, PipeOptions.Asynchronous);
		using BinaryReader reader = new BinaryReader(pipeClientStream);
		using BinaryWriter writer = new BinaryWriter(pipeClientStream);

		pipeClientStream.Connect(5000);

		writer.Write(req.MessageType);
		req.WriteContents(writer);
		writer.Flush();


		Response response = responseReader.ReadResponce(reader);

		if (response is HeartbeatEchoResponse echoResponce) {
			labelResponse.Text = "HeartbeatEcho:" + echoResponce.EchoByte.ToString("X");
		} else if (response is TerminateAcceptedResponse) {
			labelResponse.Text = "Terminated";
		} else if (response is SyntesisResultResponse speechResponce) {
			labelResponse.Text = "Speech,len=" + speechResponce.SynthesizedData.Length.ToString();

			const int wavImportCompressModePcm = 0;
			const int wavImportLoopModeDisabled = 1;

			currentStream = AudioStreamWav.LoadFromBuffer(
				speechResponce.SynthesizedData,
				new Godot.Collections.Dictionary {
					["compress/mode"] = wavImportCompressModePcm,
					["edit/loop_mode"] = wavImportLoopModeDisabled,
				}
			);

			streamPlayer.Stream = currentStream;
			streamPlayer.Play();

		} else {
			labelResponse.Text = response.Type.ToString();
		}
		
	}

}
