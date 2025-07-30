

namespace TypeToSquad.Model.Markup;


public record class DepthSegment {
	public required int Start { get; init; }
	public required int EndExclusive { get; init; }
	public required int Depth { get; init; }
};

public record class ContextStartSegment : DepthSegment {
	public required int HintStart { get; init; }
	public required int HintEndExclusive { get; init; }
	public required int TextStart { get; init; }
}


/// <remarks>
/// Inherits <see cref="ContextStartSegment"/> 
/// but <b>not</b> <see cref="ContextEnd"/>.
/// </remarks>
public record class ContextFullSegment : ContextStartSegment {
	public required int TextEndExclusive { get; init; }
}

public record class ContextEnd : DepthSegment {
	public required int TextEndExclusive { get; init; }
}
