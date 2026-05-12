using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using SizeFlags = Godot.Control.SizeFlags;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public partial class EditSoundEffectsWindow : EditTableWindowBase {
	
	protected override string LogName => "Sound Effects";
	protected override Table TargetTable => UserSettingsManager.Instance.Settings.SoundEffects;
	protected override string[] ColumnNames => ["Hint", "File Path", "Volume, %"];
	protected override SizeFlags[] InputSizeFlagsHorizontal => [SizeFlags.ShrinkBegin, SizeFlags.ExpandFill, SizeFlags.ShrinkEnd];
	
}
