using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TypeToSquad.Model;


/// <summary>
/// A node that manages playback of spoken messages.
/// Is a <see cref="Node"/> to better interact with the Godot engine.
/// </summary>
/// <remarks>
/// Expects to have no children, other than
/// nodes created by itself.
/// </remarks>
public partial class AudioManagerNode : Node, IRefrencesCore {

	#region //// Core Node

	CoreNode? _coreNode = null;

	public CoreNode CoreNode => _coreNode ?? throw new CoreNodeNullException();

	public void RecieveCoreReference(CoreNode core) => _coreNode = core;

	#endregion

	public void SetOutputDeviceFromSettings() {
		AudioServer.OutputDevice = CoreNode.UserSettings.Device;
	}

	/// <summary>
	/// Plays PCM Wav data in a new <see cref="AudioStreamPlayer"/>.
	/// </summary>
	public void PlayNew(byte[] pcmWavData) {

		// Create stream
		const int wavImportCompressModePcm = 0;
		const int wavImportLoopModeDisabled = 1;

		AudioStreamWav newStream = AudioStreamWav.LoadFromBuffer(
										pcmWavData,
										new Godot.Collections.Dictionary {
											["compress/mode"] = wavImportCompressModePcm,
											["edit/loop_mode"] = wavImportLoopModeDisabled,
										}
									);

		// Create player
		AudioStreamPlayer player = new();
		player.Finished += () => OnPlaybackFinished(player);
		this.AddChild(player);
		
		player.Stream = newStream;
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

		// Stop playblack in case of interruption
		playbackNode.Stop();

		// Free the steam
		var stream = playbackNode.Stream;
		playbackNode.Stream = null;
		//stream?.Free(); // ref counted
		stream?.Dispose();

		// Remove the node
		this.RemoveChild(playbackNode);
		playbackNode.QueueFree();
	}


	/// <summary>
	/// Checks and insure the number of currently playing streams
	/// is within limits.
	/// </summary>
	public void EnsureConcurrentNodeMax() {
		int maxChildren = CoreNode.UserSettings.MaxConcurrentStreams;
		while (this.GetChildCount() > maxChildren) {
			var oldestNode = this.GetChild<AudioStreamPlayer>(0);
			oldestNode.Stop();
			OnPlaybackFinished(oldestNode);
		}
	}

}
