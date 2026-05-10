using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad.Gui;


public partial class WindowManager : Node {
	
	#region /--- Singleton ---/

	public static WindowManager Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion

	public override void _Ready() {
		StageSingletonInstance();
	}
	
	static readonly WindowSceneAssignment windowScenes = GD.Load<WindowSceneAssignment>("res://Gui/windowSceneAssingment.tres");
	
	readonly Dictionary<WindowType, Window> currentChildrenByType = new();
	
	/// <summary> Creates an instance of one of the window scenes.</summary>
	/// <remarks>
	/// <see cref="Node._Ready"/> is not called by this method,
	/// as this method does not add the <see cref="Window"/> into the scene tree.
	/// </remarks>
	Window InstantiateWindowScene(WindowType windowType) {

		// Get window
		PackedScene? windowScene = windowScenes.Scenes.GetValueOrDefault(windowType, null);
		if (windowScene is null) {
			throw new InvalidOperationException($"No scene set for window of type {windowType}");
		}

		// Instantiate
		Node windowSceneRoot = windowScene.Instantiate();

		if (windowSceneRoot is not Window window) {
			throw new InvalidOperationException($"Scene set for window of type {windowType} must have a Window node as root.");
		}

		// Return
		return window;
	}

	/// <summary>
	/// Creates a new window of type <paramref name="windowType"/>
	/// and makes it a child of the manager.
	/// If the window already exists then focuses it instead.
	/// </summary>
	/// <returns>The created or focused window.</returns>
	public Window CreateWindowAtSelfUnique(WindowType windowType) {

		GD.Print($"Window {windowType} requested.");

		// Return existing
		if (currentChildrenByType.TryGetValue(windowType, out Window? existingWindow)) {
			// Focus requested window
			if (existingWindow.Mode == Window.ModeEnum.Minimized) {
				existingWindow.Mode = Window.ModeEnum.Windowed;
			}
			existingWindow.GrabFocus();

			return existingWindow;
		}

		// Create new
		Window newWindow = InstantiateWindowScene(windowType);
		currentChildrenByType.Add(windowType, newWindow);
		newWindow.TreeExiting += () => currentChildrenByType.Remove(windowType);

		this.AddChild(newWindow);

		return newWindow;
	}

	/// <summary>
	/// Returns a window of type <paramref name="windowType"/>
	/// if it exists as a child of the manager.
	/// Returns <see langword="null"/> if the window does not exit.
	/// </summary>
	public Window? GetExistingWindowAtSelf(WindowType windowType) {
		return currentChildrenByType.GetValueOrDefault(windowType, null!);
	}
	
	/// <summary>
	/// <para>
	/// Creates a window of type <paramref name="windowType"/>
	/// and transfers the script, script properties and
	/// children into root window.
	/// </para>
	/// <para>
	/// This is done by moving the child of the window into root,
	/// reinstantiating the script and moving all script properties to the new script instance,
	/// and then removing the original window.
	/// </para>
	/// <para>
	/// The window must have a single child,
	/// that does not have <see cref="Node.UniqueNameInOwner"/> set,
	/// to facilitate transfer.
	/// </para>
	/// <para>
	/// <see cref="Node._Ready"/> of the window is called
	/// automatically after the transfer.
	/// </para>
	/// <para>
	/// The <see cref="Node._Ready"/> method is called directly.
	/// No notification is sent, thus the children do not have their <see cref="Node._Ready"/>
	/// method called, only the window itself.
	/// </para>
	/// </summary>
	public Window CreateWindowIntoRoot(WindowType windowType) {

		GD.Print($"Window {windowType} requested into root.");

		Window window = InstantiateWindowScene(windowType);
		
		// Guards
		if (window.GetChildCount() != 1) {
			throw new NotSupportedException("Only windows with 1 child can be promoted.");
		}

		if (GetWindow().GetScript().VariantType != Variant.Type.Nil) {
			throw new InvalidOperationException("The root window already has a script. Another window might have been promoted.");
		}

		// Find root window
		Window rootWindow = this.GetWindow();

		// Move children
		Node child = window.GetChild(0);
		var deepChildren = child.FindChildren("*");
		foreach (var deepChild in deepChildren) deepChild.Owner = child; // temporarily set owner to be transfer node 

		child.Owner = null;
		window.RemoveChild(child);
		rootWindow.AddChild(child);

		foreach (var deepChild in deepChildren) deepChild.Owner = rootWindow;

		// Move script, provide core node to the new instance
		Script windowScript = window.GetScript().As<Script>();
		rootWindow.SetScript(windowScript);
		rootWindow = this.GetWindow(); // refresh the C# object reference

		foreach (Godot.Collections.Dictionary property in windowScript.GetScriptPropertyList()) {
			if (
				property["usage"]
					.As<PropertyUsageFlags>()
					.HasFlag(PropertyUsageFlags.ScriptVariable)
			) {
				StringName propertyName = property["name"].AsStringName();
				rootWindow.Set(propertyName, window.Get(propertyName));
			}
		}

		// Enable root event processing
		rootWindow.SetProcessInput(true);
		rootWindow.SetProcess(true);
		rootWindow.SetProcessShortcutInput(true);

		// Call ready
		rootWindow._Ready();
		
		// Cleanup and return
		window.QueueFree();
		return rootWindow;
	}
	
}
