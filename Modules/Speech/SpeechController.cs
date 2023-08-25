using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Speech.Synthesis;
using NAudio.CoreAudioApi;


namespace Kodipher.TypeToSquad.Modules.Speech;


[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public partial class SpeechController : Node {

	#region //// Config options

	/// <summary>
	/// Returns a list of usable TTS voice names
	/// </summary>
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

	public readonly static MMDeviceSelectionInfo defaultDevice = new() { Name = "Default", ID = "{}.{}" };

	/// <summary>
	/// Returns a list of usable output devices (FriendlyNames and IDs).
	/// Prepends "Default" as the first entry for default device selection.
	/// </summary>
	public static IEnumerable<MMDeviceSelectionInfo> GetOutputDevices() {
		using MMDeviceEnumerator enumerator = new();
		return
			enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(device => new MMDeviceSelectionInfo { Name = device.FriendlyName, ID = device.ID })
			.Prepend(defaultDevice)
			.ToArray();
	}

	#endregion

}
