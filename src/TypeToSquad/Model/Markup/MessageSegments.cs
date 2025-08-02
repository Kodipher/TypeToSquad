

namespace TypeToSquad.Model.Markup;


public record class MessageSegment {

	public int Start { get; init; }
	public int EndExclusive { get; init; }
	public string Text { get; }

	public MessageSegment(int start, int endExclusive, string originalMessage) {
		Start = start;
		EndExclusive = endExclusive;
		Text = originalMessage[start..endExclusive];
	}
}

public record class InvalidSegment : MessageSegment {
	public InvalidSegment(int start, int endExclusive, string originalMessage) 
	: base(start, endExclusive, originalMessage) { }
}

public record class HintSegment : MessageSegment {

	public string Hint { get; }

	public HintSegment(int start, int endExclusive, string originalMessage)
	: base(start, endExclusive, originalMessage) 
	{ 
		Hint = originalMessage[(start+1)..(endExclusive-1)].Trim();
	}
}

public record class ContentSegment : MessageSegment {

	public int HintEndExclusive { get; }
	public string Hint { get; }
	public string Payload { get; }

	public ContentSegment(int start, int hintEndExclusive, int endExclusive, string originalMessage)
	: base(start, endExclusive, originalMessage) 
	{
		HintEndExclusive = hintEndExclusive;
		Hint = originalMessage[(start + 1)..hintEndExclusive].Trim();
		Payload = originalMessage[(hintEndExclusive+1).. (endExclusive-1)];
	}

}
