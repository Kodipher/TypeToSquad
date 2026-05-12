using Godot;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.Settings;


public abstract partial class EditTableWindowBase : WindowEx {

	#region /--- Setup stuff, set in children ---/
	
	protected abstract string LogName { get; }
	
	protected abstract Table TargetTable { get; }
	
	protected abstract string[] ColumnNames { get; }
	
	protected abstract Control.SizeFlags[] InputSizeFlagsHorizontal { get; }
	
	#endregion

	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;
		this.GetNodeNotNull<BaseButton>("%SaveButton").Pressed += OnClose;

		// Table edit
		TableEdit tableEdit = this.GetNodeNotNull<TableEdit>("%TableEdit");
		tableEdit.InitiateFor(TargetTable, ColumnNames, InputSizeFlagsHorizontal);
	}

	public void OnClose() {
		GD.Print($"Closing {LogName}");
		this.QueueFree();
	}

}
