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

	#region //// Segmenting

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

	/// <remarks>
	/// Inherits <see cref="ContextStartSegment"/> 
	/// but <b>not</b> <see cref="ContextEnd"/>.
	/// </remarks>
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

	#endregion

	#region //// Pasing

	const string buildinHintIpa = "ipa";

	string SegmentedMessageToSsml(IReadOnlyList<DepthSegment> segments, string message) {
		return "";
	}

	void ParseMessageText(in SynthesizeRequest request, string message) {

		var segments = SegmentMessage(message);

		// Text-only message
		if (segments.Count == 1) {
			request.InputString = message;
			request.IsSsml = false;
			return;
		}

		// DEBUG
		request.InputString = "";
		request.IsSsml = false;
		return;

		// Message with contexts
		request.InputString = SegmentedMessageToSsml(segments, message);
		request.IsSsml = true;
	}

	#endregion


	public partial class SyntaxHighligher : Godot.SyntaxHighlighter {

		#region //// Cache

		string? cacheText = null;
		List<DepthSegment>? segmentsCache = null;

		void PopulateSegmentCache(string message) {
			if (cacheText == message) return;
			cacheText = message;
			segmentsCache = SegmentMessage(message);
			segmentColors = null;
		}
		public override void _ClearHighlightingCache() {
			cacheText = null;
			segmentsCache = null;
			segmentColors = null;
		}

		#endregion

		#region //// Colors

		readonly static Color colorDefault = new(1, 1, 1);
		readonly static Color colorSkipped = new(colorDefault, 0.5f);
		readonly static Color colorIpa = new(0.5f, 1, 1);

		Color[]? segmentColors = null;

		void PopulateColors() {

			if (segmentsCache is null || cacheText is null) {
				GD.PushError("Failed to populate colors. Segment cache is empty.");
				return;
			}

			segmentColors = new Color[segmentsCache.Count];
			List<string?> checkedContextHintStack = new();

			for (int i = 0; i < segmentsCache.Count; i++) {

				// Context enter
				if (segmentsCache[i] is ContextStartSegment conextStart) {
					string hint = cacheText[conextStart.HintStart..conextStart.HintEndExclusive];
					if (hint.Equals(buildinHintIpa, StringComparison.InvariantCultureIgnoreCase)) {
						checkedContextHintStack.Add(buildinHintIpa);
					} else {
						checkedContextHintStack.Add(null);
					}
				}

				// Check current context
				if (checkedContextHintStack.Count == 0) {
					segmentColors[i] = colorDefault;
					continue;
				}

				int? topMostIpaDepth = null;
				for (int j = 0; j < checkedContextHintStack.Count; j++) {
					if (checkedContextHintStack[j] is null) break;
					if (checkedContextHintStack[j] == buildinHintIpa) {
						topMostIpaDepth = j;
						break;
					}
				}

				if (checkedContextHintStack[^1] is null) {
					segmentColors[i] = colorSkipped;
				} else if (topMostIpaDepth == checkedContextHintStack.Count - 1) {
					segmentColors[i] = colorIpa;
				} else if (topMostIpaDepth is not null) {
					segmentColors[i] = colorSkipped;
				}


				// Context exit
				if (segmentsCache[i] is ContextEnd or ContextFullSegment) {
					checkedContextHintStack.RemoveAt(checkedContextHintStack.Count - 1);
				}

			}
		}

		#endregion

		public override GodotDictionary _GetLineSyntaxHighlighting(int line) {

			TextEdit source = this.GetTextEdit();

			// Segment the text, process colors
			PopulateSegmentCache(source.Text);
			PopulateColors();
			if (segmentsCache is null || segmentColors is null) {
				GD.PushError("Failed to populate highlighter or color cache.");
				this.ClearHighlightingCache();
				return new();
			} 

			// Find start index
			int startCharI = source.GetLineStartIndex(line);
			int lineLength = source.GetLine(line).Length;

			// Relay colors in the correct format
			GodotDictionary colors = new();

			void AddColorChange(int col, Color color) {
				colors[col] = new GodotDictionary() { ["color"] = color };
			}

			for (int segmentI = 0; segmentI < segmentsCache.Count; segmentI++) {
				var currentSegment = segmentsCache[segmentI];

				// Bounds check
				if (startCharI >= currentSegment.EndExclusive) continue;
				if (startCharI + lineLength <= currentSegment.Start) break;

				AddColorChange(
					Math.Max(startCharI, currentSegment.Start) - startCharI,
					segmentColors[segmentI]
				);
			}

			return colors;
		}

	}

}
