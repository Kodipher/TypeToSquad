

namespace TypeToSquad.Model.Markup;


public abstract record class MessageSegment {

	/// <summary>Start position of this segment in the source string.</summary>
	/// <remarks>
	/// When performing text replacements, the source string can be <see cref="Text"/>
	/// of segment where the replacement took place.
	/// </remarks>
	public int Start { get; protected set; } = -1;

	/// <summary>First position beyond this segment in the source string.</summary>
	/// <remarks>
	/// When performing text replacements, the source string can be <see cref="Text"/>
	/// of segment where the replacement took place.
	/// </remarks>
	public int EndExclusive { get; protected set; } = -1;

	/// <summary>Inner text of the segment</summary>
	public string Text { get; protected set; } = "";

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


public record class PlainTextSegment : MessageSegment {

	public static PlainTextSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return CreateBaseAsSubstring<PlainTextSegment>(start, endExclusive, str);
	}

	public static PlainTextSegment CreateFromText(string text) {
		return new PlainTextSegment() { Text = text };
	}

}


public record class InvalidSegment : MessageSegment {

	public static InvalidSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return MessageSegment.CreateBaseAsSubstring<InvalidSegment>(start, endExclusive, str);
	}

}


public record class HintSegment : MessageSegment {

	public HintType HintType { get; private set; } = HintType.Unset;

	/// <summary>The processed (trimmed) text of the hint.</summary>
	public string HintText { get; private set; } = "";

	public static HintSegment CreateAsSubstring(int start, int endExclusive, string str) {
		var ret = MessageSegment.CreateBaseAsSubstring<HintSegment>(start, endExclusive, str);
		ret.HintText = str[(start + 1)..(endExclusive - 1)].Trim();
		return ret;
	}

	public static HintSegment CreateWithType(HintSegment other, HintType contextType) {
		return other with { HintType = contextType };
	}

}


public record class ContentSegment : MessageSegment {

	/// <summary>The first position that separates hint and payload.</summary>
	public int HintEndExclusive { get; private set; } = -1;

	/// <summary>The processed (trimmed) text of the hint (content type).</summary>
	public string HintText { get; private set; } = "";
	public ContentType ContentType { get; private set; } = ContentType.Invalid;

	public string Payload { get; private set; } = "";

	public static ContentSegment CreateAsSubstring(int start, int hintEndExclusive, int endExclusive, string str) {
		var ret = MessageSegment.CreateBaseAsSubstring<ContentSegment>(start, endExclusive, str);
		ret.HintEndExclusive = hintEndExclusive;
		ret.HintText = str[(start + 1)..hintEndExclusive].Trim().ToLower();
		ret.Payload = str[(hintEndExclusive + 1)..(endExclusive - 1)];
		return ret;
	}

	public static ContentSegment CreateWithType(ContentSegment other, ContentType contextType) {
		return other with { ContentType = contextType };
	}

}
