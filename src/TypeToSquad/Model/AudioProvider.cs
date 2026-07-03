using Godot;
using System;
using System.Collections.Generic;
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
			
		} else {
			throw new NotSupportedException($"Unsupported node type \"{root.Type}\"");
		}
		
	}
	
	public void CreateStreamFromTextOrSsml(RenderNode textNode, Action<AudioStream> callback) {

		// Guards
		if (!(textNode.Type == RenderNodeType.Text || textNode.Type == RenderNodeType.SsmlRoot)) {
			throw new ArgumentException($"Incorrect node type. Got {textNode.Type}.", nameof(textNode));
		}
		
		// Request
		var settingsInstance = UserSettingsManager.Instance.Settings;
		
		SynthesizeRequest synthRequest = new SynthesizeRequest() {
			InputString = MessageProcessor.StringifyNodeRecursive(textNode, indented: false),
			IsSsml = textNode.Type == RenderNodeType.SsmlRoot,
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