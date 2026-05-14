using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace TypeToSquad.Model.Markup;


/// <summary>
/// Implements splitting the message into segments,
/// where each segment is either plain text or a tag.
/// </summary>
public static class MessageLexer {

	public const char TagOpen = '[';
	public const char TagClose = ']';
	public const string TagOpenAsString = "[";
	public const string TagCloseAsString = "]";
	
	/// <summary>Returns a list of segments that make up the message.</summary>
	/// <remarks>The segments' <see cref="MessageSegment.Text"/>s add up perfectly to the initial string.</remarks>
	public static List<MessageSegment> SegmentMessage(string message) {

		List<MessageSegment> segments = new();

		int currentSegmentStartI = 0;

		for (int i = 0; i < message.Length; i++) {

			// Scan until tag opening is found
			if (message[i] != TagOpen) continue;

			// Add text before tag (if there is any between tags)
			if (i != currentSegmentStartI) {
				segments.Add(MessageSegment.MakePlain(message[currentSegmentStartI..i]));
				currentSegmentStartI = i;
			}

			// Find closing tag (assuming nesting)
			int additionalDepth = 0;
			bool hasNested = false;

			i++; // "consume" tag opening
			for (/*[nop]*/; i < message.Length; i++) {

				if (message[i] == TagOpen) {
					additionalDepth++;
					hasNested = true;
					continue;
				}

				if (message[i] == TagClose) {
					if (additionalDepth == 0) break;
					additionalDepth--;
					continue;
				}

				/*[continue]*/
			}
			
			// here: `i` is at ']' or == message.Length

			// Unclosed tag
			if (i >= message.Length) {
				segments.Add(MessageSegment.MakeInvalid(message[currentSegmentStartI..]));
				currentSegmentStartI = message.Length;
				break;
			}

			// Closed tag but has nesting
			if (hasNested) {
				segments.Add(MessageSegment.MakeInvalid(message[currentSegmentStartI..(i + 1)]));
				currentSegmentStartI = i + 1;
				continue;
			}
			
			// Valid tag
			segments.Add(MessageSegment.MakeTag(message[currentSegmentStartI..(i + 1)]));
			currentSegmentStartI = i + 1;

			// [continue]
		}

		// Add till the end
		if (currentSegmentStartI < message.Length) {
			segments.Add(MessageSegment.MakePlain(message[currentSegmentStartI..]));
		}

		return segments;
	}

	public static (string type, string argument) ParseTag(string tagWithBrackets, out int? argumentStartIndex) {
		
		int typeStartIndex = -1;
		int typeExclusiveEndIndex = -1;

		for (int i = 1; i < tagWithBrackets.Length - 1; i++) {
			if (char.IsWhiteSpace(tagWithBrackets[i])) continue;
			typeStartIndex = i;
			break;
		}

		if (typeStartIndex < 0) {
			// Empty tag
			argumentStartIndex = null;
			return ("", "");
		}
		
		for (int i = typeStartIndex; i < tagWithBrackets.Length - 1; i++) {
			if (!char.IsWhiteSpace(tagWithBrackets[i])) continue;
			typeExclusiveEndIndex = i;
			break;
		}
		
		if (typeExclusiveEndIndex < 0) {
			// Empty argument
			argumentStartIndex = null;
			return (tagWithBrackets[typeStartIndex..(tagWithBrackets.Length - 1)], "");
		}

		argumentStartIndex = typeExclusiveEndIndex + 1;

		if (argumentStartIndex >= tagWithBrackets.Length - 1) {
			// Empty argument (but with space separator)
			argumentStartIndex = null;
			return (tagWithBrackets[typeStartIndex..typeExclusiveEndIndex], "");
		}
		
		return (
				tagWithBrackets[typeStartIndex..typeExclusiveEndIndex],
				tagWithBrackets[(typeExclusiveEndIndex + 1)..(tagWithBrackets.Length - 1)]
			);
	}

	#region /--- Tag Types ---/

	public const string TagTypeEmpty = "";
	public const string TagTypePhonetic = "ipa";
	public const string TagTypeVoice = "voice";
	public const string TagTypeAudio = "audio";
	public const string TagTypeBreak = "break";
	public const string TagTypeBreakAlt = "wait";

	public static readonly ReadOnlyCollection<string> BuildInTagTypes = new[] {
																			TagTypeEmpty,
																			TagTypePhonetic,
																			TagTypeVoice,
																			TagTypeAudio,
																			TagTypeBreak,
																			TagTypeBreakAlt
																		}.AsReadOnly();

	public static IEnumerable<string> GetUserTags() {
		var settingsInstance = UserSettingsManager.Instance.Settings;
		return settingsInstance.UserTags.Select(row => row.type).Distinct();
	}
	
	public static bool IsTagTypeValid(string tagType) {
		
		if (BuildInTagTypes.Contains(tagType)) return true;
		if (GetUserTags().Contains(tagType)) return true;
		
		return false;
	}

	public static bool IsTagRunningChange(string tagType) {
		// As a function for potential future use
		// (allows expanding the concept)
		return tagType == TagTypeVoice || tagType == TagTypeEmpty;
	}
	
	#endregion
	
}
