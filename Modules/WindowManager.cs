using System;
using System.Collections.Generic;

using Godot;
using Kodipher.TypeToSqaud.Modules.Configuration;
using Kodipher.TypeToSqaud.Panels.Config;
using Kodipher.TypeToSqaud.Utils;


namespace Kodipher.TypeToSqaud.Modules;


#nullable enable

public partial class WindowManager : Node {

	#region //// WindowInfo

	public enum Windows {
		Config,
		Info
	}

	public record WindowInfo {

		// Info that does not change (all required)
		public string NodeName { get; init; } = default!;
		public string WindowName { get; init; } = default!;
		public Vector2I WindowSize { get; init; } = default;
		public string PanelPath { get; init; } = default!;

		// Info that does change
		public Action<Window, Node> InitMethod { get; set; } = delegate (Window windowRef, Node panelRef) { };
		public PackedScene? PanelScene { get; set; } = null;
		public Window? WindowNode { get; set; } = null;

		// Shortcuts
		public bool IsCreated => WindowNode is not null;

	}

	#endregion

	#region //// Window storage and settings

	readonly static Theme main_theme = GD.Load<Theme>("res://assets/theme/main_theme.tres");

	public readonly IReadOnlyDictionary<Windows, WindowInfo> windowData = new Dictionary<Windows, WindowInfo>() {

		{
			Windows.Config,
			new WindowInfo {
				NodeName = "ConfigWindow",
				WindowName = "Configure",
				WindowSize = new Vector2I(600, 300),
				PanelPath = "res://Panels/Config/scn_config_panel.tscn",
			}
		}

	}.AsReadOnly();

	void ConfigureInitMethods() {

		windowData[Windows.Config].InitMethod = delegate (Window windowRef, Node panelRef) {

			ConfigPanel configPanelRef = (panelRef as ConfigPanel)!;

			// Give config reference
			var configRef = configManager.CurrentConfig;
			configPanelRef.Ready += delegate { configPanelRef.PropogateConfigurationReference(configRef); };

			// Closing window
			void ConfigSaveAndClose() {
				configManager.SaveCurrentConfig();
				DestroyWindow(Windows.Config);
			};

			windowRef.CloseRequested += ConfigSaveAndClose;
			configPanelRef.ClosePressed += ConfigSaveAndClose;

		};

	}

	#endregion

	#region //// Window creation and destruction

	/// <summary>
	/// Creates a specific window.
	/// If the window is already created it grabs focus instead.
	/// </summary>
	/// <param name="windowType"></param>
	public void CreateWindow(Windows windowType) {

		WindowInfo infoRef = windowData[windowType];

		// Do not create the same window twice
		// Instead grab focus
		if (infoRef.IsCreated) {
			infoRef.WindowNode!.GrabFocus();
			return;
		}

		// Load scene
		infoRef.PanelScene ??= GD.Load<PackedScene>(infoRef.PanelPath);

		// Create the window
		Window newWindow = new() {
			Name = infoRef.WindowName,
			Size = infoRef.WindowSize,
			MinSize = new Vector2I(160, 90),
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Transient = true,
			Theme = main_theme
		};

		Node windowPanel = infoRef.PanelScene!.Instantiate();
		newWindow.AddChild(windowPanel);

		// Init window 
		infoRef.InitMethod(newWindow, windowPanel);

		// Connect window
		AddChild(newWindow);
		infoRef.WindowNode = newWindow;

	}

	/// <summary>
	/// Destroys specified window.
	/// </summary>
	/// <param name="windowType"></param>
	public void DestroyWindow(Windows windowType) {
		WindowInfo infoRef = windowData[windowType];
		if (infoRef.IsCreated) {
			infoRef.WindowNode!.QueueFree();
			infoRef.WindowNode = null;
		}
	}

	#endregion

	ConfigurationManager configManager = default!;

	public override void _Ready() {

		// Find related nodes
		configManager = GetNode<ConfigurationManager>("%ConfigurationManager");

		// Finish info
		ConfigureInitMethods();

	}

}

#nullable restore
