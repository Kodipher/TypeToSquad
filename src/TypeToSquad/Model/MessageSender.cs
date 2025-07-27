using Godot;
using System;
using System.Collections.Generic;
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

	#region //// Parsing

	record class DepthSegment {
		public required int Start { get; init; }
		public required int EndExclusive { get; init; }
		public required int Depth { get; init; }
	};

	record class ContextStartSegment : DepthSegment {
		public required int HintStart { get; init; }
		public required int HintEndExclusive { get; init; }
		public required int TextStart { get; init; }
	}

	/// <summary>
	/// Returns a list of segments in the message, 
	/// split by depth of nesting.
	/// </summary>
	/// <remarks>
	/// The first segment is always at depth 0.
	/// The first segment may have a length of 0.
	/// The last segment may end at a depth above 0.
	/// Depth is never negative.
	/// </remarks>
	List<DepthSegment> SegmentMessage(string message) {

		// Segment message into depth blocks
		// Depth blocks include brackets
		// The first block always has a depth of 0 but may have 0 length
		List<DepthSegment> depthSegments = new();

		int currentDepth = 0;
		int currentBlockStartI = 0;

		for (int i = 0; i < message.Length; i++) {

			if (message[i] == '[') {
				// End block and go deeper
				depthSegments.Add(new DepthSegment() {
					Depth = currentDepth,
					Start = currentBlockStartI,
					EndExclusive = i
				});

				currentBlockStartI = i;
				currentDepth++;
				continue;
			}

			if (message[i] == ']' && currentDepth > 0) {

				// End block and go up
				depthSegments.Add(new DepthSegment() {
					Depth = currentDepth,
					Start = currentBlockStartI,
					EndExclusive = i + 1
				});

				currentBlockStartI = i + 1;
				currentDepth--;
				continue;
			}

		}

		// Add the last block if not empty
		if (currentBlockStartI != message.Length) {
			depthSegments.Add(new DepthSegment() {
				Depth = currentDepth,
				Start = currentBlockStartI,
				EndExclusive = message.Length
			});
		}

		// TODO: hints

		// Pad with empty segments to return to depth 0
		while (depthSegments[^1].Depth != 0) {
			depthSegments.Add(new DepthSegment() {
				Depth = depthSegments[^1].Depth - 1,
				Start = message.Length,
				EndExclusive = message.Length
			});
		}
		
		return depthSegments;
	}

	string SegmentedMessageToSsml(IReadOnlyList<DepthSegment> segments, string message) {
		return "";
	}

	void ParseMessageText(in SynthesizeRequest request, string message) {

		var segments = SegmentMessage(message);

		// DEBUG
		GD.Print("==============");
		foreach (var item in segments) {
			GD.Print(message[item.Start..item.EndExclusive], " D=", item.Depth);
		}
		GD.Print("==============");

		request.InputString = "";
		request.IsSsml = false;
		return;

		// Text-only message
		if (segments.Count == 1) {
			request.InputString = message;
			request.IsSsml = false;
			return;
		}

		// Message with contexts
		request.InputString = SegmentedMessageToSsml(segments, message);
		request.IsSsml = true;
	}

	#endregion


	public partial class SyntaxHighligher : Godot.SyntaxHighlighter {

		public override GodotDictionary _GetLineSyntaxHighlighting(int line) {
		}

		public override void _ClearHighlightingCache() {
			
		}

	}

}
