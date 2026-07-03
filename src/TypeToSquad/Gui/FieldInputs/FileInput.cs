using Godot;

using TypeToSquad.Utils;


namespace TypeToSquad.Gui.FieldInputs;


public partial class FileInput : HBoxContainer {

	public LineEdit LineEdit { get; private set; } = null!; // Set in _Ready
	public Button FileButton { get; private set; } = null!; // Set in _Ready
	public FileDialog FileDialog { get; private set; } = null!; // Set in _Ready

	/// <summary>
	/// <para>
	/// Finds and sets up children. Called automatically in <see cref="_Ready"/>.
	/// </para>
	/// <para>
	/// Can be called manually outside of <see cref="_Ready"/>,
	/// and after doing so it will not be called automatically the second time.
	/// </para>
	/// </summary>
	public void FindAndSetupChildren() {
		LineEdit = this.GetNodeNotNull<LineEdit>("%LineEdit");
		FileButton = this.GetNodeNotNull<Button>("%FileButton");
		FileDialog = this.GetNodeNotNull<FileDialog>("%FileDialog");

		FileButton.Pressed += () => FileDialog.PopupCentered(Vector2I.Zero);

		FileDialog.Visible = false;
		FileDialog.FileSelected += path => {
			LineEdit.Text = path;
			EmitSignalFileSelectedThroughDialog(path);
		};
	}
	
	public override void _Ready() {
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (LineEdit is null) FindAndSetupChildren();
	}

	[Signal]
	public delegate void FileSelectedThroughDialogEventHandler(string path);

}
