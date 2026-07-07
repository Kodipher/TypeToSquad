using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

using System.Text;
using System.Text.RegularExpressions;

using Rephidock.GeneralUtilities.Collections;

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
	
	/// <summary>
	/// <para>
	/// Creates an initial node tree.
	/// </para>
	/// <para>
	/// The tree has an ssml "speak" node as root,
	/// though it is not ready to be sent to the synthesizer.
	/// </para>
	/// <para>
	/// Some nodes are not in the standard but rather are specific to this app,
	/// like <see cref="RenderNodeType.Sound"/> (which is different to the audio tag in the standard).
	/// </para>
	/// <para>
	/// Text nodes are also elements in this tree for simplicity.
	/// </para>
	/// </summary>
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
			Attributes = { { RenderNodeAttribute.TextContent, text } }
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

	/// <summary>
	/// Given an initial tree, returns of the following populated nodes:
	/// <see cref="RenderNodeType.Text"/> with no children,
	/// <see cref="RenderNodeType.SsmlRoot"/>,
	/// <see cref="RenderNodeType.Sound"/> with no children,
	/// <see cref="RenderNodeType.Serial"/> with children of the above types.
	/// </summary>
	static RenderNode ProcessInitialNodeTree(RenderNode root) {

		RenderNode serialRoot = new RenderNode() { Type = RenderNodeType.Serial };
		serialRoot.Children.Add(root);
		
		// 1: pull out all non-ssml tags out via dfs
		//
		// Pulling out once is changing
		// ```
		// <a4>
		//   <b1></b1>
		//   <b2></b2>
		//   <target></target>
		//   <b3></b3>
		// </a4>
		// <a5></a5>
		// ```
		// into
		// ```
		// <a4>
		//   <b1></b1>
		//   <b2></b2>
		// </a4>
		// <target></target>
		// <a4>
		//   <b3></b3>
		// </a4>
		// <a5></a5>
		// ```
		void DfsPullOutWalk(RenderNode node, RenderNode? parent, int indexInParent, out bool pullOutCurrent) {

			if (node.Type == RenderNodeType.Sound) {
				pullOutCurrent = true;
				return;
			}
			
			if (node.Type == RenderNodeType.Break && !node.Attributes.ContainsKey(RenderNodeAttribute.BreakTime)) {
				pullOutCurrent = true;
				return;
			}
			
			for (int i = 0; i < node.Children.Count; i++) { // children list *is* mutated
				
				DfsPullOutWalk(node.Children[i], node, i, out bool pullingOut);

				if (pullingOut && parent is not null) {
					var pullOutChild = node.Children[i];
					var followingChildren = node.Children[(i + 1)..];
					
					var nodeCopy = node.ShallowClone();
					node.Children.RemoveRange(i, node.Children.Count - i);
					nodeCopy.Children.AddRange(followingChildren);
					
					parent.Children.InsertRange(indexInParent + 1, [pullOutChild, nodeCopy]);
					
					if (i < node.Children.Count) {
						GD.PushError($"i < node.Children.Count assertion failed in {nameof(ProcessInitialNodeTree)}");
						break;
					}
				}
				
			}
			pullOutCurrent = false;
		}
		
		DfsPullOutWalk(serialRoot, null, -1, out _);
		
		// 2: remove [break]s (with no attribute)
		// They have been pulled out already and did what they were meant to do
		serialRoot.Children.RemoveAll(child => 
										child.Type == RenderNodeType.Break &&
										!child.Attributes.ContainsKey(RenderNodeAttribute.BreakTime)
									);
		
		// 3: If ssml only contains text remove ssml wrapper 
		for (int i = 0; i < serialRoot.Children.Count; i++) {
			RenderNode currentChild = serialRoot.Children[i];
			
			if (
				currentChild.Type == RenderNodeType.SsmlRoot &&
				currentChild.Children.All(node => node.Type == RenderNodeType.Text)
			) {

				RenderNode joinedTextNode =
					currentChild.Children.Count == 1
						? currentChild.Children[0]
						: CreateTextNode(
							currentChild
								.Children
								.Select(node => node.Attributes[RenderNodeAttribute.TextContent])
								.JoinString("")
						);
				
				serialRoot.Children.RemoveAt(i);
				serialRoot.Children.Insert(i, joinedTextNode);
			}
			
		}
		
		// 4: remove empty text elements
		serialRoot.Children.RemoveAll(child => 
			child.Type == RenderNodeType.Text &&
			string.IsNullOrWhiteSpace(child.Attributes.GetValueOrDefault(RenderNodeAttribute.TextContent, ""))
		);
		
		// 5: single node in serial root -- remove serial wrapper
		if (serialRoot.Children.Count == 0) {
			GD.PushError($"0 children at the end of {nameof(ProcessInitialNodeTree)}.");
			return CreateTextNode("");
		}
		
		if (serialRoot.Children.Count == 1) {
			return serialRoot.Children[0];
		}
		
		return serialRoot;
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
		
		void AppendRecursiveHelper(RenderNode node, int indentLevel, bool isInsideDom) {
			
			string indentString = indented ? new string(' ', indentLevel * 4) : "";

			// Handle text nodes as text, not elements
			if (node.Type == RenderNodeType.Text) {
				if (indented) sb.Append(indentString);

				string textContent = node.Attributes[RenderNodeAttribute.TextContent];
				if (isInsideDom) textContent = SecurityElement.Escape(textContent);
				sb.Append(textContent);
				
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
				bool isChildInDom = isInsideDom || root.Type == RenderNodeType.SsmlRoot;
				AppendRecursiveHelper(child, indentLevel + 1, isChildInDom);
			}

			sb.AppendJoin("", [indentString, "</", node.Type, ">"]);
			if (indented) sb.Append('\n');
		}
		
		AppendRecursiveHelper(root, 0, root.Type == RenderNodeType.SsmlRoot);
		return sb.ToString();
	}

}
