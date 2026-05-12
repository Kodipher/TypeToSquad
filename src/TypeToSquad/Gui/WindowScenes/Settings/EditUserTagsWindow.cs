using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class EditUserTagsWindow : EditTableWindowBase {
	
	protected override string LogName => "User Tags";
	protected override Table TargetTable => UserSettingsManager.Instance.Settings.UserTags;
	protected override string[] ColumnNames => ["Type", "Pattern", "Replacement"];
	protected override SizeFlags[] InputSizeFlagsHorizontal => [SizeFlags.ShrinkBegin, SizeFlags.ExpandFill, SizeFlags.ExpandFill];
	
}
