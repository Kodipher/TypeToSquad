

namespace TypeToSquad.Model.Markup;


public record class MessageSegment {

	public int Start { get; private set; } = -1;
	public int EndExclusive { get; private set; } = -1;
	public string Text { get; private set; } = "";

	public static MessageSegment CreateAsSubstring(int start, int endExclusive, string str) {
		return new MessageSegment {
			Start = start,
			EndExclusive = endExclusive,
			Text = str[start..endExclusive],
		};
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
		return CreateAsSubstring<InvalidSegment>(start, endExclusive, str);
	}

}

public record class HintSegment : MessageSegment {

	public string Hint { get; private set; } = "";

	public static new HintSegment CreateAsSubstring(int start, int endExclusive, string str) {
		var ret = CreateAsSubstring<HintSegment>(start, endExclusive, str);
		ret.Hint = str[(start + 1)..(endExclusive - 1)].Trim();
		return ret;
	}

}

public record class ContentSegment : MessageSegment {

	public int HintEndExclusive { get; private set; } = -1;
	public string Hint { get; private set; } = "";
	public string Payload { get; private set; } = "";

	public static new ContentSegment CreateAsSubstring(int start, int hintEndExclusive, int endExclusive, string str) {
		var ret = CreateAsSubstring<ContentSegment>(start, endExclusive, str);
		ret.HintEndExclusive = hintEndExclusive;
		ret.Hint = str[(start + 1)..hintEndExclusive].Trim().ToLower();
		ret.Payload = str[(hintEndExclusive + 1)..(endExclusive - 1)];
		return ret;
	}

}
