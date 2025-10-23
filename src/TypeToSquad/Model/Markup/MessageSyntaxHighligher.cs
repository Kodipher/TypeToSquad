using Godot;
using System;
using System.Collections.Generic;
using GodotDictionary = Godot.Collections.Dictionary;

using TypeToSquad.Utils;


namespace TypeToSquad.Model.Markup;


public partial class MessageSyntaxHighligher : Godot.SyntaxHighlighter, IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	#region //// Segment cache

	string? cacheKey = null;
	List<MessageSegment>? segmentsCache = null;

	void PopulateSegmentCache(string message) {
		if (cacheKey == message) return;
		cacheKey = message;
		segmentsCache = CoreNode.MessageProsessor.Lexer.SegmentMessage(message);
	}

	public override void _ClearHighlightingCache() {
		cacheKey = null;
		segmentsCache = null;
	}

	#endregion

	#region //// Colors

	readonly static Color colorDefault = new(1, 1, 1);
	readonly static Color colorInvalid = new(colorDefault, 0.5f);

	readonly static Color colorContextReplacement = new(1, 1, 0);
	readonly static Color colorContextNone = new(colorContextReplacement, 0.5f);
	readonly static Color colorContextVoice = new(0, 1, 1);
	readonly static Color colorContextEmpty = new(0.5f, 0.7f, 1);

	readonly static Color colorContent = new(1, 0, 1);
	readonly static Color colorContentPayload = new(1, 0.5f, 1);

	#endregion

	public override GodotDictionary _GetLineSyntaxHighlighting(int line) {

		// Find source and bounds
		TextEdit source = this.GetTextEdit();
		int curLineStart = source.GetLineStartIndex(line);
		int curLineEndExclusive = curLineStart + source.GetLine(line).Length;

		// Segment the text, process colors
		PopulateSegmentCache(source.Text);
		if (segmentsCache is null) {
			GD.PushError("Failed to populate highlighter cache.");
			this.ClearHighlightingCache();
			return new();
		}

		// Enforce color format
		GodotDictionary colors = new();

		void AddColorChange(int atMessageIndex, Color color) {
			colors[Math.Max(curLineStart, atMessageIndex) - curLineStart] = new GodotDictionary() { ["color"] = color };
		}

		// Find colors
		for (int segmentI = 0; segmentI < segmentsCache.Count; segmentI++) {
			MessageSegment currentSegment = segmentsCache[segmentI];

			// Bounds check
			if (curLineStart >= currentSegment.EndExclusive) continue;
			if (curLineEndExclusive <= currentSegment.Start) break;

			// Add color change for misc
			if (currentSegment is PlainTextSegment) {
				AddColorChange(currentSegment.Start, colorDefault);

			} else if (currentSegment is InvalidSegment) {
				AddColorChange(currentSegment.Start, colorInvalid);

			// Add color change for contexts
			} else if (currentSegment is ContextSegment contextSegment) {

				if (contextSegment.ContextUses == ContextUses.Empty) {
					AddColorChange(currentSegment.Start, colorContextEmpty);

				} else if (contextSegment.ContextUses.HasFlag(ContextUses.VoiceChange)) {
					AddColorChange(currentSegment.Start, colorContextVoice);

				} else if (contextSegment.ContextUses.HasFlag(ContextUses.Replacements)) {
					AddColorChange(currentSegment.Start, colorContextReplacement);

				} else {
					AddColorChange(currentSegment.Start, colorContextNone);
				}

			// Add color change for content
			} else if (currentSegment is ContentSegment contentSegment) {
				if (contentSegment.Type == ContentType.Invalid) {
					AddColorChange(currentSegment.Start, colorInvalid);
				} else {
					AddColorChange(contentSegment.Start, colorContent);
					AddColorChange(contentSegment.TypeTextEndExclusive, colorContentPayload);
					AddColorChange(contentSegment.EndExclusive - 1, colorContent);
				}

			} else {
				AddColorChange(currentSegment.Start, colorInvalid);
				GD.PushError("Unknown segment in syntax highlighter.");
			}

		}
		return colors;
	}

}