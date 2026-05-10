using Godot;
using System;


namespace TypeToSquad.Utils;


public class NodeNullException : Exception {

	const string MessageNoPath = "Node at a specified path was not found. Got null.";
	const string MessageForPathFormat = "Node at path {0} was not found. Got null.";

	public NodeNullException() : this(MessageNoPath) {}

	public NodeNullException(NodePath path) : base(string.Format(MessageForPathFormat, path)) { }

	public NodeNullException(NodePath path, Exception? innerException) 
	: base(string.Format(MessageForPathFormat, path), innerException) 
	{ }

}
