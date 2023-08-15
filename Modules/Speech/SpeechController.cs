using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using NAudio.CoreAudioApi;


namespace Kodipher.TypeToSqaud.Modules.Speech;


public partial class SpeechController : Godot.Node {

	#region //// Config options

	/// <summary>
	/// Returns a list of usable TTS voice names
	/// </summary>
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	public static IEnumerable<string> GetVoices() {

		if (!OperatingSystem.IsOSPlatform("windows")) throw new PlatformNotSupportedException();
		
		using SpeechSynthesizer synthesizer = new();
		SpeechApiReflectionHelper.InjectOneCoreVoices(synthesizer);
		return
			synthesizer
			.GetInstalledVoices()
			.Where(voice => voice.Enabled)
			.Select(voice => voice.VoiceInfo.Name)
			.ToArray();
	}

	public const string defaultDeviceName = "Default";

	/// <summary>
	/// Returns a list of usable output devices (FriendlyNames).
	/// Prepends "Default" as the first entry for default device selection.
	/// </summary>
	public static IEnumerable<string> GetOutputDevices() {
		using MMDeviceEnumerator enumerator = new();
		return
			enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(device => device.FriendlyName)
			.Prepend(defaultDeviceName)
			.ToArray();
	}

	#endregion

}
