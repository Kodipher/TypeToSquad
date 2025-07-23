

namespace TypeToSquad.Model.Settings;


public record class UserSettings {

	// ===== ===== ===== GENERAL ===== ===== =====

	/// <summary>Wether to show advanced settings.</summary>
	public readonly Field<bool> UseAdvancedSettings = new(false);

	/// <summary>Wether to monitor the log file for errors.</summary>
	public readonly Field<bool> EnableErrorMonitoring = new(true);


	// ===== ===== ===== VOICE ===== ===== =====

	/// <summary>Name of the current tts voice.</summary>
	public readonly FieldOptionsRuntime Voice = new();

	/// <summary>The relative pitch of the voice.</summary>
	public readonly FieldNumericRange<double> VoicePitch = new(0, 2, defaultValue: 1);

	/// <summary>The speaking rate (speed multiplier) of the voice.</summary>
	public readonly FieldNumericRange<double> VoiceRate = new(0.5, 6, defaultValue: 1);


	// ===== ===== ===== INPUT ===== ===== =====

	/// <summary>Number of previous inputs held in memory.</summary>
	public readonly FieldNumericRange<int> HistorySlots = new(0, short.MaxValue, defaultValue: 32);


	// ===== ===== ===== AUDIO ===== ===== =====

	/// <summary>Name of the current output device.</summary>
	public readonly FieldOptionsRuntime Device = new();

	/// <summary>The volume of the voice.</summary>
	public readonly FieldNumericRange<double> SynthesisVolume = new(0, 1, defaultValue: 1);

	/// <summary>Max number of outputs played at the same time.</summary>
	public readonly FieldNumericRange<int> MaxConcurrentStreams = new(1, 64, defaultValue: 6);

}
