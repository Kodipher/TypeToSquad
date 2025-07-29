using System;
using System.Collections.Generic;


namespace TypeToSquad.Model.Markup;


internal class MessageParser {

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
	public static List<DepthSegment> SegmentMessage(string message) {

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

	public static string SegmentedMessageToSsml(IReadOnlyList<DepthSegment> segments, string message) {
		return "";
	}

}
