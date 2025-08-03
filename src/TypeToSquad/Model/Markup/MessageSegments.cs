

namespace TypeToSquad.Model.Markup;


public abstract record class MessageSegment {

	public int Start { get; protected set; } = -1;
	public int EndExclusive { get; protected set; } = -1;
	public string Text { get; protected set; } = "";

	protected static T CreateAsSubstring<T>(int start, int endExclusive, string str) 
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
		return MessageSegment.CreateAsSubstring<PlainTextSegment>(start, endExclusive, str);
	}

}


public record class InvalidSegment : MessageSegment {

	public static InvalidSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return MessageSegment.CreateAsSubstring<InvalidSegment>(start, endExclusive, str);
	}

}


public record class HintSegment : MessageSegment {

	public HintType HintType { get; private set; } = HintType.Unset;

	public string HintText { get; private set; } = "";

	public static HintSegment CreateAsSubstring(int start, int endExclusive, string str) {
		var ret = MessageSegment.CreateAsSubstring<HintSegment>(start, endExclusive, str);
		ret.HintText = str[(start + 1)..(endExclusive - 1)].Trim();
		return ret;
	}

	public static HintSegment CreateWithType(HintSegment other, HintType contextType) {
		return other with { HintType = contextType };
	}

}


public record class ContentSegment : MessageSegment {

	public int HintEndExclusive { get; private set; } = -1;
	public string HintText { get; private set; } = "";
	public ContentType ContentType { get; private set; } = ContentType.Invalid;

	public string Payload { get; private set; } = "";

	public static ContentSegment CreateAsSubstring(int start, int hintEndExclusive, int endExclusive, string str) {
		var ret = MessageSegment.CreateAsSubstring<ContentSegment>(start, endExclusive, str);
		ret.HintEndExclusive = hintEndExclusive;
		ret.HintText = str[(start + 1)..hintEndExclusive].Trim().ToLower();
		ret.Payload = str[(hintEndExclusive + 1)..(endExclusive - 1)];
		return ret;
	}

	public static ContentSegment CreateWithType(ContentSegment other, ContentType contextType) {
		return other with { ContentType = contextType };
	}

}
