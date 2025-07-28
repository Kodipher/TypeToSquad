using Godot;
using System;
using System.Collections.Generic;

using TypeToSquad.Utils;
using GodotDictionary = Godot.Collections.Dictionary;

using WinRTSpeechSynthServer.Protocol.Messages;


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

	record class ContextFullSegment : ContextStartSegment {
		public required int TextEndExclusive { get; init; }
	}

	record class ContextEnd : DepthSegment {
		public required int TextEndExclusive { get; init; }
	}

	/// <summary>
	/// Returns a list of segments in the message, 
	/// split by depth of nesting.
	/// </summary>
	/// <remarks>
	/// Segments may have a length of 0.
	/// The first and last segments are always at depth 0.
	/// Empty <see cref="ContextEnd"/> segments may be appended to return to depth 0.
	/// Depth is never negative.
	/// </remarks>
	static List<DepthSegment> SegmentMessage(string message) {

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
				depthSegments.Add(new ContextEnd() {
					Depth = currentDepth,
					Start = currentBlockStartI,
					EndExclusive = i + 1,
					TextEndExclusive = i
				});

				currentBlockStartI = i + 1;
				currentDepth--;
				continue;
			}

		}

		// Add the last block if only block or last block is not empty
		if (depthSegments.Count == 0 || currentBlockStartI != message.Length) {
			depthSegments.Add(new DepthSegment() {
				Depth = currentDepth,
				Start = currentBlockStartI,
				EndExclusive = message.Length
			});
		}

		// Parse context starts and fulls
		for (int i = 1; i < depthSegments.Count; i++) {

			// Skip if not context start
			if (depthSegments[i].Depth <= depthSegments[i - 1].Depth) {
				continue;
			}

			ContextEnd? unfinishedFullSegemnt = depthSegments[i] as ContextEnd;

			// Find enclosed substring
			int enclosedStartI = depthSegments[i].Start + 1;
			ReadOnlySpan<char> enclosedSubstr;
			if (unfinishedFullSegemnt is not null) {
				enclosedSubstr = message.AsSpan()[enclosedStartI..unfinishedFullSegemnt.TextEndExclusive];
			} else {
				enclosedSubstr = message.AsSpan()[enclosedStartI..(depthSegments[i].EndExclusive)];
			}

			// Find hint bounds
			int hintEndExclusiveSubstrI;
			bool hasSpace;

			int spaceSubstrI = enclosedSubstr.IndexOf(' ');
			if (spaceSubstrI < 0) {
				hasSpace = false;
				hintEndExclusiveSubstrI = enclosedSubstr.Length;
			} else {
				hasSpace = true;
				hintEndExclusiveSubstrI = spaceSubstrI;
			}

			// Convert segment
			if (unfinishedFullSegemnt is not null) {
				depthSegments[i] = new ContextFullSegment() {
					Depth = unfinishedFullSegemnt.Depth,
					Start = unfinishedFullSegemnt.Start,
					EndExclusive = unfinishedFullSegemnt.EndExclusive,
					HintStart = enclosedStartI,
					HintEndExclusive = enclosedStartI + hintEndExclusiveSubstrI,
					TextStart = hasSpace ? enclosedStartI + hintEndExclusiveSubstrI + 1 : unfinishedFullSegemnt.TextEndExclusive,
					TextEndExclusive = unfinishedFullSegemnt.TextEndExclusive
				};
			} else {
				// start of a conext
				depthSegments[i] = new ContextStartSegment() {
					Depth = depthSegments[i].Depth,
					Start = depthSegments[i].Start,
					EndExclusive = depthSegments[i].EndExclusive,
					HintStart = enclosedStartI,
					HintEndExclusive = enclosedStartI + hintEndExclusiveSubstrI,
					TextStart = hasSpace ? enclosedStartI + hintEndExclusiveSubstrI + 1 : depthSegments[i].EndExclusive

				};
			}

		}

		// Pad with empty segments to return to depth 0
		while (depthSegments[^1].Depth != 0) {
			depthSegments.Add(new ContextEnd() {
				Depth = depthSegments[^1].Depth - 1,
				Start = message.Length,
				EndExclusive = message.Length,
				TextEndExclusive = message.Length
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
			GD.Print($"\"{message[item.Start..item.EndExclusive]}\" D={item.Depth} | {item}");
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

		string? cacheText = null;
		List<DepthSegment>? segmentsCache = null;

		void PopulateCache(string message) {
			if (cacheText == message) return;
			cacheText = message;
			segmentsCache = SegmentMessage(message);
		}

		public override GodotDictionary _GetLineSyntaxHighlighting(int line) {

			TextEdit source = this.GetTextEdit();

			// Segment the text
			PopulateCache(source.Text);
			if (segmentsCache is null) {
				GD.PushError("Failed to populate highlighter cache");
				return new();
			} 

			// Find start index
			int startCharI = source.GetLineStartIndex(line);
			int lineLength = source.GetLine(line).Length;

			// Define return object according to docs
			GodotDictionary colors = new();

			void AddColorChange(int col, Color color) {
				colors[col] = new GodotDictionary() { ["color"] = color };
			}

			// Process colors
			// DEBUG: BASED ON DEPTH ONLY
			Color[] colorsByDepth = [new Color(1, 1, 1), new Color(1, 1, 0), new Color(0, 1, 1), new Color(1, 0, 1)];

			for (int segmentI = 0; segmentI < segmentsCache.Count; segmentI++) {
				var currentSegment = segmentsCache[segmentI];

				if (startCharI >= currentSegment.EndExclusive) continue;
				if (startCharI + lineLength <= currentSegment.Start) break;

				AddColorChange(Math.Max(startCharI, currentSegment.Start) - startCharI, colorsByDepth[currentSegment.Depth]);
			}

			return colors;
		}

		public override void _ClearHighlightingCache() {
			cacheText = null;
			segmentsCache = null;
		}

	}

}
