using Godot;
using System;
using System.Linq;

using TypeToSquad.Utils;
using Rephidock.GeneralUtilities.Collections;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace TypeToSquad.Gui.WindowScenes;


public partial class SettingsWindow : Window, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion


}
