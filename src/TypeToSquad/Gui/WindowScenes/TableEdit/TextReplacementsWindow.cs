using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class TextReplacementsWindow : TableEditWindowBase {

	protected override string LogName => "Text Replacements";

	protected override void SetupTableEdit(TableEdit tableEdit) {
		tableEdit.SetInputSizeFlagPreInit(SizeFlags.ShrinkBegin, SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		tableEdit.InitiateFor(CoreNode.UserSettings.TextReplacements);
		tableEdit.SetColumnNamesPostInit("Context", "Pattern", "Replacement");
	}

}
