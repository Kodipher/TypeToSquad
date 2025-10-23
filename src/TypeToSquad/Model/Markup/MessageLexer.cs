using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace TypeToSquad.Model.Markup;


[Flags]
public enum ContextUses {
	None = 0,
	Empty = 1,          // Empty contexts have special treatment
	Replacements = 2,
	VoiceChange = 4
}

public enum ContentType {
	Invalid = 0,
	Ipa = 1,
	Audio = 2
}


/// <summary>
/// Implements splitting the message into segments,
/// where each segment is either plain text or a tag.
/// </summary>
public class MessageLexer : IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	#region //// Content and context type parsing

	readonly public static ReadOnlyDictionary<string, ContentType> contextHintStrings =
		new Dictionary<string, ContentType>() {
			["ipa"] = ContentType.Ipa,
			["snd"] = ContentType.Audio,
			["audio"] = ContentType.Audio
		}.AsReadOnly();

	/// <summary>
	/// Given a <see cref="ContentSegment"/>, returns a new <see cref="ContentSegment"/> 
	/// with <see cref="ContentSegment.Type"/> set.
	/// </summary>
	ContentSegment CreateTypedContentSegment(ContentSegment segment) {

		var contextType =
			contextHintStrings
			.FirstOrDefault(
				pair => pair.Key == segment.TypeText,
				KeyValuePair.Create("", ContentType.Invalid)
			).Value;

		return ContentSegment.CreateWithType(segment, contextType);
	}

	/// <summary>
	/// Given a <see cref="ContextSegment"/>, returns a new <see cref="ContextSegment"/> 
	/// with <see cref="ContextSegment.ContextUses"/> set.
	/// </summary>
	ContextSegment CreateTypedContextSegment(ContextSegment segment) {

		// Empty context
		if (string.IsNullOrWhiteSpace(segment.Context)) {
			return ContextSegment.CreateWithUses(segment, ContextUses.Empty);
		}

		ContextUses currentUses = ContextUses.None;

		// Check for languages
		bool hintInLanguages = CoreNode
								.UserSettings
								.VoiceChanges
								.Any(row => row.hint.Trim() == segment.Context);
		if (hintInLanguages) {
			currentUses |= ContextUses.VoiceChange;
		}

		// Check for replacements
		bool hintInReplacements = CoreNode
								.UserSettings
								.TextReplacements
								.Any(row => row.context.Trim() == segment.Context);
		if (hintInReplacements) {
			currentUses |= ContextUses.Replacements;
		}

		// Create new one
		return ContextSegment.CreateWithUses(segment, currentUses);
	}

	#endregion

	/// <summary>Returns a list of segments that make up the message.</summary>
	public List<MessageSegment> SegmentMessage(string message) {

		/// <summary>
		/// Iterates message string index until 
		/// the tag is closed or the message is over.
		/// </summary>
		/// <remarks>
		/// <paramref name="i"/> is assumed to start 
		/// at the opening of the tag.
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
			hintExclusiveEndI = -1; // first whitespace after first nonwhitespace

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

		// ----------


		List<MessageSegment> segments = new();

		int currentSegmentStartI = 0;

		for (int i = 0; i < message.Length; i++) {

			// Scan until tag opening is found
			if (message[i] != '[') continue;

			// Add text before tag
			if (i != currentSegmentStartI) {
				segments.Add(
					PlainTextSegment.CreateAsSubstring(
						start: currentSegmentStartI,
						endExclusive: i,
						str: message
					)
				);

				currentSegmentStartI = i;
			}

			// Find closing (assuming nesting)
			ScanUntilClosed(ref i, out bool hasNested);

			// Unclosed tag
			if (i >= message.Length) {
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

			// Closed tag but has nesting
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
					CreateTypedContentSegment(
						ContentSegment.CreateAsSubstring(
							start: currentSegmentStartI,
							endExclusive: i + 1,
							hintEndExclusive: hintEndExclusive,
							str: message
						)
					)
				);
				currentSegmentStartI = i + 1;
				continue;
			}

			// Context (not content)
			segments.Add(
				CreateTypedContextSegment(
					ContextSegment.CreateAsSubstring(
						start: currentSegmentStartI,
						endExclusive: i + 1,
						str: message
					)
				)
			);
			currentSegmentStartI = i + 1;

			// [continue]
		}

		// Add a segment lasting till the end if not there
		if (currentSegmentStartI < message.Length) {
			segments.Add(
				PlainTextSegment.CreateAsSubstring(
					start: currentSegmentStartI,
					endExclusive: message.Length,
					str: message
				)
			);
		}

		return segments;
	}

}
