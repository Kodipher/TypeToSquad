using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using WinRTSpeechSynthServer.Protocol;
using WinRTSpeechSynthServer.Protocol.Messages;

using TypeToSquad.Model.Settings;


namespace TypeToSquad.Model;


/// <summary>Queries are stores voices supplied by the daemon.</summary>
public partial class DaemonVoiceStorage : Node {
	
	#region /--- Singleton ---/

	public static DaemonVoiceStorage Instance { get; private set; } = null!; // Set in _Ready

	private void StageSingletonInstance() {
		Instance ??= this;
	}

	#endregion
	
	
	public VoiceInfo? DefaultVoice { get; private set; } = null;

	public ReadOnlyDictionary<string, VoiceInfo>? VoicesByKey { get; private set; } = null;
	
	public static string VoiceToSelectionKey(VoiceInfo voice) => $"{voice.Name} ({voice.Language})";
	
	/// <remarks>Throws, if unable to find a voice.</remarks>
	/// <exception cref="InvalidOperationException">Voice query was not finished.</exception>
	/// <exception cref="KeyNotFoundException">No voice under given key.</exception>
	public VoiceInfo GetVoiceByKey(string key) {
		if (VoicesByKey is null) throw new InvalidOperationException("Voices accessed before the query is finished.");
		if (!VoicesByKey.TryGetValue(key, out VoiceInfo? info)) throw new KeyNotFoundException($"No voice under key \"{key}\"");
		return info;
	}
	
	
	public override void _Ready() {

		StageSingletonInstance();
		
		// Find voices
		SpeechDaemon.Instance.DispatchRequest<AllVoicesResponse>(
			new GetVoicesRequest(),
			voicesResponse => {
				
				DefaultVoice = voicesResponse.DefaultVoice;
				
				VoicesByKey = voicesResponse
								.Voices
								.ToDictionary(VoiceToSelectionKey)
								.AsReadOnly();
				
				// Update relevant runtime options
				var settings = UserSettingsManager.Instance.Settings;

				string[] voiceOptions = voicesResponse
											.Voices
											.OrderBy(v => v.Language)
											.Select(VoiceToSelectionKey)
											.ToArray();

				string voiceDefaultOption = VoiceToSelectionKey(voicesResponse.DefaultVoice);
				
				Field TableVoiceSelectPrototype() {
					var field = new FieldOptionsRuntime();
					field.SetOptions(settings.VoiceKey.Options!, settings.VoiceKey.DefaultOption!);
					return field;
				}
				
				settings.VoiceKey.SetOptions(voiceOptions, voiceDefaultOption);
				settings.VoiceChanges.ChangePrototypeForColumn(1, TableVoiceSelectPrototype);

			}
		);
	}
	
}