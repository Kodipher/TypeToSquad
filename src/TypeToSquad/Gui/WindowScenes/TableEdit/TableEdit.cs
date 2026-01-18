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
	SizeFlags[]? inputSizeFlags = null;

	public override void _Ready() {

		// Find main grid
		mainGrid = this.GetNodeNotNull<GridContainer>("%MainGrid");
		mainGrid.Columns = gridRowTotalPrototypes;

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

		// Connect Add
		this.GetNodeNotNull<Button>("%AddRowButton").Pressed += OnAddPressed;
	}

	#region //// Row Prototype

	Control prototypeDump = null!; // Set in _Ready

	Button upButtonPrototype = null!; // Set in _Ready
	Button downButtonPrototype = null!; // Set in _Ready
	Button deleteButtonPrototype = null!; // Set in _Ready
	Label columnNamePrototype = null!; // Set in _Ready

	const int gridRowTotalPrototypes = 3;
	const int gridRowLeftPrototypes = 2;

	IEnumerable<Control> CreateNewRowNodes() {

		if (targetTable is null) throw new InvalidOperationException($"{nameof(targetTable)} was not set.");

		// Up button
		var upButton = upButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		upButton.Pressed += () => OnUpPressed(upButton);
		yield return upButton;

		var downButton = downButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		downButton.Pressed += () => OnDownPressed(downButton);
		yield return downButton;

		// Inputs
		Field?[] validators = targetTable.GetValidationProxies();
		for (int i = 0; i < validators.Length; i++) {
			Field? validator = validators[i];
			Control inputControl; 

			if (validator is null) {
				inputControl = FieldInputCreator.CreateForAnyUnlinked();
			} else {
				inputControl = FieldInputCreator.CreateFor(validator, isUnlinked: true);
			}

			FieldInputCreator.ConnectOnControlSubmit(inputControl, newVal => OnInputSubmit(inputControl, newVal));

			if (inputSizeFlags is not null && i < inputSizeFlags.Length) {
				inputControl.SizeFlagsHorizontal = inputSizeFlags[i];
			}
			inputControl.SizeFlagsVertical = SizeFlags.ShrinkBegin;
			inputControl.Visible = true;

			yield return inputControl;
		}

		// Delete button
		var deleteButton = deleteButtonPrototype.Duplicate() as Button ?? throw new NodeNullException();
		deleteButton.Pressed += () => OnDeletePressed(deleteButton);
		yield return deleteButton;
	}

	#endregion

	#region //// Init

	public void InitiateFor(Table table) {
		ThrowIfInitiated();

		targetTable = table;

		mainGrid.Columns = gridRowTotalPrototypes + table.ColumnCount;

		// Header
		for (int i = 0; i < table.ColumnCount; i++) {

			Label columnName = columnNamePrototype.Duplicate() as Label ?? throw new NodeNullException();

			if (inputSizeFlags is not null && i < inputSizeFlags.Length) {
				columnName.SizeFlagsHorizontal = inputSizeFlags[i];
			}
			columnName.SizeFlagsVertical = SizeFlags.ShrinkBegin;

			mainGrid.AddChild(columnName);
			mainGrid.MoveChild(columnName, 0 + gridRowLeftPrototypes + i);;
		}

		// Rows
		for (int i = 0; i < table.Count; i++) {
			foreach (var rowNode in CreateNewRowNodes()) mainGrid.AddChild(rowNode);

			int rowStartIndex = -(gridRowTotalPrototypes + table.ColumnCount);
			// Negative index counting backwards from end

			Variant[] rowData = table.GetAtAsArray(i);
			for (int j = 0; j < rowData.Length; j++) {
				var controlNode = mainGrid.GetChild<Control>(rowStartIndex + gridRowLeftPrototypes + j);
				FieldInputCreator.SetControlInputValue(controlNode, rowData[j]);
			}
		}
	}

	public void SetInputSizeFlagPreInit(params SizeFlags[] inputSizeFlags) {
		ThrowIfInitiated();
		this.inputSizeFlags = inputSizeFlags.ToArray();
	}

	public void SetColumnNamesPostInit(params string[] columnNames) {
		ThrowIfNotInitiated();

		if (columnNames.Length != targetTable!.ColumnCount) {
			throw new ArgumentException($"Column count mismatch. Expected {targetTable.ColumnCount}, got {columnNames.Length}.");
		}

		for (int i = 0; i < columnNames.Length; i++) {
			mainGrid.GetChild<Label>(gridRowLeftPrototypes + i).Text = columnNames[i];
		}
	}

	#endregion

	int GridIndexToTableIndex(int sourceIndex) {
		return (sourceIndex / mainGrid.Columns) - 1;
	}
	
	int TableIndexToGridRowStartIndex(int tableIndex) {
		return (tableIndex + 1) * mainGrid.Columns;
	}

	void OnUpPressed(Button source) {
		ThrowIfNotInitiated();

		// Move inside table
		int rowIndex = GridIndexToTableIndex(source.GetIndex());
		if (rowIndex < 1) return;

		var rowData = targetTable!.GetAtAsArray(rowIndex);
		targetTable.RemoveAt(rowIndex);
		targetTable.InsertAsArray(rowIndex - 1, rowData);

		// Move inside grid by moving a previous row down
		int offset = TableIndexToGridRowStartIndex(rowIndex - 1);
		for (int nodesLeft = mainGrid.Columns; nodesLeft > 0; nodesLeft--) {
			mainGrid.MoveChild(mainGrid.GetChild(offset), offset + 2*mainGrid.Columns - 1);
		}
	}

	void OnDownPressed(Button source) {
		ThrowIfNotInitiated();

		// Move inside table
		int rowIndex = GridIndexToTableIndex(source.GetIndex());
		if (rowIndex >= targetTable!.Count - 1) return;

		var rowData = targetTable.GetAtAsArray(rowIndex);
		targetTable.RemoveAt(rowIndex);
		targetTable.InsertAsArray(rowIndex + 1, rowData);

		// Move inside grid
		int offset = TableIndexToGridRowStartIndex(rowIndex);
		for (int nodesLeft = mainGrid.Columns; nodesLeft > 0; nodesLeft--) {
			mainGrid.MoveChild(mainGrid.GetChild(offset), offset + 2 * mainGrid.Columns - 1);
		}
	}

	void OnInputSubmit(Control source, Variant newValue) {
		ThrowIfNotInitiated();

		// Find row and column index
		int childIndex = source.GetIndex();
		int rowIndex = GridIndexToTableIndex(childIndex);
		int indexWithinRow = (childIndex % mainGrid.Columns) - gridRowLeftPrototypes;

		// Change the value
		Variant[] values;

		values = targetTable!.GetAtAsArray(rowIndex);
		values[indexWithinRow] = newValue;
		targetTable!.SetAtAsArray(rowIndex, values);

		// Update the node
		values = targetTable!.GetAtAsArray(rowIndex);
		FieldInputCreator.SetControlInputValue(source, values[indexWithinRow]);
	}

	void OnDeletePressed(Button source) {
		ThrowIfNotInitiated();

		// Find row index
		int rowIndex = GridIndexToTableIndex(source.GetIndex());

		// Focus another row to keep focus
		int rowIndexToFocus = rowIndex == targetTable!.Count - 1 ? rowIndex - 1 : rowIndex + 1;
		int nodeIndexToFocus = TableIndexToGridRowStartIndex(rowIndexToFocus) + mainGrid.Columns - 1;
		mainGrid.GetChild<Control>(nodeIndexToFocus).GrabFocus();

		// Remove row
		targetTable!.RemoveAt(rowIndex);

		// Remove nodes
		int offset = TableIndexToGridRowStartIndex(rowIndex);
		for (int i = offset; i < offset + targetTable!.ColumnCount + gridRowTotalPrototypes; i++) {
			mainGrid.GetChild(i).QueueFree();
		}
	}

	void OnAddPressed() {
		ThrowIfNotInitiated();

		// Add empty/default data row
		targetTable!.AddAsArray(Array.Empty<Variant>());
		Variant[] rowData = targetTable.GetAtAsArray(targetTable.Count - 1);
		
		// Add new row into the table
		foreach (var rowNode in CreateNewRowNodes()) mainGrid.AddChild(rowNode);

		// Fill new row with default data
		int rowStartIndex = -(gridRowTotalPrototypes + targetTable.ColumnCount);

		for (int j = 0; j < rowData.Length; j++) {
			var controlNode = mainGrid.GetChild<Control>(rowStartIndex + gridRowLeftPrototypes + j);
			FieldInputCreator.SetControlInputValue(controlNode, rowData[j]);
		}
	}

	#region //// Throw helpers

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ThrowIfNotInitiated() {
		if (targetTable is null) {
			throw new InvalidOperationException($"{nameof(TableEdit)} was not initiated.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ThrowIfInitiated() {
		if (targetTable is not null) {
			throw new InvalidOperationException($"{nameof(TableEdit)} was already initiated.");
		}
	}

	#endregion

}
