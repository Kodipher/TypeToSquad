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
public static class MessageProcessor {
	
	/// <summary>Returns a new list of segments where adjacent plain text segments are joined into one.</summary>
	public static List<MessageSegment> CombineAdjacentPlainTextSegments(List<MessageSegment> segments) {

		List<MessageSegment> newSegments = new();

		foreach (MessageSegment seg in segments) {
			
			// Add non-plain-text
			if (!seg.IsPlainText) {
				newSegments.Add(seg);
				continue;
			}

			// Add plain-text after non-plain-text
			if (newSegments.Count == 0 || !newSegments[^1].IsPlainText) {
				newSegments.Add(seg);
				continue;
			}

			// Join text segments
			newSegments[^1] = MessageSegment.MakePlain(newSegments[^1].Text + seg.Text);
		}

		return newSegments;
	}
	
	static string StringJoinValidSegments(IEnumerable<MessageSegment> segments) {
		StringBuilder sb = new StringBuilder();
		foreach (var seg in segments) {
			if (seg.IsValid) sb.Append(seg.Text);
		}
		return sb.ToString();
	}
	
	#region /--- Text replacements, User Tags ---/
	
	/// <summary>Performs a single pass of text replacements on a string.</summary>
	/// <remarks>
	/// The new string may contain tags.
	/// When it does, the replacement will also be interrupted.
	/// </remarks>
	static string PerformReplacementsOnString(string text) {

		var settingsInstance = UserSettingsManager.Instance.Settings;

		string newText = text;
		
		foreach ((string pattern, string replacement) in settingsInstance.TextReplacements) {

			// Empty pattern
			if (string.IsNullOrEmpty(pattern)) continue;

			// Try replace
			Regex patternRegex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
			newText = patternRegex.Replace(newText, replacement);

			// Stop if introduced tags to parse tags
			bool hasReplaced = text != newText;
			if (hasReplaced) {
				bool newHasTags = newText.Contains(MessageLexer.TagOpen) || newText.Contains(MessageLexer.TagClose);
				if (newHasTags) break;
			}

		}
		
		return newText;
	}

	/// <summary>Creates a new list of segments by performing 1 pass of text replacements in current segments.</summary>
	static List<MessageSegment> PerformReplacementPass(IEnumerable<MessageSegment> segments, out bool anyTextReplaced) {
		
		List<MessageSegment> newSegments = new();
		anyTextReplaced = false;

		foreach (MessageSegment seg in segments) {

			// Add everything but text directly
			if (!seg.IsPlainText) {
				newSegments.Add(seg);
				continue;
			}

			// Perform replacements in text
			string newText = PerformReplacementsOnString(seg.Text);

			if (newText != seg.Text) {
				anyTextReplaced = true;
				newSegments.AddRange(MessageLexer.SegmentMessage(newText)); // with replacements
				continue;
			}

			newSegments.Add(seg); // No replacements
		}

		return newSegments;
	}
	
	/// <summary>Performs replacement rules on the tag argument</summary>
	/// <remarks>Does not return early when a tag is added, unlike <see cref="PerformReplacementsOnString"/>.</remarks>
	/// <returns>The processed tag argument.</returns>
	static string PerformTagRulesOnString(string tagType, string tagArgument) {
		
		var settingsInstance = UserSettingsManager.Instance.Settings;

		string processedArg = tagArgument;
		
		foreach ((string type, string pattern, string replacement) in  settingsInstance.UserTags) {
			
			// Skip irrelevant
			if (type != tagType) continue;
		
			// Replace
			Regex patternRegex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
			processedArg = patternRegex.Replace(processedArg, replacement);
		}

		return processedArg;
	}
	
	/// <summary>Returns a new list of segments where user tags have been handled.</summary>
	static List<MessageSegment> PerformUserTagsPass(List<MessageSegment> segments, out bool anyFound) {
		
		List<MessageSegment> newSegments = new();
		anyFound = false;

		foreach (MessageSegment seg in segments) {

			// Add non-tags
			if (!seg.IsTag || !seg.IsValid) {
				newSegments.Add(seg);
				continue;
			}

			// Add build-in
			if (MessageLexer.BuildInTagTypes.Contains(seg.TagType)) {
				newSegments.Add(seg);
				continue;
			}
			
			// Handle user tag
			string processedContent = PerformTagRulesOnString(seg.TagType, seg.TagArgument);
			newSegments.AddRange(MessageLexer.SegmentMessage(processedContent));
			anyFound = true;
		}

		return newSegments;
	}
	
	#endregion
	
	#region /--- Compile SSML ---/
	
	const string SsmlHeaderFormat = """
<speak version="1.0"
	xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{0}">
""";

	const string SsmlFooter = "</speak>";

	const string SsmlVoiceOpen = """<voice name="{0}" xml:lang="{1}">""";
	const string SsmlVoiceClose = "</voice>";

	const string SsmlIpa = """<phoneme alphabet="ipa" ph="{0}"></phoneme>""";
	const string SsmlBreak = """<break time="{0}"/>""";
	const string SsmlAudio = """<audio src="{0}"/>""";
	
	/// <remarks>User tags must be handled before this method is called.</remarks>
	static string SegmentedMessageToSsml(IEnumerable<MessageSegment> segments) {
		
		// Shortcuts
		var settingsInstance = UserSettingsManager.Instance.Settings;
		var voiceStorage = DaemonVoiceStorage.Instance;
		
		
		StringBuilder sb = new StringBuilder();
		
		// Header
		string defaultVoiceLang = voiceStorage.GetVoiceByKey(settingsInstance.VoiceKey).Language;
		sb.AppendFormat(SsmlHeaderFormat, SecurityElement.Escape(defaultVoiceLang));
		
		// Content
		bool isInsideVoice = false;

		foreach (var seg in segments) {

			if (!seg.IsValid) continue;

			if (seg.IsTag) {

				switch (seg.TagType) {
					
					case MessageLexer.TagTypeEmpty:
						if (isInsideVoice) {
							sb.Append(SsmlVoiceClose);
							isInsideVoice = false;
						}
						break;
					
					case MessageLexer.TagTypePhonetic:
						sb.AppendFormat(SsmlIpa, SecurityElement.Escape(seg.TagArgument));
						break;

					case MessageLexer.TagTypeVoice: {
						
						if (isInsideVoice) {
							sb.Append(SsmlVoiceClose);
							isInsideVoice = false;
						}

						if (seg.TagArgument == "") break;
						
						string? voiceKey = settingsInstance
											.VoiceChanges
											.Where(row => row.hint == seg.TagArgument)
											.Select(string? (row) => row.voiceKey)
											.FirstOrDefault();

						if (voiceKey is null) break;

						var voiceInfo = voiceStorage.GetVoiceByKey(voiceKey);
						string voiceName = SecurityElement.Escape(voiceInfo.Name);
						string voiceLang = SecurityElement.Escape(voiceInfo.Language);

						sb.AppendFormat(SsmlVoiceOpen, voiceName, voiceLang);
						isInsideVoice = true;
					} break;
					
					case MessageLexer.TagTypeAudio:
						
						if (seg.TagArgument == "") break;
						
						string? path = settingsInstance
										.SoundEffects
										.Where(row => row.hint == seg.TagArgument)
										.Select(string? (row) => row.path)
										.FirstOrDefault();
						
						if (path is null) break;
						
						sb.AppendFormat(SsmlAudio, SecurityElement.Escape(path));
						break;
					
					case MessageLexer.TagTypeBreak:
					case MessageLexer.TagTypeBreakAlt:
						sb.AppendFormat(SsmlBreak, SecurityElement.Escape(seg.TagArgument));
						break;
					
					default:
						throw new InvalidOperationException("Unhandled tag found.");
				}

				continue;
			}
			
			// Plain text
			string segText = seg.Text;
			segText = SecurityElement.Escape(segText);
			sb.Append(segText);

			// [continue]
		}

		if (isInsideVoice) sb.Append(SsmlVoiceClose);
		
		// Footer
		sb.Append(SsmlFooter);

		return sb.ToString();
	}

	#endregion

	/// <summary>
	/// Processes the message, performing analysis and text replacements.
	/// Returns a plain text or a message in SSML format.
	/// </summary>
	public static (string requestString, bool isSsml) ProcessMessage(string message) {

		var segments = MessageLexer.SegmentMessage(message);
		
		// User tags and Text replacements
		for (int i = 0, n = UserSettingsManager.Instance.Settings.MaxReplacementPasses; i < n; i++) {
			segments = PerformUserTagsPass(segments, out bool anyFound);
			segments = PerformReplacementPass(segments, out bool anyReplaced);
			//segments = CombineAdjacentPlainTextSegments(segments); // keep locality of user tags
			if (!anyReplaced && !anyFound) break;

			if (i == n - 1) GD.PushError("Text replacement passes limit reached.");
		}

		// Text-only message
		if (segments.All(seg => !seg.IsValid || seg.IsPlainText)) {
			return (StringJoinValidSegments(segments), false);
		}

		// Message with contexts
		return (SegmentedMessageToSsml(segments), true);
	}

}
