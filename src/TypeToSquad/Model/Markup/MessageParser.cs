using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace TypeToSquad.Model.Markup;


public enum HintType {
	Unset = 0,
	ReplacementContext,
	UnknownReplacementContext,
	VoiceChange,

	/// <summary>The size of the enum</summary>
	Max
}

public enum ContentType {
	Invalid = 0,
	Ipa = 1,
	Audio = 2
}


/// <summary>
/// A special parser to allow user "lexicon" (implemented via regex replacements),
/// as well certain SSML features.
/// See docs folder for details.
/// </summary>
public class MessageParser : IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Typed hint and content segments

	readonly static ReadOnlyDictionary<string, ContentType> contextHintStrings =
		new Dictionary<string, ContentType>() {
			["ipa"] = ContentType.Ipa,
			["snd"] = ContentType.Audio,
			["audio"] = ContentType.Audio
		}.AsReadOnly();

	/// <summary>
	/// Given a <see cref="ContentSegment"/>, returns a new <see cref="ContentSegment"/> 
	/// with <see cref="ContentSegment.ContentType"/> set.
	/// </summary>
	ContentSegment CreateTypedContentSegment(ContentSegment segment) {
		if (CoreNode is null) return ContentSegment.CreateWithType(segment, ContentType.Invalid);

		var contextType = 
			contextHintStrings
			.FirstOrDefault(
				pair => pair.Key == segment.HintText,
				KeyValuePair.Create("", ContentType.Invalid)
			).Value;
		
		return ContentSegment.CreateWithType(segment, contextType);
	}

	/// <summary>
	/// Given a <see cref="HintSegment"/>, returns a new <see cref="HintSegment"/> 
	/// with <see cref="HintSegment.HintType"/> set.
	/// </summary>
	HintSegment CreateTypedHintSegment(HintSegment segment) {
		if (CoreNode is null) return HintSegment.CreateWithType(segment, HintType.Unset);

		// Check for empty context
		if (string.IsNullOrWhiteSpace(segment.HintText)) {
			return HintSegment.CreateWithType(segment, HintType.ReplacementContext);
		}

		// Check for languages
		bool hintInLanguages = CoreNode
								.UserSettings
								.VoiceChanges
								.Any(row => row.hint.Trim() == segment.HintText);
		if (hintInLanguages) return HintSegment.CreateWithType(segment, HintType.VoiceChange);

		// Check for replacements
		bool hintInReplacements = CoreNode
								.UserSettings
								.TextReplacements
								.Any(row => row.context.Trim() == segment.HintText);
		if (hintInReplacements) return HintSegment.CreateWithType(segment, HintType.ReplacementContext);

		// Nothing found
		return HintSegment.CreateWithType(segment, HintType.UnknownReplacementContext);
	}

	#endregion

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

			}

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

	/// <summary>Removes invalid segments and invalid content segments</summary>
	public void StripInvalidSegments(List<MessageSegment> segments) {
		segments.RemoveAll(seg => {
			if (seg is InvalidSegment) return true;
			if (seg is ContentSegment conSeg && conSeg.ContentType == ContentType.Invalid) return true;
			return false;
		});
	}

	/// <summary>Removes replacement context segments.</summary>
	public void StripReplacementContextSegments(List<MessageSegment> segments) {
		segments.RemoveAll(seg => {
			if (seg is not HintSegment hintSeg) return false;
			if (hintSeg.HintType == HintType.ReplacementContext) return true;
			if (hintSeg.HintType == HintType.UnknownReplacementContext) return true;
			return false;
		});
	}

	#region //// Text replacements

	const string allContextsMarker = "*";

	/// <summary>
	/// Creates a new list of segments by performing 1 pass of text replacements in current segments.
	/// <b>Note:</b> the indexes in the returned segments may no longer correspond to the original message!
	/// </summary>
	public List<MessageSegment> ReplaceTextSinglePass(IEnumerable<MessageSegment> segments, out bool anyTextReplaced) {
		anyTextReplaced = false;

		if (CoreNode is null) return segments.ToList();

		string currentContext = "";
		List<MessageSegment> newSegments = new();

		foreach (MessageSegment seg in segments) {

			// Handle context changes
			if (seg is HintSegment hintSeg) {
				if (hintSeg.HintType is HintType.ReplacementContext or HintType.UnknownReplacementContext) {
					currentContext = hintSeg.HintText;
				}
			}

			// Handle everything but text
			if (seg is not PlainTextSegment) {
				newSegments.Add(seg);
				continue;
			}

			// Perform replacements in text
			string newText = seg.Text;
			foreach (var (context, pattern, replacement) in CoreNode.UserSettings.TextReplacements) {

				// Context check
				string contextTrimmed = context.Trim();
				if (
					contextTrimmed != allContextsMarker &&
					contextTrimmed != currentContext
				) {
					continue;
				}

				// Empty pattern
				if (string.IsNullOrWhiteSpace(pattern)) continue;

				// Try replace
				Regex patternRegex = new Regex(pattern, RegexOptions.Singleline);
				newText = patternRegex.Replace(seg.Text, replacement);
				
				// Exit on first replacement
				if (newText != seg.Text) break;
			}

			// No replacement
			if (newText == seg.Text) {
				newSegments.Add(seg);
				continue;
			}

			// Some replacement applied
			anyTextReplaced = true;
			newSegments.AddRange(SegmentMessage(newText));
		}

		return newSegments;
	}

	#endregion

	/// <remarks>Assumes context and invalid segments were already stripped.</remarks>
	public string SegmentedMessageToPlainText(IEnumerable<MessageSegment> segments) {
		StringBuilder sb = new StringBuilder();
		foreach (var seg in segments) {
			if (seg is not PlainTextSegment) continue;
			sb.Append(seg.Text);
		}
		return sb.ToString();
	}

	#region //// SSML

	/// <remarks>Assumes context and invalid segments were already stripped.</remarks>
	public string SegmentedMessageToSsml(IEnumerable<MessageSegment> segments) {
		throw new NotImplementedException();
		// reminder: escape xml characters
		return "";
	}

	#endregion

}
