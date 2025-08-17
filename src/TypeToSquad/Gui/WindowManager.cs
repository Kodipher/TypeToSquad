using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad.Gui;


public enum WindowType {
	Unknown,
	Main,
	Debug,
	SimpleSettings,
	Settings,
	Shortcuts,
	EditReplacements,
	EditVoiceChanges
}


public partial class WindowManager : Node, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Window Scenes

	[Export]
	public Godot.Collections.Dictionary<WindowType, PackedScene?> WindowScenes { get; set; } = [];

	/// <summary>
	/// Creates an instance of one of the window scenes.
	/// Provides the <see cref="TypeToSquad.CoreNode"/> reference
	/// to the root node of the window, if it's <see cref="IRefrencesCore"/>.
	/// Only the root node is checked.
	/// </summary>
	/// <remarks>
	/// <see cref="Node._Ready"/> is not called by this method,
	/// as this method does not add the <see cref="Window"/> into the scene tree.
	/// </remarks>
	Window InstantiateWindowScene(WindowType windowType) {

		// Get window
		PackedScene? windowScene = WindowScenes.GetValueOrDefault(windowType, null);
		if (windowScene is null) {
			throw new InvalidOperationException($"No scene set for window of type {windowType}");
		}

		// Instantiate
		Node windowSceneRoot = windowScene.Instantiate();

		if (windowSceneRoot is not Window) {
			throw new InvalidOperationException($"Scene set for window of type {windowType} must have a Window node as root.");
		}

		Window window = (Window)windowSceneRoot;

		// Provide Core
		if (windowSceneRoot is IRefrencesCore windowRootWithInterfaced) {
			if (CoreNode is not null) windowRootWithInterfaced.RecieveCoreReference(CoreNode);
		}

		return window;
	}

	#endregion

	/// <summary>
	/// <para>
	/// Creates a window of type <paramref name="windowType"/>
	/// and transfters the script, script proprties and
	/// children into root window.
	/// (as seen by <see cref="CoreNode"/>).
	/// </para>
	/// <para>
	/// The created window must have a single child,
	/// that does not have <see cref="Node.UniqueNameInOwner"/> set,
	/// to facilitate transfer.
	/// </para>
	/// <para>
	/// Because root must be ready by the point this is called,
	/// the <see cref="Node._Ready"/> of the window is not called automatically.
	/// </para>
	/// </summary>
	/// <returns>The root window, as the newly created window.</returns>
	public Window CreateWindowIntoRoot(WindowType windowType) {

		GD.Print($"Window {windowType} requested into root.");

		Window window = InstantiateWindowScene(windowType);

		// Guards
		if (window.GetChildCount() != 1) {
			throw new NotSupportedException("Only windows with 1 child can be unpacked.");
		}

		if (GetWindow().GetScript().VariantType != Variant.Type.Nil) {
			throw new InvalidOperationException("The root window already has a script. Another window might have been created this way.");
		}

		if (CoreNode is null) throw new InvalidOperationException("Core node must be set.");

		// Find root window
		Window rootWindow = CoreNode.GetWindow();

		// Move children
		Node child = window.GetChild(0);
		var deepChildren = child.FindChildren("*");
		foreach (var deepChild in deepChildren) deepChild.Owner = child;

		child.Owner = null;
		window.RemoveChild(child);
		rootWindow.AddChild(child);

		foreach (var deepChild in deepChildren) deepChild.Owner = rootWindow;

		// Move script, provide core node to the new instance
		Script windowScript = window.GetScript().As<Script>();
		rootWindow.SetScript(windowScript);
		rootWindow = CoreNode.GetWindow(); // refresh the C# object reference

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

		if (rootWindow is IRefrencesCore windowRootWithInterfaced) {
			if (CoreNode is not null) windowRootWithInterfaced.RecieveCoreReference(CoreNode);
		}

		// Enable root event processing
		rootWindow.SetProcessInput(true);
		rootWindow.SetProcess(true);
		rootWindow.SetProcessShortcutInput(true);

		// Cleanup and return
		window.QueueFree();
		return rootWindow;
	}


	readonly Dictionary<WindowType, Window> currentChildrenByType = new();

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
	/// if it exsists as a child of the manager.
	/// Returns <see langword="null"/> if the window does not exit.
	/// </summary>
	public Window? GetExistingWindowAtSelf(WindowType windowType) {
		if (currentChildrenByType.TryGetValue(windowType, out Window? existingWindow)) {
			return existingWindow;
		}
		return null;
	}

}
