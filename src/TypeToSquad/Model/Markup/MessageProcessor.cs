using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

using System.Text;
using System.Text.RegularExpressions;

using VoiceInfo = WinRTSpeechSynthServer.Protocol.VoiceInfo;


namespace TypeToSquad.Model.Markup;


/// <summary>
/// Processes messages and allows use
/// of some SSML features through markup.
/// See docs folder for details.
/// </summary>
public static class MessageProcessor {
	
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
			newSegments[^1] = MessageLexer.MakePlainSegment(newSegments[^1].Text + seg.Text);
		}

		return newSegments;
	}
	
	#endregion
	
	#region /--- Compiling Render Nodes ---/
	
	static RenderNode SegmentsToInitialTree(IEnumerable<MessageSegment> segments) {
		
		// Shortcuts
		var settingsInstance = UserSettingsManager.Instance.Settings;
		var voiceStorage = DaemonVoiceStorage.Instance;
		
		// Create tree
		Stack<RenderNode> nodeStack = new Stack<RenderNode>();
		
		RenderNode root = CreateSsmlRoot(voiceStorage.GetVoiceByKey(settingsInstance.VoiceKey));
		nodeStack.Push(root);

		foreach (var seg in segments) {

			if (!seg.IsValid) continue;

			if (seg.IsPlainText) {
				AppendChildAtCurrent(CreateTextNode(seg.Text));
				continue;
			}

			switch (seg.TagType) {

				case MessageLexer.TagTypeEmpty: {

					RenderNode[] orderedParents = nodeStack.ToArray();

					// Find voice
					int topVoiceIndex = -1;

					for (int i = 0; i < orderedParents.Length; i++) {
						if (orderedParents[i].Type == RenderNodeType.Voice) {
							topVoiceIndex = i;
							break;
						}
					}

					// Not in a voice tag: do nothing
					if (topVoiceIndex == -1) break;

					// Pull out tags until including voice
					// then push other changes back
					for (int i = 0; i <= topVoiceIndex; i++) {
						nodeStack.Pop();
					}
					for (int i = topVoiceIndex - 1; i >= 0; i--) {
						AppendChildAtCurrent(orderedParents[i].ShallowClone());
					}

				} break;

				case MessageLexer.TagTypeIpa:
					nodeStack.Peek().Children.Add(CreateIpaNode(seg.TagArgument));
					break;

				case MessageLexer.TagTypeVoice: {
					
					RenderNode[] orderedParents = nodeStack.ToArray();
					
					// Find voice
					int topVoiceIndex = -1;
					for (int i = 0; i < orderedParents.Length; i++) {
						if (orderedParents[i].Type == RenderNodeType.Voice) {
							topVoiceIndex = i;
							break;
						}
					}

					// Pull out until including voice
					if (topVoiceIndex != -1) {
						for (int i = 0; i <= topVoiceIndex; i++) {
							nodeStack.Pop();
						}
					}

					// Push new voice
					if (seg.TagArgument != "") {
						string? voiceKey = settingsInstance
							.VoiceChanges
							.Where(row => row.hint == seg.TagArgument)
							.Select(row => row.voiceKey)
							.FirstOrDefault();

						if (voiceKey is not null) {
							var voiceInfo = voiceStorage.GetVoiceByKey(voiceKey);
							var voiceNode = CreateVoiceNode(voiceInfo);
							AppendChildAtCurrent(voiceNode);
							nodeStack.Push(voiceNode);
						}
					}
					
					// Push other changes
					if (topVoiceIndex != -1) {
						for (int i = topVoiceIndex - 1; i >= 0; i--) {
							AppendChildAtCurrent(orderedParents[i].ShallowClone());
						}
					}
					
				} break;
					
				case MessageLexer.TagTypeBreak:
				case MessageLexer.TagTypeBreakAlt:
					AppendChildAtCurrent(CreateBreakNode(seg.TagArgument.Trim() == "" ? null : seg.TagArgument.Trim()));
					break;
				
				case MessageLexer.TagTypeAudio:
				case MessageLexer.TagTypeAudioAlt:
					AppendChildAtCurrent(CreateSoundNode(seg.TagArgument));
					break;
					
				default:
					throw new InvalidOperationException($"Unhandled tag of type \"{seg.TagType}\" found.");
			}
			
			// [continue]
		}

		return root;
		
		// Local helpers
		void AppendChildAtCurrent(RenderNode node) {
			nodeStack.Peek().Children.Add(node);
		}
		
	}
	
	static RenderNode CreateSsmlRoot(VoiceInfo defaultVoice) {
		return new RenderNode() {
			Type = RenderNodeType.SsmlRoot,
			Attributes = {
				{ RenderNodeAttribute.SsmlRootVersion, "1.0" },
				{ RenderNodeAttribute.SsmlXmlNamespace, "http://www.w3.org/2001/10/synthesis" },
				{ RenderNodeAttribute.SsmlLanguage, SecurityElement.Escape(defaultVoice.Language) },
			}
		};
	}
	
	static RenderNode CreateTextNode(string text) {
		return new RenderNode() {
			Type = RenderNodeType.Text,
			Attributes = { { RenderNodeAttribute.TextContent, SecurityElement.Escape(text) } }
		};
	}
	
	static RenderNode CreateVoiceNode(VoiceInfo voiceInfo) {
		return new RenderNode() {
			Type = RenderNodeType.Voice,
			Attributes = {
				{ RenderNodeAttribute.VoiceName, SecurityElement.Escape(voiceInfo.Name) },
				{ RenderNodeAttribute.VoiceLanguage, SecurityElement.Escape(voiceInfo.Language) },
			}
		};
	}
	
	static RenderNode CreateIpaNode(string phonemes) {
		return new RenderNode() {
			Type = RenderNodeType.Phoneme,
			Attributes = {
				{ RenderNodeAttribute.PhonemeAlphabet, "ipa" },
				{ RenderNodeAttribute.PhonemePhonemes, SecurityElement.Escape(phonemes) },
			}
		};
	}
	
	static RenderNode CreateBreakNode(string? time) {
		var node = new RenderNode() { Type = RenderNodeType.Break };
		if (time is not null) {
			node.Attributes.Add(RenderNodeAttribute.BreakTime, SecurityElement.Escape(time));
		}
		return node;
	}
	
	static RenderNode CreateSoundNode(string hint) {
		return new RenderNode() {
			Type = RenderNodeType.Sound,
			Attributes = { { RenderNodeAttribute.SoundHint, hint } }
		};
	}

	static RenderNode ProcessInitialNodeTree(RenderNode root) {
		// TODO
		return root;
	}
	
	#endregion

	/// <summary>Processes the message, performing analysis and text replacements.</summary>
	public static RenderNode ProcessMessage(string message) {

		var segments = MessageLexer.SegmentMessage(message);
		
		// User tags and Text replacements
		for (int i = 0, n = UserSettingsManager.Instance.Settings.MaxReplacementPasses; i < n; i++) {
			segments = PerformUserTagsPass(segments, out bool anyFound);
			segments = PerformReplacementPass(segments, out bool anyReplaced);
			//segments = CombineAdjacentPlainTextSegments(segments); // keep locality of user tags
			if (!anyReplaced && !anyFound) break;

			if (i == n - 1) GD.PushError("Text replacement passes limit reached.");
		}

		// Compile
		var tree = SegmentsToInitialTree(segments);
		tree = ProcessInitialNodeTree(tree);
		
		return tree;
	}
	
	/// <remarks>Text nodes are appended as text, every other node - as a dom element.</remarks>
	public static string StringifyNodeRecursive(RenderNode root, bool indented = false) {
		
		StringBuilder sb = new();
		
		void AppendRecursiveHelper(RenderNode node, int indentLevel) {
			
			string indentString = indented ? new string(' ', indentLevel * 4) : "";

			// Handle text nodes as text, not elements
			if (node.Type == RenderNodeType.Text) {
				if (indented) sb.Append(indentString);
				sb.Append(node.Attributes[RenderNodeAttribute.TextContent]);
				if (indented) sb.Append('\n');
				return;
			}
			
			sb.AppendJoin("", [indentString, "<", node.Type]);
			foreach (var pair in node.Attributes) {
				sb.AppendJoin<string>("", [" ", pair.Key, "=\"", pair.Value, "\""]);
			}
			sb.Append('>');
			if (indented) sb.Append('\n');

			foreach (var child in node.Children) {
				AppendRecursiveHelper(child, indentLevel + 1);
			}

			sb.AppendJoin("", [indentString, "</", node.Type, ">"]);
			if (indented) sb.Append('\n');
		}
		
		AppendRecursiveHelper(root, 0);
		return sb.ToString();
	}

}
