using Godot;
using System;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


public partial class TableEditWindowBase : WindowEx, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	protected virtual string LogName => "TableEditWindow";

	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;
		this.GetNodeNotNull<BaseButton>("%SaveButton").Pressed += OnClose;

		// Table edit
		if (CoreNode is not null) {
			TableEdit tableEdit = this.GetNodeNotNull<TableEdit>("%TableEdit");
			SetupTableEdit(tableEdit);
		}

	}

	public void OnClose() {
		GD.Print($"Closing {LogName}");
		this.QueueFree();
	}

	protected virtual void SetupTableEdit(TableEdit tableEdit) { }

}
