using Godot;
using System;
using System.Collections.Generic;
using GodotDictionary = Godot.Collections.Dictionary;

using TypeToSquad.Utils;


namespace TypeToSquad.Model.Markup;


[GlobalClass]
public partial class MessageSyntaxHighlighter : Godot.SyntaxHighlighter {

	#region /--- Segment cache ---/

	string? cacheKey = null;
	List<MessageSegment>? segmentsCache = null;

	void PopulateSegmentCache(string message) {
		if (cacheKey == message) return;
		cacheKey = message;
		segmentsCache = MessageLexer.SegmentMessage(message);
	}

	public override void _ClearHighlightingCache() {
		cacheKey = null;
		segmentsCache = null;
	}

	#endregion

	#region /--- Colors ---/

	static readonly Color colorDefault = new(1, 1, 1);
	static readonly Color colorInvalid = new(colorDefault, 0.5f);

	static readonly Color colorTag = new(1, 0, 1);
	static readonly Color colorTagArgument = new(1, 0.5f, 1);
	static readonly Color colorTagRunningChange = new(0, 1, 1);
	static readonly Color colorTagRunningChangeArgument = new(0.5f, 1, 1);
	
	static readonly Color colorTagInvalid = new(colorTag, 0.5f);
	
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
		int nextSegmentStartI = 0;
		for (int segmentI = 0; segmentI < segmentsCache.Count; segmentI++) {
			
			MessageSegment currentSegment = segmentsCache[segmentI];

			int currentSegmentStartI = nextSegmentStartI;
			int currentSegmentEndExclusiveI = nextSegmentStartI + currentSegment.Text.Length;

			nextSegmentStartI = currentSegmentEndExclusiveI; // for next loop iteration
			
			// Bounds check
			if (curLineStart >= currentSegmentEndExclusiveI) continue;
			if (curLineEndExclusive <= currentSegmentStartI) break;

			// Add colors
			if (currentSegment.IsPlainText) {
				AddColorChange(currentSegmentStartI, colorDefault);

			} else if (!currentSegment.IsValid) {
				AddColorChange(currentSegmentStartI, colorInvalid);
				
			} else if (!MessageLexer.IsTagTypeValid(currentSegment.TagType)) {
				AddColorChange(currentSegmentStartI, colorTagInvalid);

			} else {

				bool isRunningChange = MessageLexer.IsTagRunningChange(currentSegment.TagType);

				Color tagColor = isRunningChange ? colorTagRunningChange : colorTag;
				Color argumentColor = isRunningChange ? colorTagRunningChangeArgument : colorTagArgument;

				_ = MessageLexer.ParseTag(currentSegment.Text, out int? argumentStartIndexInSeg);

				if (argumentStartIndexInSeg.HasValue) {
					AddColorChange(currentSegmentStartI, tagColor); // [
					AddColorChange(currentSegmentStartI + argumentStartIndexInSeg.Value, argumentColor);
					AddColorChange(currentSegmentEndExclusiveI - 1, tagColor); // ]
				} else {
					AddColorChange(currentSegmentStartI, tagColor); // [
				}
				
			}

		}
		return colors;
	}

}