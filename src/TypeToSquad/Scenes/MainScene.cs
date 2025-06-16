using Godot;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using WinRTSpeechSynthServer.Protocol;


namespace TypeToSquad.Scenes;


public partial class MainScene : HBoxContainer {

	Label labelHeart;
	Label labelResponse;

	public override void _Ready() {
		var terminateButton = GetNode<Button>("ButtonTerminate");
		var heatbeatButton = GetNode<Button>("ButtonHeartbeat");

		labelHeart = GetNode<Label>("LabelHeart");
		labelResponse = GetNode<Label>("LabelResponse");

		terminateButton.Pressed += () => SendRequest(new TerminateRequest());
		heatbeatButton.Pressed += () => {
			byte value = (byte)Random.Shared.Next(0x100);
			labelHeart.Text = value.ToString("X");
			SendRequest(new HeartbeatRequest() { EchoByte = value });
		};

		
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


		byte responseType = reader.ReadByte();
		labelResponse.Text = responseType.ToString("X");
		if (responseType == (byte)ResponseType.HeartbeatEcho) {
			labelResponse.Text += " " + reader.ReadByte().ToString("X");
		}
		
	}

}
