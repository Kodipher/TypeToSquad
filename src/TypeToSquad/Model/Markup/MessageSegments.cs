

namespace TypeToSquad.Model.Markup;


public abstract record MessageSegment {

	/// <summary>Start position of this segment in the source string.</summary>
	/// <remarks>
	/// When performing text replacements, the source string can be <see cref="Text"/>
	/// of segment where the replacement took place.
	/// </remarks>
	public int Start { get; protected init; } = -1;

	/// <summary>First position beyond this segment in the source string.</summary>
	/// <remarks>
	/// When performing text replacements, the source string can be <see cref="Text"/>
	/// of segment where the replacement took place.
	/// </remarks>
	public int EndExclusive { get; protected init; } = -1;

	/// <summary>Inner text of the segment</summary>
	public string Text { get; protected init; } = "";

	protected static T CreateBaseAsSubstring<T>(int start, int endExclusive, string str) 
	where T : MessageSegment, new()
	{
		return new T {
			Start = start,
			EndExclusive = endExclusive,
			Text = str[start..endExclusive],
		};
	}

}


public record PlainTextSegment : MessageSegment {

	public static PlainTextSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return CreateBaseAsSubstring<PlainTextSegment>(start, endExclusive, str);
	}

	public static PlainTextSegment CreateFromText(string text) {
		return new PlainTextSegment() {
			Start = 0,
			EndExclusive = text.Length,
			Text = text,
		};
	}

}


public record InvalidSegment : MessageSegment {

	public static InvalidSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return MessageSegment.CreateBaseAsSubstring<InvalidSegment>(start, endExclusive, str);
	}

}


public record ContextSegment : MessageSegment {

	/// <summary>Context of the segment (trimmed).</summary>
	public string Context { get; private set; } = "";

	public ContextUses ContextUses { get; private set; } = ContextUses.None;

	public static ContextSegment CreateAsSubstring(int start, int endExclusive, string str) {
		var ret = MessageSegment.CreateBaseAsSubstring<ContextSegment>(start, endExclusive, str);
		ret.Context = str[(start + 1)..(endExclusive - 1)].Trim();
		return ret;
	}

	public static ContextSegment CreateWithUses(ContextSegment other, ContextUses uses) {
		return other with { ContextUses = uses };
	}

}


public record ContentSegment : MessageSegment {

	/// <summary>The first position that separates hint and payload.</summary>
	public int TypeTextEndExclusive { get; private set; } = -1;

	/// <summary>The normalized (trimmed, lowercase) text of content type.</summary>
	public string TypeText { get; private set; } = "";

	/// <summary>Type of the content.</summary>
	public ContentType Type { get; private set; } = ContentType.Invalid;

	/// <summary>The content payload. Not automatically trimmed.</summary>
	public string Payload { get; private set; } = "";

	public static ContentSegment CreateAsSubstring(int start, int hintEndExclusive, int endExclusive, string str) {
		var ret = MessageSegment.CreateBaseAsSubstring<ContentSegment>(start, endExclusive, str);
		ret.TypeTextEndExclusive = hintEndExclusive;
		ret.TypeText = str[(start + 1)..hintEndExclusive].Trim().ToLower();
		ret.Payload = str[(hintEndExclusive + 1)..(endExclusive - 1)];
		return ret;
	}

	public static ContentSegment CreateWithType(ContentSegment other, ContentType contextType) {
		return other with { Type = contextType };
	}

}
