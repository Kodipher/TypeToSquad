using System;
using Kodipher.TypeToSqaud.Modules.Speech;


namespace Kodipher.TypeToSqaud.Modules.Configuration;


public class Configuration {

	/// <summary>Name of the current output device</summary>
	public readonly FieldOptions<string> Device = new(
		SpeechController.GetOutputDevices,
		defaultValue: SpeechController.defaultDeviceName
	);

	/// <summary>Name of the current tts voice</summary>
	public readonly FieldOptions<string> Voice = new(
		OperatingSystem.IsOSPlatform("windows") ? SpeechController.GetVoices : () => Array.Empty<string>(),
		defaultValue: ""
	);

	/// <summary>The max number of tts outputs that are played at the same time</summary>
	public readonly FieldIntRange MaxConcurentStreams = new(1, 64, defaultValue: 6);

}
