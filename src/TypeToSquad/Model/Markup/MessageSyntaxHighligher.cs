using Godot;
using System;
using System.Collections.Generic;

using TypeToSquad.Utils;
using GodotDictionary = Godot.Collections.Dictionary;


namespace TypeToSquad.Model.Markup;


public partial class MessageSyntaxHighligher : Godot.SyntaxHighlighter {
	
	/*

	#region //// Cache

	string? cacheText = null;
	List<DepthSegment>? segmentsCache = null;

	void PopulateSegmentCache(string message) {
		if (cacheText == message) return;
		cacheText = message;
		segmentsCache = MessageParser.SegmentMessage(message);
		segmentColors = null;
	}
	public override void _ClearHighlightingCache() {
		cacheText = null;
		segmentsCache = null;
		segmentColors = null;
	}

	#endregion

	#region //// Colors

	readonly static Color colorDefault = new(1, 1, 1);
	readonly static Color colorSkipped = new(colorDefault, 0.5f);
	readonly static Color colorIpa = new(0.5f, 1, 1);

	// Test
	readonly static Color colorLanguage = new(1, 1, 0.5f);
	const string buildinHintIpa = "ipa";

	Color[]? segmentColors = null;

	void PopulateColors() {

		if (segmentsCache is null || cacheText is null) {
			GD.PushError("Failed to populate colors. Segment cache is empty.");
			return;
		}

		segmentColors = new Color[segmentsCache.Count];
		List<string?> checkedContextHintStack = new();

		for (int i = 0; i < segmentsCache.Count; i++) {

			// Context enter
			if (segmentsCache[i] is ContextStartSegment conextStart) {
				string hint = cacheText[conextStart.HintStart..conextStart.HintEndExclusive];
				if (hint.Equals(buildinHintIpa, StringComparison.InvariantCultureIgnoreCase)) {
					checkedContextHintStack.Add(buildinHintIpa);
				} else if (hint.Equals("de", StringComparison.InvariantCultureIgnoreCase)) {
					checkedContextHintStack.Add(hint);
				} else {
					checkedContextHintStack.Add(null);
				}
			}

			// Check current context
			if (checkedContextHintStack.Count == 0) {
				segmentColors[i] = colorDefault;
				continue;
			}

			int topMostInvalid = checkedContextHintStack.IndexOf(null);
			int topMostIpa = checkedContextHintStack.IndexOf(buildinHintIpa);
			string? currentHint = checkedContextHintStack[^1];

			if (topMostInvalid != -1) {
				segmentColors[i] = colorSkipped;
			} else if (topMostIpa == checkedContextHintStack.Count - 1) {
				segmentColors[i] = colorIpa;
			} else if (topMostIpa != -1) {
				segmentColors[i] = colorSkipped;
			} else if (currentHint == "de") {
				segmentColors[i] = colorLanguage;
			}


			// Context exit
			if (segmentsCache[i] is ContextEnd or ContextFullSegment) {
				checkedContextHintStack.RemoveAt(checkedContextHintStack.Count - 1);
			}

		}
	}

	#endregion

	public override GodotDictionary _GetLineSyntaxHighlighting(int line) {

		TextEdit source = this.GetTextEdit();

		// Segment the text, process colors
		PopulateSegmentCache(source.Text);
		PopulateColors();
		if (segmentsCache is null || segmentColors is null) {
			GD.PushError("Failed to populate highlighter or color cache.");
			this.ClearHighlightingCache();
			return new();
		}

		// Find start index
		int startCharI = source.GetLineStartIndex(line);
		int lineLength = source.GetLine(line).Length;

		// Relay colors in the correct format
		GodotDictionary colors = new();

		void AddColorChange(int col, Color color) {
			colors[col] = new GodotDictionary() { ["color"] = color };
		}

		for (int segmentI = 0; segmentI < segmentsCache.Count; segmentI++) {
			var currentSegment = segmentsCache[segmentI];

			// Bounds check
			if (startCharI >= currentSegment.EndExclusive) continue;
			if (startCharI + lineLength <= currentSegment.Start) break;

			AddColorChange(
				Math.Max(startCharI, currentSegment.Start) - startCharI,
				segmentColors[segmentI]
			);
		}

		return colors;
	}
	*/
}