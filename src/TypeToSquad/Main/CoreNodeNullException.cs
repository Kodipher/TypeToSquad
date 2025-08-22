using Godot;
using System;


namespace TypeToSquad;


public class CoreNodeNullException : InvalidOperationException {

	const string message = "Core Node is null or was not set";

	public CoreNodeNullException() : base(message) {}

	public CoreNodeNullException(NodePath path, Exception? innerException) : base(message, innerException) {}

}
