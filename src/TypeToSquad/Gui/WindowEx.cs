using Godot;


namespace TypeToSquad.Gui;


/// <summary>
/// An extension of the <see cref="Window"/> class.
/// Has a few exports.
/// </summary>
[GlobalClass]
public partial class WindowEx : Window {

	#region //// Auto focus on ready

	/// <summary>
	/// <para>
	/// The control to be focused automatically when the node is ready.
	/// </para>
	/// <para>
	/// <see cref="TabContainer"/> has special handling 
	/// to focus its <see cref="TabBar"/>
	/// </para>
	/// </summary>
	[Export]
	public Control? AutoFocusNode { get; set; } = null;

	void TryAutoFocusNode() {

		if (AutoFocusNode is null) return;

		if (AutoFocusNode is TabContainer tabContainerAutofocus) {
			tabContainerAutofocus.GetTabBar().GrabFocus();
			return;
		}

		AutoFocusNode?.GrabFocus();
	}

	#endregion

	#region //// Theme swap on keyboard input

	/// <summary>
	/// The main theme used by window.
	/// Overrides <see cref="Window.Theme"/>.
	/// The theme is assumed to not change.
	/// </summary>
	[Export]
	public Theme? MainTheme { get; set; } = null;

	/// <summary>
	/// A theme that patches <see cref="MainTheme"/>
	/// when keyboard input is detected.
	/// </summary>
	/// <remarks>
	/// Only stylebox patches are supported.
	/// </remarks>
	[Export]
	public Theme? KeyboardPatchTheme { get; set; } = null;

	/// <summary>
	/// If set, the theme patch will be reverted
	/// on mouse click.
	/// </summary>
	[Export]
	public bool RevertThemeOnMouseInput { get; set; } = false;

	bool isThemePatched = false;

	void HandleThemeSwitchInput(InputEvent inputEvent) {

		if (isThemePatched) {

			// Try revert theme patch
			if (!RevertThemeOnMouseInput) return;

			if (inputEvent is not InputEventMouseButton inputEventMouseButton) return;


			if (inputEventMouseButton.Pressed) {
				ResetThemeToMain();
			}
			
		} else {

			// Try patch
			if (inputEvent is not InputEventKey inputEventKey) return;
				
			if (!inputEventKey.Pressed) return;

			if (GuiGetFocusOwner() is TextEdit) return;


			if (inputEventKey.IsActionPressed("ui_focus_next", exactMatch: true)) {
				PatchTheme();
				SetInputAsHandled(); // do not move
				return;
			}

			if (inputEventKey.IsActionPressed("ui_focus_prev", exactMatch: true)) {
				PatchTheme();
				return;
			}

			if (
				inputEventKey.IsActionPressed("ui_up", exactMatch: true) ||
				inputEventKey.IsActionPressed("ui_down", exactMatch: true) ||
				inputEventKey.IsActionPressed("ui_left", exactMatch: true) ||
				inputEventKey.IsActionPressed("ui_right", exactMatch: true)
			) {
				PatchTheme();
				SetInputAsHandled(); // do not move
				return;
			}
			
		}

	}

	void ResetThemeToMain() {
		if (MainTheme is null) return;
		this.Theme = (Theme)MainTheme.Duplicate();
		isThemePatched = false;
	}

	void PatchTheme() {
		if (isThemePatched) return;
		if (KeyboardPatchTheme is null) return;

		foreach (var type in KeyboardPatchTheme.GetTypeList()) {
			foreach (var property in KeyboardPatchTheme.GetStyleboxList(type)) {
				var proeprtyOverride = KeyboardPatchTheme.GetStylebox(property, type);
				this.Theme.SetStylebox(property, type, proeprtyOverride);
			}
		}

		isThemePatched = true;
	}

	#endregion

	#region //// UI Zoom

	// Note: experimental

	[Export]
	public float MinContentScale { get; set; } = 0.5f;

	[Export]
	public float MaxContentScale { get; set; } = 5.0f;

	void HandleZoomInput(InputEvent @event) {
		if (@event.IsActionPressed("zoom_in")) {
			ContentScaleFactor = Mathf.Min(ContentScaleFactor + 0.1f, MaxContentScale);
		} else if (@event.IsActionPressed("zoom_out")) {
			ContentScaleFactor = Mathf.Max(ContentScaleFactor - 0.1f, MinContentScale);
		} else if (@event.IsActionPressed("zoom_reset")) {
			this.ContentScaleFactor = 1.0f;
		}
	}

	#endregion

	public override void _Ready() {
		base._Ready();
		TryAutoFocusNode();
		ResetThemeToMain();
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);
		HandleThemeSwitchInput(@event);
		HandleZoomInput(@event);
	}

}
