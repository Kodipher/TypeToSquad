using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model;


/// <summary>Manages playback of spoken messages.</summary>
public partial class AudioManager : Node {

	#region /--- Singleton ---/

	public static AudioManager Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion

	public override void _Ready() {
		StageSingletonInstance();
		InitOutputDeviceSetting();
	}

	public void InitOutputDeviceSetting() {
		var settingsInstance = UserSettingsManager.Instance.Settings;
		settingsInstance.Device.SetOptions(AudioServer.GetOutputDeviceList());
		AudioServer.OutputDevice = settingsInstance.Device;
	}

	/// <remarks>Assumes exclusive ownership of <paramref name="stream."/></remarks>
	public void PlayNew(AudioStream stream) {

		// Create player
		AudioStreamPlayer player = new();
		player.Finished += () => OnPlaybackFinished(player);
		this.AddChild(player);
		
		player.Stream = stream;
		player.Play();

		// Check max
		EnsureConcurrentNodeMax();
	}

	/// <summary>Stops all currently playing audio.</summary>
	public void StopAll() {
		foreach (var playbackNode in GetChildren().Cast<AudioStreamPlayer>()) {
			playbackNode.Stop();
			OnPlaybackFinished(playbackNode);
		}
	}


	void OnPlaybackFinished(AudioStreamPlayer playbackNode) {

		// Guards
		if (!IsInstanceValid(playbackNode)) return;
		if (playbackNode.GetParent() != this) return;

		// Stop playback in case of interruption
		playbackNode.Stop();

		// Free the steam
		var stream = playbackNode.Stream;
		playbackNode.Stream = null;
		//stream?.Free(); // ref counted
		//stream?.Dispose();
		
		// Remove the node
		this.RemoveChild(playbackNode);
		playbackNode.QueueFree();
	}


	/// <summary>
	/// Checks and ensures that
	/// the number of currently playing streams is within limits.
	/// </summary>
	public void EnsureConcurrentNodeMax() {
		int maxChildren = UserSettingsManager.Instance.Settings.MaxConcurrentStreams;
		while (this.GetChildCount() > maxChildren) {
			var oldestNode = this.GetChild<AudioStreamPlayer>(0);
			oldestNode.Stop();
			OnPlaybackFinished(oldestNode);
		}
	}

}
