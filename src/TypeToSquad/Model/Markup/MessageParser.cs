using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model.Markup;


public enum HintType {
	Unknown = 0,
	ReplacementContext,
	VoiceChange,

	/// <summary>The size of the enum</summary>
	Max
}


/// <summary>
/// <para>
/// A special parser to allow user "lexicon" (implemented via regex replacements),
/// as well certain SSML features.
/// </para>
/// <para>
/// The message may have tags in it, indicated by square brackets.
/// A tag can be
/// - a voice change
/// - a replacement context change (including empty context)
/// - a part of a message that is not direct text (like ipa phonetic spelling).
/// </para>
/// <para>Tags are not allowed to be inside tags.</para>
/// </summary>
public class MessageParser : IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	/// <summary>
	/// Given a <see cref="HintSegment"/>, returns a new <see cref="HintSegment"/> 
	/// with <see cref="HintSegment.HintType"/> set.
	/// </summary>
	public HintSegment CreateTypedHintSegment(HintSegment segment) {

		if (CoreNode is null) return HintSegment.CreateWithType(segment, HintType.Unknown);

		// Check for empty context
		if (string.IsNullOrWhiteSpace(segment.Hint)) {
			return HintSegment.CreateWithType(segment, HintType.ReplacementContext);
		}

		// Check for languages
		bool hintInLanguages = CoreNode
								.UserSettings
								.VoiceChanges
								.Any(row => row.hint.Trim() == segment.Hint);
		if (hintInLanguages) return HintSegment.CreateWithType(segment, HintType.VoiceChange);

		// Check for replacements
		bool hintInReplacements = CoreNode
								.UserSettings
								.TextReplacements
								.Any(row => row.context.Trim() == segment.Hint);
		if (hintInReplacements) return HintSegment.CreateWithType(segment, HintType.ReplacementContext);

		// Nothing found
		return HintSegment.CreateWithType(segment, HintType.Unknown);
	}


	/// <summary>Returns a list of segments that make up the message.</summary>
	public List<MessageSegment> SegmentMessage(string message) {

		List<MessageSegment> segments = new();

		/// <summary>
		/// Iterates message string index until 
		/// the tag is closed or the message is over.
		/// </summary>
		/// <remarks>
		/// <paramref name="i"/> is assumed to start 
		/// at the openign of the tag.
		/// </remarks>
		void ScanUntilClosed(ref int i, out bool hasNested) {

			int additionalDepth = 0;
			hasNested = false;

			i++; // "consume" tag opening
			for (; i < message.Length; i++) {

				if (message[i] == '[') {
					additionalDepth++;
					hasNested = true;
					continue;
				}

				if (message[i] == ']') {
					if (additionalDepth == 0) break;
					additionalDepth--;
					continue;
				}

			}
		}

		bool IsSegmentContent(int openingI, int closingI, out int hintExclusiveEndI) {

			// A tag is a content if there is whitesapce
			// separating non-whitespace on either side
			int firstNonWhitespace = -1;
			int lastNonWhitespace = -1;
			hintExclusiveEndI = -1;	// first whitespace after first nonwhitespace

			for (int i = openingI + 1; i <= closingI - 1; i++) {

				if (char.IsWhiteSpace(message[i])) {

					if (hintExclusiveEndI != -1) continue;
					if (firstNonWhitespace == -1) continue;
					hintExclusiveEndI = i;

				} else {

					if (firstNonWhitespace == -1) firstNonWhitespace = i;
					lastNonWhitespace = i;
				}

			}

			// No non-whitespace
			if (firstNonWhitespace == -1) return false;

			// No whitespace after first non-whitespace
			if (hintExclusiveEndI == -1) return false;

			// No non-whitespace in the middle
			if (hintExclusiveEndI > lastNonWhitespace) return false;

			return true;
		}

		int currentSegmentStartI = 0;

		for (int i = 0; i < message.Length; i++) {

			if (message[i] == '[') {

				// Add text before tag
				if (i != currentSegmentStartI) {
					segments.Add(
						MessageSegment.CreateAsSubstring(
							start: currentSegmentStartI,
							endExclusive: i,
							str: message
						)
					);

					currentSegmentStartI = i;
				}

				// Find closing (assuming nesting)
				ScanUntilClosed(ref i, out bool hasNested);

				// Closed segment
				if (i < message.Length) {

					if (hasNested) {
						segments.Add(
							InvalidSegment.CreateAsSubstring(
								start: currentSegmentStartI,
								endExclusive: i + 1,
								str: message
							)
						);
						currentSegmentStartI = i + 1;
						continue;
					}

					bool isContent = IsSegmentContent(currentSegmentStartI, i, out int hintEndExclusive);

					// Content
					if (isContent) {
						segments.Add(
							ContentSegment.CreateAsSubstring(
								start: currentSegmentStartI,
								endExclusive: i + 1,
								hintEndExclusive: hintEndExclusive,
								str: message
							)
						);
						currentSegmentStartI = i + 1;
						continue;
					}

					// Hint with no content
					segments.Add(
						CreateTypedHintSegment(
							HintSegment.CreateAsSubstring(
								start: currentSegmentStartI,
								endExclusive: i + 1,
								str: message
							)
						)
					);
					currentSegmentStartI = i + 1;
					continue;
				}

				// Unclosed segment
				segments.Add(
					InvalidSegment.CreateAsSubstring(
						start: currentSegmentStartI,
						endExclusive: message.Length,
						str: message
					)
				);
				currentSegmentStartI = message.Length;
				break;

			}
			
			// [continue]
		}

		// Add a segment lasting till the end if not there
		if (currentSegmentStartI < message.Length) {
			segments.Add(
				MessageSegment.CreateAsSubstring(
					start: currentSegmentStartI,
					endExclusive: message.Length,
					str: message
				)
			);
		}

		return segments;
	}

	public static string SegmentedMessageToSsml(IReadOnlyList<MessageSegment> segments, string message) {
		return "";
	}

}
