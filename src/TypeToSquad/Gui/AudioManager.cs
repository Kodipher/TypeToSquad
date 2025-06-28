using Godot;
using System;
using System.Collections.Generic;

using TypeToSquad.Model;
using TypeToSquad.Model.Settings;
using TypeToSquad.Utils;


namespace TypeToSquad.Gui;


public partial class AudioManager : Node, IRefrencesCore {

	#region //// Core Node

	public CoreNode? CoreNode { get; set; } = null;

	public void RecieveCoreReference(CoreNode? core) => CoreNode = core;

	#endregion



}
