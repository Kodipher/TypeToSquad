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
		segmentsCache = CoreNode.MessageParser.SegmentMessage(message);
	}

	public override void _ClearHighlightingCache() {
		cacheKey = null;
		segmentsCache = null;
	}

	#endregion

	#region //// Colors

	readonly static Color colorDefault = new(1, 1, 1);
	readonly static Color colorInvalid = new(colorDefault, 0.5f);

	readonly static Color colorHintReplacement = new(1, 1, 0);
	readonly static Color colorHintUnknownReplacement = new(colorHintReplacement, 0.5f);
	readonly static Color colorHintLanguage = new(0, 1, 1);

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

			// Add color change
			if (currentSegment is PlainTextSegment) {
				AddColorChange(currentSegment.Start, colorDefault);

			} else if (currentSegment is InvalidSegment) {
				AddColorChange(currentSegment.Start, colorInvalid);

			} else if (currentSegment is HintSegment hintSegment) {
				switch (hintSegment.HintType) {
					case HintType.UnknownReplacementContext:
						AddColorChange(currentSegment.Start, colorHintUnknownReplacement);
						break;
					case HintType.ReplacementContext:
						AddColorChange(currentSegment.Start, colorHintReplacement);
						break;
					case HintType.VoiceChange:
						AddColorChange(currentSegment.Start, colorHintLanguage);
						break;
					default:
						AddColorChange(currentSegment.Start, colorInvalid);
						break;
				}

			} else if (currentSegment is ContentSegment contentSegment) {
				if (contentSegment.ContentType == ContentType.Invalid) {
					AddColorChange(currentSegment.Start, colorInvalid);
				} else {
					AddColorChange(contentSegment.Start, colorContent);
					AddColorChange(contentSegment.HintEndExclusive, colorContentPayload);
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