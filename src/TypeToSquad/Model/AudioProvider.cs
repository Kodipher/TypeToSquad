using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TypeToSquad.Model.Markup;

using SynthesizeRequest = WinRTSpeechSynthServer.Protocol.Messages.SynthesizeRequest;
using SynthesisResultResponse = WinRTSpeechSynthServer.Protocol.Messages.SynthesisResultResponse;


namespace TypeToSquad.Model;


/// <summary>
/// Converts <see cref="RenderNode"/>s of known types
/// gotten from <see cref="MessageProcessor.ProcessInitialNodeTree"/>
/// into <see cref="AudioStream"/>s.
/// </summary>
/// <remarks>
///	May sent requests via <see cref="SpeechDaemon"/>.
/// </remarks>
public partial class AudioProvider : Node {
	
	#region /--- Singleton ---/

	public static AudioProvider Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion
	
	public override void _Ready() {
		StageSingletonInstance();
	}

	public void CreateStream(RenderNode root, Action<AudioStream> callback) {

		if (
			root.Type == RenderNodeType.Text ||
			root.Type == RenderNodeType.SsmlRoot
		) {
			CreateStreamFromTextOrSsml(root, callback);
		} 
		
		else if (root.Type == RenderNodeType.Sound) {
			CreateStreamFromSound(root, callback);
		}
		
		else if (root.Type == RenderNodeType.Serial) {
			CreateStreamFromSerial(root, callback);
		}
		
		else {
			throw new NotSupportedException($"Unsupported node type \"{root.Type}\"");
		}
		
	}
	
	public void CreateStreamFromTextOrSsml(RenderNode textOrSpeakNode, Action<AudioStream> callback) {

		// Guards
		if (!(textOrSpeakNode.Type == RenderNodeType.Text || textOrSpeakNode.Type == RenderNodeType.SsmlRoot)) {
			throw new ArgumentException($"Incorrect node type. Got {textOrSpeakNode.Type}.", nameof(textOrSpeakNode));
		}
		
		// Request
		var settingsInstance = UserSettingsManager.Instance.Settings;
		
		SynthesizeRequest synthRequest = new SynthesizeRequest() {
			InputString = MessageProcessor.StringifyNodeRecursive(textOrSpeakNode, indented: false),
			IsSsml = textOrSpeakNode.Type == RenderNodeType.SsmlRoot,
			VoiceName = DaemonVoiceStorage.Instance.GetVoiceByKey(settingsInstance.VoiceKey).Name,
			Pitch = settingsInstance.VoicePitch,
			Rate = settingsInstance.VoiceRate,
			Volume = settingsInstance.SynthesisVolumePercent / 100.0
		};

		SpeechDaemon.Instance.DispatchRequest<SynthesisResultResponse>(
			synthRequest,
			(response) => {

				// Voice does not exist
				if (!response.GivenVoiceExists) {
					GD.PushError("Selected voice does not exist.");
					var voiceField = settingsInstance.VoiceKey;
					voiceField.Value = voiceField.DefaultValue;
				}

				callback(CreateStreamFromDaemonResponse(response));
			}
		);
		
	}

	public void CreateStreamFromSound(RenderNode soundNode, Action<AudioStream> callback) {
		
		// Guards
		if (soundNode.Type != RenderNodeType.Sound) {
			throw new ArgumentException($"Incorrect node type. Got {soundNode.Type}.", nameof(soundNode));
		}

		// Find sound file
		var settingsInstance = UserSettingsManager.Instance.Settings;

		string hint = soundNode.Attributes.GetValueOrDefault(RenderNodeAttribute.SoundHint, "");
		
		(string hint, string path, int volumePercent)? soundRow = settingsInstance
			.SoundEffects
			.Select(row => row as (string hint, string, int)?)
			.FirstOrDefault(row => row!.Value.hint == hint, null);

		if (soundRow is null) {
			throw new InvalidOperationException($"Unknown sound effect \"{hint}\"");
		}
		
		// Path check
		string path = soundRow.Value.path;
		if (!File.Exists(path)) {
			throw new FileNotFoundException($"File not found at \"{path}\" for sound effect \"{hint}\"");
		}
		
		// Load external
		static AudioStream WrapStreamWithVolume(AudioStream stream, double volumeMult) {
			throw new NotImplementedException();
		}
		
		string extension = (Path.GetExtension(path) ?? "").ToLower();
		switch (extension) {

			case ".wav":
			case ".wave": {
				var stream = AudioStreamWav.LoadFromFile(path);
				double volumeMultiplier = soundRow.Value.volumePercent / 100.0;
				
				if (volumeMultiplier >= 1) {
					callback(stream);
					break;
				}

				callback(WrapStreamWithVolume(stream, volumeMultiplier));
			} break;
			
			case ".ogg": {
				var stream = AudioStreamOggVorbis.LoadFromFile(path);
				double volumeMultiplier = soundRow.Value.volumePercent / 100.0;
				
				if (volumeMultiplier >= 1) {
					callback(stream);
					break;
				}

				callback(WrapStreamWithVolume(stream, volumeMultiplier));
			} break;
			
			case ".mp3": {
				var stream = AudioStreamMP3.LoadFromFile(path);
				double volumeMultiplier = soundRow.Value.volumePercent / 100.0;
				
				if (volumeMultiplier >= 1) {
					callback(stream);
					break;
				}

				callback(WrapStreamWithVolume(stream, volumeMultiplier));
			} break;
			
			default:
				throw new NotSupportedException($"File type \"{extension}\" of sound effect \"{hint}\" is not supported.");
		}
		
	}
	
	public void CreateStreamFromSerial(RenderNode serialNode, Action<AudioStream> callback) {
		
		// Guards
		if (serialNode.Type != RenderNodeType.Serial) {
			throw new ArgumentException($"Incorrect node type. Got {serialNode.Type}.", nameof(serialNode));
		}

		AudioStreamPlaylist playlist = new() {
			FadeTime = 0,
			Loop = false,
			Shuffle = false,
			StreamCount = serialNode.Children.Count
		};
		
		void IndexedCallback(int index) {

			if (index == serialNode.Children.Count) {
				callback(playlist);
				return;
			}
			
			CreateStream(serialNode.Children[index], stream => {
				playlist.SetListStream(index, stream);
				IndexedCallback(index + 1);
			});
		}

		// Start the chain
		IndexedCallback(0);
	}

	public AudioStreamWav CreateStreamFromDaemonResponse(SynthesisResultResponse response) {

		byte[] pcmWavData = response.SynthesizedData;
		const int wavImportCompressModePcm = 0;
		const int wavImportLoopModeDisabled = 1;

		return AudioStreamWav.LoadFromBuffer(
			pcmWavData,
			new Godot.Collections.Dictionary {
				["compress/mode"] = wavImportCompressModePcm,
				["edit/loop_mode"] = wavImportLoopModeDisabled,
			}
		);
	}

}