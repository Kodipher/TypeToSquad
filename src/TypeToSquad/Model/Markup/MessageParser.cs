using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security;


namespace TypeToSquad.Model.Markup;


[Flags]
public enum ContextUses {
	None = 0,
	Empty = 1,			// Empty contexts have special treatment
	Replacements = 2,
	VoiceChange = 4
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

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	#region //// Content and context type parsing

	readonly static ReadOnlyDictionary<string, ContentType> contextHintStrings =
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

	/// <summary>Removes invalid segments and invalid content segments</summary>
	public void StripInvalidSegments(List<MessageSegment> segments) {
		segments.RemoveAll(seg => {
			if (seg is InvalidSegment) return true;
			if (seg is ContentSegment conSeg && conSeg.Type == ContentType.Invalid) return true;
			return false;
		});
	}

	#region //// Text replacements

	const string allContextsMarker = "*";

	/// <summary>
	/// Creates a new list of segments by performing 1 pass of text replacements in current segments.
	/// <b>Note:</b> the indexes in the returned segments may become invalid.
	/// </summary>
	public List<MessageSegment> ReplaceTextSinglePass(IEnumerable<MessageSegment> segments, out bool anyTextReplaced) {
		anyTextReplaced = false;

		string currentContext = "";
		List<MessageSegment> newSegments = new();

		foreach (MessageSegment seg in segments) {

			// Context changes
			if (seg is ContextSegment hintSeg) {
				currentContext = hintSeg.Context;
			}

			// Add everything but text directly
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
				Regex patternRegex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
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

	/// <summary>
	/// Returns a new list of segments where adjacent <see cref="PlainTextSegment"/>s
	/// in the source <paramref name="segments"/> array are joined into one.
	/// <b>Note:</b> the indexes in the returned segments may become invalid.
	/// </summary>
	public List<MessageSegment> JoinPlainTextSegements(List<MessageSegment> segments) {

		List<MessageSegment> newSegments = new();

		foreach (MessageSegment seg in segments) {

			// Add non-text segments directly
			if (seg is not PlainTextSegment) {
				newSegments.Add(seg);
				continue;
			}

			// Add text segments not after text segments
			if (newSegments.Count == 0 || newSegments[^1] is not PlainTextSegment) {
				newSegments.Add(seg);
				continue;
			}

			// Join text segments
			newSegments[^1] = PlainTextSegment.CreateFromText(newSegments[^1].Text + seg.Text);
		}

		return newSegments;
	}

	#endregion

	public bool IsPlainTextOnly(IEnumerable<MessageSegment> segments) {

		foreach (var seg in segments) {

			if (seg is PlainTextSegment) continue;

			if (
				seg is ContextSegment contextSeg && 
				(contextSeg.ContextUses == ContextUses.Empty ||
				contextSeg.ContextUses == ContextUses.Replacements)
			) {
				continue;
			}

			return false;
		}

		return true;
	}

	public string SegmentedMessageToPlainText(IEnumerable<MessageSegment> segments) {
		StringBuilder sb = new StringBuilder();
		foreach (var seg in segments) {
			if (seg is not PlainTextSegment) continue;
			sb.Append(seg.Text);
		}
		return sb.ToString();
	}

	#region //// SSML

	const string ssmlHeaderFormat = """
<speak version="1.0"
	xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{0}">
""";

	const string ssmlFooter = """</speak>""";

	const string ssmlVoiceOpen = """<voice name="{0}" xml:lang="{1}">""";
	const string ssmlVoiceClose = """</voice>""";

	const string ssmlIpa = """<phoneme alphabet="ipa" ph="{0}"></phoneme>""";

	/// <remarks>Assumes invalid segments were already stripped.</remarks>
	public string SegmentedMessageToSsml(IEnumerable<MessageSegment> segments) {
		if (CoreNode.SpeechDaemon.VoicesByName is null) throw new InvalidOperationException("Cannot find voice information.");

		StringBuilder sb = new StringBuilder();

		// Header
		string defaultVoiceName = CoreNode.UserSettings.Voice;
		string defaultVoiceLang = CoreNode.SpeechDaemon.VoicesByName[defaultVoiceName].Language;
		sb.AppendFormat(ssmlHeaderFormat, defaultVoiceLang);

		// Segments
		bool isInsideVoice = false;

		foreach (var seg in segments) {

			if (seg is PlainTextSegment) {
				string segText = seg.Text;
				segText = SecurityElement.Escape(segText);
				sb.Append(segText);
			}

			if (seg is ContextSegment hintSegment) {

				// Voice changes
				if (isInsideVoice) {
					sb.Append(ssmlVoiceClose);
					isInsideVoice = false;
				}

				if (hintSegment.ContextUses.HasFlag(ContextUses.VoiceChange)) {
					
					string voiceNameKey = CoreNode
						.UserSettings
						.VoiceChanges
						.FirstOrDefault(row => row.hint == hintSegment.Context)
						.voiceName;

					if (!CoreNode.SpeechDaemon.VoicesByName.TryGetValue(voiceNameKey, out var voiceInfo)) {
						continue;
					}

					string voiceName = SecurityElement.Escape(voiceInfo.Name);
					string voiceLang = SecurityElement.Escape(voiceInfo.Language);

					sb.AppendFormat(ssmlVoiceOpen, voiceName, voiceLang);
					isInsideVoice = true;
				}
			}

			// Content
			if (seg is ContentSegment contentSegment) {

				switch (contentSegment.Type) {

					case ContentType.Ipa:
						sb.AppendFormat(ssmlIpa, SecurityElement.Escape(contentSegment.Payload));
						break;

					case ContentType.Audio:
						throw new NotImplementedException("Inserting audio is not implemented.");
						break;

					default:
						throw new ArgumentException("Invalid inline content segmet.");
				}

			}

			// [continue]
		}

		if (isInsideVoice) sb.Append(ssmlVoiceClose);
		sb.Append(ssmlFooter);

		return sb.ToString();
	}

	#endregion

}
