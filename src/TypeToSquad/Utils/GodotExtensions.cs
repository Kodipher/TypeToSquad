using Godot;
using System;


namespace TypeToSquad.Utils;


public static class GodotExtensions {

	/// <summary>
	/// Equivalent of <see cref="Node.GetNode{T}(NodePath)"/>
	/// with an additional null check.
	/// Throws <see cref="NodeNullException"/> if node does not exist.
	/// Throws <see cref="InvalidCastException"/> if node is of incorrect type.
	/// </summary>
	/// <exception cref="NodeNullException">Node was not found.</exception>
	/// <exception cref="InvalidCastException">Node could not be cast to requested type.</exception>
	public static T GetNodeNotNull<T>(this Node thisNode, NodePath path) where T : notnull, Node {
		return thisNode.GetNode<T>(path) ?? throw new NodeNullException(path);
	}

}
