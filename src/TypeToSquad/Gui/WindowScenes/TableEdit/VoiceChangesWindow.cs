using System;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class VoiceChangesWindow : TableEditWindowBase {

	protected override string LogName => "Voice Changes";

	protected override void SetupTableEdit(TableEdit tableEdit) {
		if (CoreNode is null) throw new InvalidOperationException();

		tableEdit.SetInputSizeFlagPreInit(SizeFlags.ShrinkBegin, SizeFlags.ExpandFill);
		tableEdit.InitiateFor(CoreNode.UserSettings.VoiceChanges);
		tableEdit.SetColumnNamesPostInit("Context", "Voice");
	}

}
