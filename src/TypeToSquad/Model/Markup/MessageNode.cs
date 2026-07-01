using System;
using System.Collections.Generic;


namespace TypeToSquad.Model.Markup;


/// <summary>
///	Represents a node of a tree that makes up the message.
/// The tree can be the root.
/// </summary>
/// <remarks>
/// Only partially immutable.
/// <see cref="Children"/> and <see cref="Attributes"/> can be mutated.
/// </remarks>
public record MessageNode {

	public MessageNodeType Type { get; init; } = MessageNodeType.Invalid;
	
	public List<MessageNode> Children { get; } = new();
	
	public Dictionary<MessageNodeAttribute, string> Attributes { get; } = new();

	/// <summary>The text in the original message this node represents.</summary>
	public string OriginalText { get; init; } = "";
	
}


public sealed class MessageNodeType(string value) : IEquatable<MessageNodeType> { 
	// strongly typed id / string enum
	
	public static readonly MessageNodeType
		Invalid = new("invalid"),
		Text = new("text"),
		OriginalMessageTag = new("tag"),
		OriginalMessageRoot = new("message"),
		
		Break = new("break"),
		Sound = new("sound");

	#region /--- ToString, Equality ---/
	
	readonly string value = value;

	public override string ToString() => value;

	public override int GetHashCode() => value.GetHashCode();

	public bool Equals(MessageNodeType? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return value == other.value;
	}

	public override bool Equals(object? obj) {
		return ReferenceEquals(this, obj) || obj is MessageNodeType other && Equals(other);
	}
	
	public static bool operator ==(MessageNodeType? left, MessageNodeType? right) => Equals(left, right);

	public static bool operator !=(MessageNodeType? left, MessageNodeType? right) => !Equals(left, right);
	
	#endregion
	
}


public sealed class MessageNodeAttribute(string value) : IEquatable<MessageNodeAttribute> { 
	// strongly typed id / string enum

	public static readonly MessageNodeAttribute
		OriginalMessageTagType = new("og-tag-type"),
		OriginalMessageTagArgument = new("og-tag-argument");
	
	#region /--- ToString, Equality ---/
	
	readonly string value = value;

	public override string ToString() => value;

	public override int GetHashCode() => value.GetHashCode();

	public bool Equals(MessageNodeAttribute? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return value == other.value;
	}

	public override bool Equals(object? obj) {
		return ReferenceEquals(this, obj) || obj is MessageNodeAttribute other && Equals(other);
	}
	
	public static bool operator ==(MessageNodeAttribute? left, MessageNodeAttribute? right) => Equals(left, right);

	public static bool operator !=(MessageNodeAttribute? left, MessageNodeAttribute? right) => !Equals(left, right);
	
	#endregion
	
}
