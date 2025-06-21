using Godot;
using System;


namespace TypeToSquad.Utils;


public class NodeNullException : Exception {

	const string message = "Node at a specified path was not found. Got null.";
	const string messageForPathFormat = "Node at path {0} was not found. Got null.";

	public NodeNullException() : this(message) {}

	public NodeNullException(NodePath path) : base(string.Format(messageForPathFormat, path)) { }

	public NodeNullException(NodePath path, Exception? innerException) 
	: base(string.Format(messageForPathFormat, path), innerException) 
	{ }

}
