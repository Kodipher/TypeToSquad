

namespace TypeToSquad.Model.Markup;


/// <remarks>Immutable.</remarks>
public record MessageSegment {
	
	public string Text { get; init; } = "";
	
	/// <summary>The trimmed type of the tag.</summary>
	public string TagType { get; init; } = "";

	/// <summary>The value of the tag. Not automatically trimmed.</summary>
	public string TagArgument { get; init; } = "";
	
	public bool IsValid { get; init; } = false;

	public bool IsTag { get; init; } = false;

	public bool IsPlainText => IsValid && !IsTag;

}
