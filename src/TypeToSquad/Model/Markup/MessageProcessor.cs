using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;


namespace TypeToSquad.Model.Markup;


/// <summary>
/// Processes messages and allows use
/// of some SSML features through markup.
/// See docs folder for details.
/// </summary>
public class MessageProsessor {

	#region //// Segment list operations

	/// <summary>
	/// Returns a new list of segments where adjacent <see cref="PlainTextSegment"/>s
	/// in the source <paramref name="segments"/> array are joined into one.
	/// <b>Note:</b> the indexes in the returned segments may become invalid.
	/// </summary>
	public List<MessageSegment> CombineAdjacentPlainTextSegments(List<MessageSegment> segments) {

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

	/// <summary>Removes invalid segments and invalid content segments</summary>
	public void StripInvalidSegmentsInPlace(List<MessageSegment> segments) {
		segments.RemoveAll(seg => {
			if (seg is InvalidSegment) return true;
			if (seg is ContentSegment conSeg && conSeg.Type == ContentType.Invalid) return true;
			return false;
		});
	}

	#endregion

	#region //// Text replacements

	const string allContextsMarker = "*";

	/// <summary>
	/// Performs a single pass of text replacements 
	/// on a plain text single segment.
	/// </summary>
	/// <remarks>
	/// Does not perform replacements in place,
	/// instead returns the new string.
	/// The new string may contain tags.
	/// </remarks>
	(string newText, bool anyReplaced) ReplaceTextSinglePass(PlainTextSegment segment, string currentContext) {

		var settingsInstance = UserSettingsManager.Instance.Settings;

		string newText = segment.Text;
		foreach (var (context, pattern, replacement) in settingsInstance.TextReplacements) {

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
			newText = patternRegex.Replace(segment.Text, replacement);

			// Exit on first replacement
			if (newText != segment.Text) break;
		}

		// No replacement
		if (newText == segment.Text) {
			return (segment.Text, false);
		}

		return (newText, true);
	}

	/// <summary>
	/// Creates a new list of segments by performing 1 pass of text replacements in current segments.
	/// <b>Note:</b> the indexes in the returned segments may become invalid.
	/// </summary>
	List<MessageSegment> ReplaceTextSinglePassAll(IEnumerable<MessageSegment> segments, out bool anyTextReplaced) {
		anyTextReplaced = false;

		string currentContext = "";
		List<MessageSegment> newSegments = new();

		foreach (MessageSegment seg in segments) {

			// Context changes
			if (seg is ContextSegment hintSeg) {
				currentContext = hintSeg.Context;
			}

			// Add everything but text directly
			if (seg is not PlainTextSegment textSeg) {
				newSegments.Add(seg);
				continue;
			}

			// Perform replacements in text
			(string newText, bool textReplaced) = ReplaceTextSinglePass(textSeg, currentContext);

			if (textReplaced) {
				anyTextReplaced = true;
				newSegments.AddRange(MessageLexer.SegmentMessage(newText));
				continue;
			}

			newSegments.Add(seg); // No replacements
		}

		return newSegments;
	}

	#endregion

	#region //// Compile Plain Text

	bool IsPlainTextOnly(IEnumerable<MessageSegment> segments) {

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

	string SegmentedMessageToPlainText(IEnumerable<MessageSegment> segments) {
		StringBuilder sb = new StringBuilder();
		foreach (var seg in segments) {
			if (seg is not PlainTextSegment) continue;
			sb.Append(seg.Text);
		}
		return sb.ToString();
	}

	#endregion

	#region //// Compile SSML

	const string ssmlHeaderFormat = """
<speak version="1.0"
	xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{0}">
""";

	const string ssmlFooter = """</speak>""";

	const string ssmlVoiceOpen = """<voice name="{0}" xml:lang="{1}">""";
	const string ssmlVoiceClose = """</voice>""";

	const string ssmlIpa = """<phoneme alphabet="ipa" ph="{0}"></phoneme>""";
	const string ssmlBreak = """<break time="{0}"/>""";

	/// <remarks>Assumes invalid segments were already stripped.</remarks>
	string SegmentedMessageToSsml(IEnumerable<MessageSegment> segments) {

		// Singleton shortcuts
		var settingsInstance = UserSettingsManager.Instance.Settings;
		var speechDaemon = SpeechDaemon.Instance;

		// Guards
		if (speechDaemon.VoicesByName is null) throw new InvalidOperationException("Cannot find voice information.");


		
		StringBuilder sb = new StringBuilder();

		// Header
		string defaultVoiceName = settingsInstance.Voice;
		string defaultVoiceLang = speechDaemon.VoicesByName[defaultVoiceName].Language;
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
					
					string voiceNameKey = 
						settingsInstance
						.VoiceChanges
						.FirstOrDefault(row => row.hint == hintSegment.Context)
						.voiceName;

					if (!speechDaemon.VoicesByName.TryGetValue(voiceNameKey, out var voiceInfo)) {
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

					case ContentType.Wait:
						sb.AppendFormat(ssmlBreak, SecurityElement.Escape(contentSegment.Payload));
						break;

					default:
						throw new NotSupportedException("Unknown inline content segmet.");
				}

			}

			// [continue]
		}

		if (isInsideVoice) sb.Append(ssmlVoiceClose);
		sb.Append(ssmlFooter);

		return sb.ToString();
	}

	#endregion

	/// <summary>
	/// Processes the message, performing analysis and text replacements.
	/// Returns a plain text or an ssml message.
	/// </summary>
	public (string requestString, bool isSsml) ProcessMessage(string message) {

		var segments = MessageLexer.SegmentMessage(message);

		// Text replacements
		for (int i = 0, n = UserSettingsManager.Instance.Settings.MaxReplacementPasses; i < n; i++) {
			segments = ReplaceTextSinglePassAll(segments, out bool anyReplaced);
			segments = CombineAdjacentPlainTextSegments(segments);
			if (!anyReplaced) break;

			if (i == n - 1) GD.PushError("Text replacement passes limit reached.");
		}

		// Stip non-content stuff
		StripInvalidSegmentsInPlace(segments);
		segments = CombineAdjacentPlainTextSegments(segments);

		// Text-only message
		if (IsPlainTextOnly(segments)) {
			return (SegmentedMessageToPlainText(segments), false);
		}

		// Message with contexts
		return (SegmentedMessageToSsml(segments), true);
	}

}
