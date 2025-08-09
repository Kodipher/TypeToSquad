using Godot;
using System;
using System.Reflection;


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

	/// <summary>
	/// <para>
	/// Tries to a convert any object into a <see cref="Variant"/>.
	/// </para>
	/// <para>
	/// <b>NOTE:</b> only intended to be used with variant compatible types.
	/// Prefer to use <see cref="Variant.From{T}(in T)"/> when possible.
	/// </para>
	/// </summary>
	public static Variant VariantFromUnsafe(object? from) {
		if (from is null) return new Variant();

		return (Variant)(
			variantFromMethod.Value.MakeGenericMethod(from.GetType()).Invoke(null, [from]) 
			?? throw new ArgumentException($"Variant.From returned null")
		);
	}

	readonly static Lazy<MethodInfo> variantFromMethod = new(
		() => typeof(Variant)
				.GetMethod(nameof(Variant.From)) 
				?? throw new InvalidOperationException($"Could not find Varaint.From method")
	);

	/// <summary>
	/// <para>
	/// An unsafe version of <see cref="Variant.As{T}()"/>.
	/// Takes a <see cref="Type"/> parameter for reflection purposes.
	/// </para>
	/// <para>
	/// <b>NOTE:</b> only intended to be used with variant compatible types.
	/// Prefer to use <see cref="Variant.As{T}()"/> when possible.
	/// </para>
	/// </summary>
	public static object? AsUnsafe(this Variant variant, Type type) {
		return variantAsMethod.Value.MakeGenericMethod([type]).Invoke(variant, []);
	}

	readonly static Lazy<MethodInfo> variantAsMethod = new(
		() => typeof(Variant)
				.GetMethod(nameof(Variant.As))
				?? throw new InvalidOperationException($"Could not find Varaint.As method")
	);

	/// <summary>
	/// Sets caret position to end of the text:
	/// after the last chracter of the last line.
	/// </summary>
	/// <remarks>Does not call <see cref="TextEdit.MergeOverlappingCarets"/></remarks>
	public static void SetCaretPositionToEnd(this TextEdit textEditNode, int caretIndex = 0) {
		var lastLineIndex = textEditNode.GetLineCount() - 1;
		textEditNode.SetCaretLine(lastLineIndex, caretIndex: caretIndex);
		textEditNode.SetCaretColumn(textEditNode.GetLine(lastLineIndex).Length, caretIndex: caretIndex);
	}

	/// <summary>
	/// Returns the start index of a specific line
	/// in the <see cref="TextEdit.Text"/> string.
	/// An out of bounds line returns -1.
	/// </summary>
	public static int GetLineStartIndex(this TextEdit textEditNode, int lineIndex) {

		if (lineIndex < 0) return -1;

		int totalLines = textEditNode.GetLineCount();
		if (lineIndex >= totalLines) return -1;

		int startIndex = 0;
		for (int i = 0; i < lineIndex; i++) {
			startIndex += textEditNode.GetLine(i).Length;
			startIndex += 1; // The engine separates lines by "\n"
		}

		return startIndex;
	}

}
