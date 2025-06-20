using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad;


public enum WindowType {
	Unknown,
	Main
}


public partial class WindowManager : Node, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode core) => CoreNode = core;

	#endregion

	#region //// Window Scenes

	[Export]
	public Godot.Collections.Dictionary<WindowType, PackedScene?> WindowScenes { get; set; } = [];

	/// <summary>
	/// Creates an instance of one of the window scenes.
	/// Provides the <see cref="TypeToSquad.CoreNode"/> reference
	/// to the root node of the window, if its <see cref="IRefrencesCore"/>.
	/// </summary>
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

}
