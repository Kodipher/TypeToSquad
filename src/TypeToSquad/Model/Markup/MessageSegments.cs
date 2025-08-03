

namespace TypeToSquad.Model.Markup;


public record class MessageSegment {

	public int Start { get; protected set; } = -1;
	public int EndExclusive { get; protected set; } = -1;
	public string Text { get; protected set; } = "";

	public static MessageSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return CreateAsSubstring<MessageSegment>(start, endExclusive, str);
	}

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


public record class InvalidSegment : MessageSegment {

	public static new InvalidSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return MessageSegment.CreateAsSubstring<InvalidSegment>(start, endExclusive, str);
	}

}


public record class HintSegment : MessageSegment {

	public HintType HintType { get; private set; } = HintType.Unknown;

	public string Hint { get; private set; } = "";

	public static new HintSegment CreateAsSubstring(int start, int endExclusive, string str) {
		var ret = MessageSegment.CreateAsSubstring<HintSegment>(start, endExclusive, str);
		ret.Hint = str[(start + 1)..(endExclusive - 1)].Trim();
		return ret;
	}

	public static HintSegment CreateWithType(HintSegment other, HintType contextType) {
		return other with { HintType = contextType };
	}

}


public record class ContentSegment : MessageSegment {

	public int HintEndExclusive { get; private set; } = -1;
	public string Hint { get; private set; } = "";
	public string Payload { get; private set; } = "";

	public static ContentSegment CreateAsSubstring(int start, int hintEndExclusive, int endExclusive, string str) {
		var ret = MessageSegment.CreateAsSubstring<ContentSegment>(start, endExclusive, str);
		ret.HintEndExclusive = hintEndExclusive;
		ret.Hint = str[(start + 1)..hintEndExclusive].Trim().ToLower();
		ret.Payload = str[(hintEndExclusive + 1)..(endExclusive - 1)];
		return ret;
	}

}
