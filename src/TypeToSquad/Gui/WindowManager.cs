using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad.Gui;


public enum WindowType {
	Unknown,
	Main,
	Debug,
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
	/// Creates a window of type <paramref name="windowType"/>
	/// and unpacks the contents into <paramref name="unpackDestination"/>.
	/// The child of the window will be the child of the destination node.
	/// No window properties are copied.
	/// Also provides the <see cref="TypeToSquad.CoreNode"/> reference
	/// to the child of the root node if it's <see cref="IRefrencesCore"/>.
	/// </summary>
	/// <remarks>
	/// Only script-less windows are supported.
	/// Only windows with 1 child node are supported.
	/// </remarks>
	public void CreateWindowUnpacked(WindowType windowType, Node unpackDestination) {
		Window window = InstantiateWindowScene(windowType);

		// Guards
		ArgumentNullException.ThrowIfNull(unpackDestination);

		if (window.GetChildCount() != 1) {
			throw new NotSupportedException("Only windows with 1 child can be unpacked.");
		}

		if (window.GetScript().VariantType != Variant.Type.Nil) {
			throw new NotSupportedException("Only windows without a script can be unpacked.");
		}

		// Unpack
		Node child = window.GetChild(0);
		foreach (var deepChild in child.FindChildren("*")) deepChild.Owner = child;

		child.Owner = null;
		window.RemoveChild(child);
		unpackDestination.AddChild(child);

		// Provide core
		if (child is IRefrencesCore childWithCode) childWithCode.RecieveCoreReference(CoreNode);

		// Cleanup
		window.Free();
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
			existingWindow.GrabFocus();
			return existingWindow;
		}

		// Create new
		Window newWindow = InstantiateWindowScene(windowType);
		currentChildrenByType.Add(windowType, newWindow);
		newWindow.TreeExiting += () => currentChildrenByType.Remove(windowType);

		return newWindow;
	}

}
