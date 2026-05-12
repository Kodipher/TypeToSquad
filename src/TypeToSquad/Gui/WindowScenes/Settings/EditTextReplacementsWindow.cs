using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class EditTextReplacementsWindow : EditTableWindowBase {
	
	protected override string LogName => "Text Replacements";
	protected override Table TargetTable => UserSettingsManager.Instance.Settings.TextReplacements;
	protected override string[] ColumnNames => ["Pattern", "Replacement"];
	protected override SizeFlags[] InputSizeFlagsHorizontal => [SizeFlags.ExpandFill, SizeFlags.ExpandFill];
	
}
