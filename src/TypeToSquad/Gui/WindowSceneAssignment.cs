using Godot;
using System;


namespace TypeToSquad.Gui;


public enum WindowType {
	Unknown,
	Main,
	UnusedSlotDebug,
	SimpleSettings,
	Settings,
	Shortcuts,
	EditReplacements,
	EditVoiceChanges,
	EditSoundEffects,
	EditUserTags
}


[GlobalClass]
public partial class WindowSceneAssignment : Resource {
	
	[Export] 
	public Godot.Collections.Dictionary<WindowType, PackedScene?> Scenes { get; set; } = [];
	    
}
