using Godot;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class TableEditWindowBase : WindowEx, IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	protected virtual string LogName => "TableEditWindow";

	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;
		this.GetNodeNotNull<BaseButton>("%SaveButton").Pressed += OnClose;

		// Table edit
		TableEdit tableEdit = this.GetNodeNotNull<TableEdit>("%TableEdit");
		SetupTableEdit(tableEdit);
	}

	public void OnClose() {
		GD.Print($"Closing {LogName}");
		this.QueueFree();
	}

	protected virtual void SetupTableEdit(TableEdit tableEdit) { }

}
