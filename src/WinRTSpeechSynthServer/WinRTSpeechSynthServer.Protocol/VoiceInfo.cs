

namespace WinRTSpeechSynthServer.Protocol;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public enum VoiceGender : byte {
	Unknown = 0x00,
	Male = 0x01,
	Female = 0x02
}
#pragma warning restore CS1591


/// <summary>Information about a voice (synthesis engine) installed on the system.</summary>
public record VoiceInfo {

	/// <summary>The unique id of the synthesis engine (voice).</summary>
	public required string Id { get; init; }

	/// <summary>The display name of the synthesis engine (voice).</summary>
	public required string Name { get; init; }

	/// <summary>The BCP-47 language tag of the synthesis engine (voice).</summary>
	public required string Language { get; init; }

	/// <summary>The gender of the synthesis engine (voice).</summary>
	public required VoiceGender Gender { get; init; }

}
