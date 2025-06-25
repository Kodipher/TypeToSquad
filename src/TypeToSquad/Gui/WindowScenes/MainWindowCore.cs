using Godot;
using System;
using System.Linq;

using TypeToSquad.Utils;
using Rephidock.GeneralUtilities.Collections;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


/// <remarks>
/// This contents of this window are to be unpacked,
/// thefore the main window has its programing in an immediate single child.
/// </remarks>
public partial class MainWindowCore : Node, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	// TEST BENCH
	Label labelHeart = null!;
	Label labelResponse = null!;
	TextEdit textEditInput = null!;

	AudioStreamPlayer streamPlayer = null!;
	AudioStreamWav currentStream = null!;

	public override void _Ready() {

		labelResponse = this.GetNodeNotNull<Label>("%LabelResponse");
		textEditInput = this.GetNodeNotNull<TextEdit>("%TextEditInput");
		streamPlayer = this.GetNodeNotNull<AudioStreamPlayer>("%AudioStreamPlayer");

		var terminateButton = this.GetNodeNotNull<Button>("%ButtonTerminate");
		terminateButton.Pressed += () => CoreNode?.SpeechDaemon.DispatchRequest(new TerminateRequest(), HandleResponse);

		var heatbeatButton = this.GetNodeNotNull<Button>("%ButtonHeartbeat");
		labelHeart = this.GetNodeNotNull<Label>("%LabelHeart");
		heatbeatButton.Pressed += () => {
			byte value = (byte)Random.Shared.Next(0x100);
			labelHeart.Text = value.ToString("X2");
			CoreNode?.SpeechDaemon.DispatchRequest(new HeartbeatRequest() { EchoByte = value }, HandleResponse);
		};

		var getVoicesButton = this.GetNodeNotNull<Button>("%ButtonGetVoices");
		getVoicesButton.Pressed += () => CoreNode?.SpeechDaemon.DispatchRequest(new GetVoicesRequest(), HandleResponse);

		var setVoiceButton = this.GetNodeNotNull<Button>("%ButtonSetVoice");
		setVoiceButton.Pressed += () => {
			CoreNode?.SpeechDaemon.DispatchRequest(new SetVoiceRequest() { VoiceName = textEditInput.Text }, HandleResponse);
		};

		var setDefaultVoiceButton = this.GetNodeNotNull<Button>("%ButtonSetDefaultVoice");
		setDefaultVoiceButton.Pressed += () => CoreNode?.SpeechDaemon.DispatchRequest(new SetVoiceToDefaultRequest(), HandleResponse);

		var speakButton = this.GetNodeNotNull<Button>("%ButtonSpeak");
		speakButton.Pressed += () => {
			CoreNode?.SpeechDaemon.DispatchRequest(new SynthesizeTextRequest() { InputString = textEditInput.Text }, HandleResponse);
		};

	}


	public override void _Process(double delta) {
	}

	public void HandleResponse(Response response) {

		if (response is HeartbeatEchoResponse echoResponce) {
			labelResponse.Text = "HeartbeatEcho:" + echoResponce.EchoByte.ToString("X");
		} else if (response is TerminateAcceptedResponse) {
			labelResponse.Text = "Terminated";
		} else if (response is AllVoicesResponse allVoicesResponse) {
			labelResponse.Text = allVoicesResponse.Voices.Select(voice => voice.Name).JoinString("\n");
		} else if (response is VoiceSetResponse voiceSetResponse) {
			labelResponse.Text = "Was set? " + voiceSetResponse.WasSet.ToString();
		} else if (response is DefaultVoiceSetResponse defaultVoiceResponse) {
			labelResponse.Text = "Default: " + defaultVoiceResponse.DefaultVoice.Name;
		} else if (response is SyntesisResultResponse speechResponce) {
			labelResponse.Text = "Speech,len=" + speechResponce.SynthesizedData.Length.ToString();

			const int wavImportCompressModePcm = 0;
			const int wavImportLoopModeDisabled = 1;

			if (currentStream is not null) {
				streamPlayer.Stop();
				streamPlayer.Stream = null;
				currentStream.Dispose();
			}

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
