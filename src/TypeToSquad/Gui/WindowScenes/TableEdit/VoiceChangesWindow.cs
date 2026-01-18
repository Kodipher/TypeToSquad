using TypeToSquad.Model;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class VoiceChangesWindow : TableEditWindowBase {

	protected override string LogName => "Voice Changes";

	protected override void SetupTableEdit(TableEdit tableEdit) {
		tableEdit.SetInputSizeFlagPreInit(SizeFlags.ShrinkBegin, SizeFlags.ExpandFill);
		tableEdit.InitiateFor(UserSettingsManager.Instance.Settings.VoiceChanges);
		tableEdit.SetColumnNamesPostInit("Context", "Voice");
	}

}
