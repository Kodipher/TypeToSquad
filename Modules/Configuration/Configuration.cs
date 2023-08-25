using System;
using Kodipher.TypeToSquad.Modules.Speech;


namespace Kodipher.TypeToSquad.Modules.Configuration;


public class Configuration {

	/// <summary>Name of the current output device</summary>
	public readonly FieldMMDevice Device =
		OperatingSystem.IsOSPlatform("windows") ?
		new(
			SpeechController.GetOutputDevices, defaultDevice: SpeechController.defaultDevice
		) : new(
			() => Array.Empty<MMDeviceSelectionInfo>(), new MMDeviceSelectionInfo() { ID="", Name="" }
		);

	/// <summary>Name of the current tts voice</summary>
	public readonly FieldOptions<string> Voice = new(
		OperatingSystem.IsOSPlatform("windows") ? SpeechController.GetVoices : () => Array.Empty<string>(),
		defaultValue: ""
	);

	/// <summary>The max number of tts outputs that are played at the same time</summary>
	public readonly FieldIntRange MaxConcurrentStreams = new(1, 64, defaultValue: 6);

	/// <summary>The number of previous inputs held in memory</summary>
	public readonly FieldIntRange HistorySlots = new(0, short.MaxValue, defaultValue: 32);

}
