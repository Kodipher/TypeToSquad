using Godot;
using System;
using System.Linq;

using TypeToSquad.Utils;
using Rephidock.GeneralUtilities.Collections;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


public partial class MainWindowCore : Control, IRefrencesCore {

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
		FindNodes();

		errorIndicator.Hide();

		settingsButton.Pressed += () => {
			if (CoreNode is null) return;
			var windowType = CoreNode.UserSettings.UseAdvancedSettings ? WindowType.AdvancedSettings : WindowType.Settings;
			CoreNode.WindowManager.CreateWindowAtSelfUnique(windowType); 
		};
	}

}
