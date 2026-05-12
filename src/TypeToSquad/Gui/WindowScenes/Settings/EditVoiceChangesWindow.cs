using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class EditVoiceChangesWindow : EditTableWindowBase {
	
	protected override string LogName => "Voice Changes";
	protected override Table TargetTable => UserSettingsManager.Instance.Settings.VoiceChanges;
	protected override string[] ColumnNames => ["Hint", "Voice"];
	protected override SizeFlags[] InputSizeFlagsHorizontal => [SizeFlags.ShrinkBegin, SizeFlags.ExpandFill];
	
}
