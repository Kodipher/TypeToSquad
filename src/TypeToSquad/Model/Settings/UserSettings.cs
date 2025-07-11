using Godot;
using System;


namespace TypeToSquad.Model.Settings;


public record class UserSettings {

	/// <summary>Wether to show advanced settings.</summary>
	public readonly Field<bool> UseAdvancedSettings = new(false);

	/// <summary>Name of the current output device.</summary>
	public readonly FieldOptionsRuntime Device = new();

	/// <summary>Name of the current tts voice.</summary>
	public readonly FieldOptionsRuntime Voice = new();

	/// <summary>Max number of outputs played at the same time.</summary>
	public readonly FieldIntRange MaxConcurrentStreams = new(1, 64, defaultValue: 6);

	/// <summary>Number of previous inputs held in memory.</summary>
	public readonly FieldIntRange HistorySlots = new(0, short.MaxValue, defaultValue: 32);

}
