using Godot;


namespace Kodipher.TypeToSqaud.Utils;


public static class ControlNodeExtensions
{

    /// <summary>
    /// Sets caret position to end of the current text:
    /// after the last chracter of the last line.
    /// </summary>
    /// <param name="textEditNode">Node to perform operation on</param>
    /// <remarks><b>Note:</b> Does not call <see cref="TextEdit.MergeOverlappingCarets"/></remarks>
    public static void SetCaretPositionToEnd(this TextEdit textEditNode)
    {
        var lastLineIndex = textEditNode.GetLineCount() - 1;
        textEditNode.SetCaretLine(lastLineIndex);
        textEditNode.SetCaretColumn(textEditNode.GetLine(lastLineIndex).Length);
    }

}

