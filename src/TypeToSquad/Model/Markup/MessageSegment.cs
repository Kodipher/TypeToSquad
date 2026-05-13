

namespace TypeToSquad.Model.Markup;


public record MessageSegment {

	/// <summary>Inner text of the segment</summary>
	public string Text { get; init; } = "";

	public bool IsValid { get; init; } = false;

	public bool IsTag { get; init; } = false;

	/// <summary>The trimmed type of the tag.</summary>
	public string TagType { get; private set; } = "";

	/// <summary>The value of the tag. Not automatically trimmed.</summary>
	public string TagArgument { get; private set; } = "";
	
	public bool IsPlainText => IsValid && !IsTag;
	
	public static MessageSegment MakePlain(string str) {
		return new MessageSegment {
			IsValid = true,
			Text = str
		};
	}
	
	public static MessageSegment MakeInvalid(string str) {
		return new MessageSegment {
			IsValid = false,
			Text = str
		};
	}
	
	/// <remarks>Include tag brackets.</remarks>
	public static MessageSegment MakeTag(string str) {

		(string type, string argument) = MessageLexer.ParseTag(str, out _);
		
		return new MessageSegment {
			IsValid = true,
			Text = str,
			IsTag = true,
			TagType = type,
			TagArgument = argument
		};
	}

}
