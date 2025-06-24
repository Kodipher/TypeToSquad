using Godot;
using System;

using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


/// <remarks>
/// This contents of this window are to be unpacked,
/// thefore the main window has its programing in an immediate single child.
/// </remarks>
public partial class MainWindowCore : Node, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode core) => CoreNode = core;

	#endregion


	Label labelHeart = null!;
	Label labelResponse = null!;
	TextEdit textEdit = null!;

	AudioStreamPlayer streamPlayer = null!;
	AudioStreamWav currentStream = null!;

	public override void _Ready() {
		var terminateButton = GetNode<Button>("ButtonTerminate");
		var heatbeatButton = GetNode<Button>("ButtonHeartbeat");
		var speakButton = GetNode<Button>("ButtonSpeak");

		labelHeart = GetNode<Label>("LabelHeart");
		labelResponse = GetNode<Label>("LabelResponse");
		textEdit = GetNode<TextEdit>("TextEdit");

		streamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");

		terminateButton.Pressed += () => CoreNode?.SpeechDaemon.DispatchRequest(new TerminateRequest(), HandleResponse);
		heatbeatButton.Pressed += () => {
			byte value = (byte)Random.Shared.Next(0x100);
			labelHeart.Text = value.ToString("X");
			CoreNode?.SpeechDaemon.DispatchRequest(new HeartbeatRequest() { EchoByte = value }, HandleResponse);
		};
		speakButton.Pressed += () => {
			CoreNode?.SpeechDaemon.DispatchRequest(new SynthesizeTextRequest() { InputString = textEdit.Text }, HandleResponse);
		};
	}


	public override void _Process(double delta) {
	}

	public void HandleResponse(Response response) {

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
