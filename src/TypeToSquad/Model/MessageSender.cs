using Godot;
using System;
using System.Text;

using WinRTSpeechSynthServer.Protocol.Messages;

using GodotDictionary = Godot.Collections.Dictionary;


namespace TypeToSquad.Model;


/// <summary>
/// Handles messgae parsing (into requests)
/// and sending those rerequest via <see cref="SpeechDaemon"/>.
/// </summary>
public partial class MessageSender : IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	public void SendMessage(string message) {
		if (CoreNode is null) return;
		
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
		request.InputString = message;
		request.IsSsml = false;
	}

	public partial class SyntaxHighligher : Godot.SyntaxHighlighter {

		public override GodotDictionary _GetLineSyntaxHighlighting(int line) {
		}

		public override void _ClearHighlightingCache() {
			
		}

	}

}
