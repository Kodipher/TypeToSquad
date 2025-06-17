

namespace WinRTSpeechSynthServer.Protocol;


public enum VoiceGender : byte {
	Unknown = 0x00,
	Male = 0x01,
	Female = 0x02
}


public record VoiceInfo {
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Language { get; init; }
	public required VoiceGender Gender { get; init; }
}
