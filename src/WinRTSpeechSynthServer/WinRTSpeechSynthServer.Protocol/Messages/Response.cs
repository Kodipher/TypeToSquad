using System;
using System.ComponentModel;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol.Messages;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


public enum ResponseType : byte {
	Unknown = 0x00,
	SynthesisResult = 0x21,
	AllVoices = 0x29,
	TerminationAccepted = 0x2F,
	HeartbeatEcho = 0x30,
	UnknownRequestType = 0xFF,
}


/// <summary>Base class for all response messages passed during communication.</summary>
public abstract record class Response : Message {

	public sealed override bool IsRequest => false;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed override byte MessageType => (byte)Type;

	public abstract ResponseType Type { get; }

}


public sealed record SynthesisResultResponse : Response {

	public override ResponseType Type => ResponseType.SynthesisResult;

	/// <summary>The audio synthesized from the given message.</summary>
	public byte[] SynthesizedData { get; set; } = Array.Empty<byte>();

	/// <summary>
	/// Whether the given voice was used (true) or
	/// the default voice was used as a fallback (false).
	/// </summary>
	public bool GivenVoiceExists { get; set; } = true;


	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteBufferWithLength(SynthesizedData);
		payloadWriter.Write(GivenVoiceExists);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		SynthesizedData = payloadReader.ReadBufferWithLength();
		GivenVoiceExists = payloadReader.ReadBoolean();
	}
}


public sealed record AllVoicesResponse : Response {

	public override ResponseType Type => ResponseType.AllVoices;

	public VoiceInfo[] Voices { get; set; } = Array.Empty<VoiceInfo>();

	public VoiceInfo DefaultVoice { get; set; } = VoiceInfo.Empty;

	public override void ReadContents(BinaryReader payloadReader) {
		Voices = payloadReader.ReadVoiceInfoArray();
		DefaultVoice = payloadReader.ReadVoiceInfo();
	}

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(Voices);
		payloadWriter.Write(DefaultVoice);
	}

}


public sealed record UnknownRequestResponse : Response {
	public override ResponseType Type => ResponseType.UnknownRequestType;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public sealed record TerminateAcceptedResponse : Response {
	public override ResponseType Type => ResponseType.TerminationAccepted;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public sealed record HeartbeatEchoResponse : Response {

	public override ResponseType Type => ResponseType.HeartbeatEcho;

	public byte EchoByte { get; set; }

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(EchoByte);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		EchoByte = payloadReader.ReadByte();
	}
}


#pragma warning restore CS1591
