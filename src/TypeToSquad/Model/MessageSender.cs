using Godot;
using System;
using System.Linq;

using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Model;


/// <summary>
/// Handles messgae parsing (into requests)
/// and sending those rerequest via <see cref="SpeechDaemon"/>.
/// </summary>
public class MessageSender : IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	public void SendMessage(string message) {
		
		SynthesizeRequest request = new SynthesizeRequest();

		// Message
		ParseMessageText(in request, message);

		// Voice
		request.VoiceName = CoreNode.UserSettings.Voice;
		request.Pitch = CoreNode.UserSettings.VoicePitch;
		request.Rate = CoreNode.UserSettings.VoiceRate;
		request.Volume = CoreNode.UserSettings.SynthesisVolumePercent / 100.0;

		// Send request
		CoreNode.SpeechDaemon.DispatchRequest(
			request,
			responce => {

				if (responce is not SyntesisResultResponse synthesisResult) {
					GD.PushError("Synthesis request response is not a SyntesisResultResponse.");
					return;
				}

				if (!synthesisResult.GivenVoiceExists) {
					GD.PushError("Selected voice does not exist.");
					var voiceField = CoreNode.UserSettings.Voice;
					voiceField.Value = voiceField.DefaultValue;
				}

				// Play resulting data
				CoreNode.AudioManager.PlayNew(synthesisResult.SynthesizedData);
			}
		);
	}

	void ParseMessageText(in SynthesizeRequest request, string message) {

		var parser = CoreNode.MessageParser;

		var segments = parser.SegmentMessage(message);

		// Text replacements
		for (int i = 0, n = CoreNode.UserSettings.MaxReplacementPasses; i < n; i++) {
			segments = parser.ReplaceTextSinglePass(segments, out bool anyReplaced);
			segments = parser.JoinPlainTextSegements(segments);
			if (!anyReplaced) break;

			if (i == n - 1) GD.PushError("Text replacement passes limit reached.");
		}

		// Stip non-content stuff
		parser.StripInvalidSegments(segments);
		parser.StripReplacementContextSegments(segments);
		segments = parser.JoinPlainTextSegements(segments);

		// Text-only message
		if (segments.All(seg => seg is Markup.PlainTextSegment)) {
			request.InputString = parser.SegmentedMessageToPlainText(segments);
			request.IsSsml = false;
			return;
		}

		// Message with contexts
		request.InputString = parser.SegmentedMessageToSsml(segments);
		request.IsSsml = true;
	}

}
