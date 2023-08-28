using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Speech.Synthesis;
using NAudio.CoreAudioApi;

using Kodipher.TypeToSquad.Modules.Configuration;


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

		// System guard
		if (!OperatingSystem.IsOSPlatform("windows")) throw new PlatformNotSupportedException();

		using MMDeviceEnumerator enumerator = new();
		return
			enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(device => new MMDeviceSelectionInfo { Name = device.FriendlyName, ID = device.ID })
			.Prepend(defaultDevice)
			.ToArray();
	}

	#endregion

	#region //// Active speech storage

	//
	// When ActiveSpeech objects are created they are placed in activeStreams
	// When ActiveSpeech are forcefully shut they are put in shutStreams
	// When ActiveSpeech are finished in either list they are diposed of
	//

	readonly LinkedList<ActiveSpeech> activeStreams = new();

	readonly LinkedList<ActiveSpeech> shutStreams = new();

	private void AddActiveSpeech(ActiveSpeech speech) {

		// Ready guard
		if (!IsNodeReady()) {
			speech.Dispose();
			throw new InvalidOperationException("Cannot add active speech: Node is not ready.");
		}

		// Double check completed
		CheckSpeechesForDisposal();

		// Remove the oldest speeches if going over the threshold
		int maxConcurentStreams = configManager.CurrentConfig.MaxConcurrentStreams.Value;
		if (activeStreams.Count >= maxConcurentStreams) {

			int streamsToClear = activeStreams.Count + 1 - maxConcurentStreams;

			for (int i = 0; i < streamsToClear; i++) {
				ShutOldestSpeech();
			}

		}

		// Add current stream at the end
		activeStreams.AddLast(speech);
	}

	private void ShutOldestSpeech() {

		// Find first in the list -- oldest;
		ActiveSpeech? streamToEnd = activeStreams.First?.Value;
		if (streamToEnd is null) return;

		// If exists: shut
		streamToEnd.Shut();
		activeStreams.Remove(streamToEnd);
		shutStreams.AddLast(streamToEnd);
	}

	private void CheckSpeechesForDisposal() {

		LinkedListNode<ActiveSpeech>? currentNode;

		// Check every active speech if it is ready for disposal
		currentNode = activeStreams.First;
		while (currentNode is not null) {
			if (currentNode.Value.IsReadyForDisposal) {
				// If it is - dispose
				LinkedListNode<ActiveSpeech>? nextNode = currentNode.Next;
				activeStreams.Remove(currentNode);
				currentNode.Value.Dispose();
				currentNode = nextNode;
			} else {
				currentNode = currentNode.Next;
			}
		}

		// Check every shut node if it is ready for disposal
		currentNode = shutStreams.First;
		while (currentNode is not null) {
			if (currentNode.Value.IsReadyForDisposal) {
				// If it is - dispose
				LinkedListNode<ActiveSpeech>? nextNode = currentNode.Next;
				shutStreams.Remove(currentNode);
				currentNode.Value.Dispose();
				currentNode = nextNode;
			} else {
				currentNode = currentNode.Next;
			}
		}

	}

	#endregion

	#region //// Perform synthesis

	/// <summary>
	/// Perform speech synthesis.
	/// </summary>
	/// <param name="text">Text to speak</param>
	public void PerformSynthesis(string text) {

		// System guard
		if (!OperatingSystem.IsOSPlatform("windows")) throw new PlatformNotSupportedException();

		// Ready guard
		if (!IsNodeReady()) {
			throw new InvalidOperationException("Cannot perform syntehsis: Node is not ready.");
		}

		GD.Print($"Speaking \"{text}\"");

		// Setup active speech
		MMDevice device;
		using (MMDeviceEnumerator enumerator = new()) {

			string deviceID = configManager.CurrentConfig.Device.DeviceID;

			if (deviceID == defaultDevice.ID) {
				device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
			} else {
				device = enumerator.GetDevice(deviceID);
			}
		}

		ActiveSpeech speech = new(device);

		// Play
		// todo
		//outputDevice.Init(audioFile);
		//outputDevice.Play();

		// Add active speech to storage
		AddActiveSpeech(speech);
		speech.OnReadyForDisposal += (object? _sender, EventArgs _) => CheckSpeechesForDisposal();

	}

	/// <summary>
	/// Removes all currently playing speeches.
	/// </summary>
	public void Shut() {
		GD.Print("Shutting up");

		while (activeStreams.Count > 0) {
			ShutOldestSpeech();
		}

		CheckSpeechesForDisposal();
	}

	#endregion

	ConfigurationManager configManager = null!;

	public override void _Ready() {

		// Find related nodes
		configManager = GetNode<ConfigurationManager>("%ConfigurationManager");

	}

}
