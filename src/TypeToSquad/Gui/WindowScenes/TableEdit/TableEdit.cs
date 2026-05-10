using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using TypeToSquad.Gui.WindowScenes.Settings;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui.WindowScenes.TableEdit;


/// <summary>
/// <para>
/// A generic edit for all <see cref="Table{TRowTuple}"/>.
/// </para>
/// <para>
/// Has to be initiated in code with <see cref="InitiateFor(Table)"/>
/// and other methods.
/// </para>
/// </summary>
/// <remarks>
/// Tied to a specific initial state of the MainGrid.
/// Assumes validators do not change after initiation.
/// </remarks>
public partial class TableEdit : ScrollContainer {

	GridContainer mainGrid = null!; // Set in _Ready

	Table? targetTable = null; // Set when initiated
	SizeFlags[]? inputSizeFlagsHorizontal = null;

	Table TargetTable => targetTable ?? throw new NullReferenceException($"{nameof(TargetTable)} was not set.");
	
	public override void _Ready() {

		// Find main grid
		mainGrid = this.GetNodeNotNull<GridContainer>("%MainGrid");
		mainGrid.Columns = RowAdditionalNodeCount;

		// Find and move button prototypes
		prototypeDump = this.GetNodeNotNull<Control>("%PrototypeDump");

		upButtonPrototype = this.GetNodeNotNull<Button>("%UpButtonPrototype");
		downButtonPrototype = this.GetNodeNotNull<Button>("%DownButtonPrototype");
		deleteButtonPrototype = this.GetNodeNotNull<Button>("%DeleteButtonPrototype");
		columnNamePrototype = this.GetNodeNotNull<Label>("%ColumnNamePrototype");

		Control[] allPrototypes = [upButtonPrototype, downButtonPrototype, deleteButtonPrototype, columnNamePrototype];
		foreach (var proto in allPrototypes) {
			proto.UniqueNameInOwner = false;
			proto.GetParent().RemoveChild(proto);
			prototypeDump.AddChild(proto);
		}

		// Remove extra placeholders
		// (but keep placeholders in the header row)
		Control inputPlaceholder = this.GetNodeNotNull<Control>("%InputPlaceholder");
		inputPlaceholder.UniqueNameInOwner = false;
		inputPlaceholder.GetParent().RemoveChild(inputPlaceholder);
		inputPlaceholder.QueueFree();

		// Connect Add
		this.GetNodeNotNull<Button>("%AddRowButton").Pressed += OnAddPressed;
	}

	#region /--- Row Prototype ---/

	/// <summary>The number of nodes in a row which are not inputs for the table.</summary>
	const int RowAdditionalNodeCount = 3;
	
	/// <summary>How many of the <see cref="RowAdditionalNodeCount"/> are to the left of input nodes.</summary>
	const int RowAdditionalNodeCountLeft = 2;
	
	Control prototypeDump = null!; // Set in _Ready

	Button upButtonPrototype = null!; // Set in _Ready
	Button downButtonPrototype = null!; // Set in _Ready
	Button deleteButtonPrototype = null!; // Set in _Ready
	Label columnNamePrototype = null!; // Set in _Ready

	/// <summary>Create a row of nodes for the grid container based on a row of fields in the target table.</summary>
	IEnumerable<Control> CreateNodeRow(int tableRowIndex) {

		// Movement buttons
		var upButton = upButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		upButton.Pressed += () => OnUpPressed(upButton);
		yield return upButton;

		var downButton = downButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		downButton.Pressed += () => OnDownPressed(downButton);
		yield return downButton;

		// Inputs
		IReadOnlyList<Field> rowFields = TargetTable.GetFieldsAt(tableRowIndex);
		for (var j = 0; j < rowFields.Count; j++) {
			
			Control inputControl = FieldInputCreator.CreateFor(rowFields[j]);
			
			if (inputSizeFlagsHorizontal is not null && j < inputSizeFlagsHorizontal.Length) {
				inputControl.SizeFlagsHorizontal = inputSizeFlagsHorizontal[j];
			}
			inputControl.SizeFlagsVertical = SizeFlags.ShrinkBegin;

			yield return inputControl;
		}

		// Delete button
		var deleteButton = deleteButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		deleteButton.Pressed += () => OnDeletePressed(deleteButton);
		yield return deleteButton;
	}

	IEnumerable<Label> CreateUnnamedHeaderLabels() {
		
		for (int i = 0; i < TargetTable.ColumnCount; i++) {
			
			Label columnName = columnNamePrototype.Duplicate() as Label ?? throw new NodeNullException();

			if (inputSizeFlagsHorizontal is not null && i < inputSizeFlagsHorizontal.Length) {
				columnName.SizeFlagsHorizontal = inputSizeFlagsHorizontal[i];
			}
			columnName.SizeFlagsVertical = SizeFlags.ShrinkBegin;
			
			yield return columnName;
		}
	}

	#endregion

	public void InitiateFor(Table table, string[] columnNames, SizeFlags[] inputSizeFlagsHorizontal) {
		
		if (targetTable != null) throw new InvalidOperationException($"{nameof(TargetTable)} was already set.");
		
		// Set
		targetTable = table;
		this.inputSizeFlagsHorizontal = inputSizeFlagsHorizontal.ToArray();
		
		// Setup grid header
		mainGrid.Columns = RowAdditionalNodeCount + table.ColumnCount;

		Label[] headerLabels = CreateUnnamedHeaderLabels().ToArray();
		for (int i = 0; i < headerLabels.Length; i++) {
			
			if (i < columnNames.Length) {
				headerLabels[i].Text = columnNames[i];
			}
			
			mainGrid.AddChild(headerLabels[i]);
			mainGrid.MoveChild(headerLabels[i], RowAdditionalNodeCountLeft + i);
		}

		// Rows
		for (int i = 0; i < table.Count; i++) {
			foreach (var rowNode in CreateNodeRow(i)) mainGrid.AddChild(rowNode);
		}
	}

	int GridIndexToTableIndex(int sourceIndex) {
		return (sourceIndex / mainGrid.Columns) - 1;
	}
	
	int TableIndexToGridRowStartIndex(int tableIndex) {
		return (tableIndex + 1) * mainGrid.Columns;
	}

	void OnUpPressed(Button source) {

		// Move inside table
		int rowIndex = GridIndexToTableIndex(source.GetIndex());
		if (rowIndex < 1) return;

		TargetTable.MoveRow(rowIndex, rowIndex - 1);

		// Move inside grid by moving a previous row down
		int offset = TableIndexToGridRowStartIndex(rowIndex - 1);
		for (int nodesLeft = mainGrid.Columns; nodesLeft > 0; nodesLeft--) {
			mainGrid.MoveChild(mainGrid.GetChild(offset), offset + 2*mainGrid.Columns - 1);
		}
	}
	
	void OnDownPressed(Button source) {
		
		// Move inside table
		int rowIndex = GridIndexToTableIndex(source.GetIndex());
		if (rowIndex >= TargetTable.Count - 1) return;
		
		TargetTable.MoveRow(rowIndex, rowIndex + 1);

		// Move inside grid
		int offset = TableIndexToGridRowStartIndex(rowIndex);
		for (int nodesLeft = mainGrid.Columns; nodesLeft > 0; nodesLeft--) {
			mainGrid.MoveChild(mainGrid.GetChild(offset), offset + 2 * mainGrid.Columns - 1);
		}
	}
	
	void OnDeletePressed(Button source) {
		
		// Find row index
		int rowIndex = GridIndexToTableIndex(source.GetIndex());

		// Focus another row to keep focus
		int rowIndexToFocus = rowIndex == TargetTable.Count - 1 ? rowIndex - 1 : rowIndex + 1;
		int nodeIndexToFocus = TableIndexToGridRowStartIndex(rowIndexToFocus) + mainGrid.Columns - 1;
		mainGrid.GetChild<Control>(nodeIndexToFocus).GrabFocus();

		// Remove row
		TargetTable.RemoveAt(rowIndex);

		// Remove nodes
		int offset = TableIndexToGridRowStartIndex(rowIndex);
		for (int i = offset; i < offset + TargetTable.ColumnCount + RowAdditionalNodeCount; i++) {
			mainGrid.GetChild(i).QueueFree();
		}
	}
	
	void OnAddPressed() {
		TargetTable.AddEmpty();
		foreach (var rowNode in CreateNodeRow(TargetTable.Count - 1)) {
			mainGrid.AddChild(rowNode);
		}
	}

}
