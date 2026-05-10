using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class TextReplacementsWindow : TableEditWindowBase {
	
	protected override string LogName => "Text Replacements";
	protected override Table TargetTable => UserSettingsManager.Instance.Settings.TextReplacements;
	protected override string[] ColumnNames => ["Context", "Pattern", "Replacement"];
	protected override SizeFlags[] InputSizeFlagsHorizontal => [SizeFlags.ShrinkBegin, SizeFlags.ExpandFill, SizeFlags.ExpandFill];
	
}
