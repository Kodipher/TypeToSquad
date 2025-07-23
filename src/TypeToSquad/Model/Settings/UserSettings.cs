

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


	// ===== ===== ===== INPUT ===== ===== =====

	/// <summary>Number of previous inputs held in memory.</summary>
	public readonly FieldNumericRange<int> HistorySlots = new(0, short.MaxValue, defaultValue: 32);


	// ===== ===== ===== AUDIO ===== ===== =====

	/// <summary>Name of the current output device.</summary>
	public readonly FieldOptionsRuntime Device = new();

	/// <summary>Max number of outputs played at the same time.</summary>
	public readonly FieldNumericRange<int> MaxConcurrentStreams = new(1, 64, defaultValue: 6);

}
