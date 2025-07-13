using Godot;


namespace TypeToSquad.Utils;


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
	/// </summary>
	[Export]
	public Theme? MainTheme { get; set; } = null;

	/// <summary>
	/// A theme that patchrs <see cref="MainTheme"/>
	/// when keyboard input is detected.
	/// </summary>
	[Export]
	public Theme? KeyboardPatchTheme { get; set; } = null;

	void HandleInputEvent() {
	}

	#endregion

	public override void _Ready() {
		base._Ready();
		TryAutoFocusNode();
	}

}
