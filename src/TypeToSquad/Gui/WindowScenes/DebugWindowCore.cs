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
public partial class DebugWindowCore : Control, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	// TEST BENCH
	Label labelHeart = null!;
	Label labelResponse = null!;
	TextEdit textEditInput = null!;

	public override void _Ready() {

		// Check if actually in a subwindow
		Node? parent = this.GetParent();
		if (parent is Window parentWindow) {
			parentWindow.CloseRequested += () => parentWindow.QueueFree();
		}

		// Set up test bench
		labelResponse = this.GetNodeNotNull<Label>("%LabelResponse");
		textEditInput = this.GetNodeNotNull<TextEdit>("%TextEditInput");

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
		/*
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

		var speakSsmlButton = this.GetNodeNotNull<Button>("%ButtonSpeakSsml");
		speakSsmlButton.Pressed += () => {
			CoreNode?.SpeechDaemon.DispatchRequest(new SynthesizeSsmlRequest() { InputString = textEditInput.Text }, HandleResponse);
		};
		*/
	}


	public void HandleResponse(Response response) {

		if (response is HeartbeatEchoResponse echoResponce) {
			labelResponse.Text = "HeartbeatEcho:" + echoResponce.EchoByte.ToString("X2");
		} else if (response is TerminateAcceptedResponse) {
			labelResponse.Text = "Terminated";
		} else if (response is AllVoicesResponse allVoicesResponse) {
			labelResponse.Text = allVoicesResponse.Voices.Select(voice => voice.Name).JoinString("\n");
		/*
		} else if (response is VoiceSetResponse voiceSetResponse) {
			labelResponse.Text = "Was set? " + voiceSetResponse.WasSet.ToString();
		} else if (response is DefaultVoiceSetResponse defaultVoiceResponse) {
			labelResponse.Text = "Default: " + defaultVoiceResponse.DefaultVoice.Name;
		*/
		} else if (response is SyntesisResultResponse speechResponce) {
			labelResponse.Text = "Speech,len=" + speechResponce.SynthesizedData.Length.ToString();
			CoreNode?.AudioManager?.PlayNew(speechResponce.SynthesizedData);
		} else {
			labelResponse.Text = response.Type.ToString();
		}
		
	}

}
