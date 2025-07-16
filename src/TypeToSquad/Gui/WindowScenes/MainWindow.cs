using Godot;
using System;
using System.Linq;

using TypeToSquad.Model;
using TypeToSquad.Utils;
using Rephidock.GeneralUtilities.Collections;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindow : WindowEx, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion

	#region //// Nodes

	// All set in FindNodes, which is called in _Ready
	BaseButton speakButton = null!;
	BaseButton shutButton = null!;
	TextEdit messageTextEdit = null!;

	BaseButton settingsButton = null!;

	BaseButton errorIndicator = null!;

	void FindNodes() {
		speakButton = this.GetNodeNotNull<BaseButton>("%SpeakButton");
		shutButton = this.GetNodeNotNull<BaseButton>("%ShutButton");
		messageTextEdit = this.GetNodeNotNull<TextEdit>("%MessageTextEdit");

		settingsButton = this.GetNodeNotNull<BaseButton>("%SettingsButton");

		errorIndicator = this.GetNodeNotNull<BaseButton>("%ErrorIndicator");
	}

	#endregion

	public override void _Ready() {
		base._Ready();

		FindNodes();

		// Init error indicator
		errorIndicator.Hide();
		if (CoreNode is not null) {
			CoreNode.LogMonitor.OnErrorFound += errorIndicator.Show;
		}

		// Connect button signals
		speakButton.Pressed += OnSpeakPressed;
		shutButton.Pressed += OnShutPressed;
		settingsButton.Pressed += OnSettingsPressed;
		errorIndicator.Pressed += OnErrorIndicatorPressed;

		// Core check
		if (CoreNode is null) GD.PushError("Main Window has not been provided with CoreNode.");
	}

	public void OnSettingsPressed() {
		if (CoreNode is null) return;

		var windowType = CoreNode.UserSettings.UseAdvancedSettings ? WindowType.AdvancedSettings : WindowType.Settings;
		CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType);
	}

	public void OnErrorIndicatorPressed() {
		if (CoreNode is null) return;

		GD.Print("Opening log file.");
		errorIndicator.Hide();

		CoreNode.LogMonitor.SeekToLogEnd();
		CoreNode.LogMonitor.BlockChecks = false;
		OS.ShellOpen(LogMonitor.GetLogfilePath());
	}

	public void OnSpeakPressed() {
		if (CoreNode is null) return;

		GD.Print("Speaking.");
		CoreNode.LogMonitor.CheckLog();
	}

	public void OnShutPressed() {
		if (CoreNode is null) return;

		GD.Print("Shutting.");
	}

}
