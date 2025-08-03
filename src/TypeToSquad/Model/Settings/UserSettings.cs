

namespace TypeToSquad.Model.Settings;


/// <summary>
/// A storage for all user settings.
/// </summary>
/// <remarks>
/// Only fields implementing <see cref="IVariantSavable"/> are saved.
/// Properties are ignored.
/// </remarks>
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

	/// <summary>A table of voices change hints and voices they correspond to.</summary>
	public readonly Table<(string hint, string voiceName)> VoiceChanges = new();


	// ===== ===== ===== INPUT ===== ===== =====

	/// <summary>Number of previous inputs held in memory.</summary>
	public readonly FieldNumericRange<int> HistorySlots = new(0, short.MaxValue, defaultValue: 32);

	/// <summary>A table of text replacements to perform. Patterns are written using regex.</summary>
	public readonly Table<(string context, string pattern, string replacement)> TextReplacements = new();


	// ===== ===== ===== AUDIO ===== ===== =====

	/// <summary>Name of the current output device.</summary>
	public readonly FieldOptionsRuntime Device = new();

	/// <summary>The volume of the voice.</summary>
	public readonly FieldNumericRange<int> SynthesisVolumePercent = new(0, 100, defaultValue: 100);

	/// <summary>Max number of outputs played at the same time.</summary>
	public readonly FieldNumericRange<int> MaxConcurrentStreams = new(1, 64, defaultValue: 6);

}
