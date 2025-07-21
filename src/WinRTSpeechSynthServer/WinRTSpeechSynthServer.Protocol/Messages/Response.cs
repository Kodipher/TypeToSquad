using System;
using System.ComponentModel;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol.Messages;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


public enum ResponseType : byte {
	Unknown = 0x00,
	SyntesisResult = 0x21,
	AllVoices = 0x29,
	VoiceSetConfirmation = 0x2A,
	DefaultVoiceSetConfirmation = 0x2B,
	TerminationAccepted = 0x2F,
	HeartbeatEcho = 0x30,
	UnknwonRequestType = 0xFF,
}


/// <summary>Base class for all responce messages passed during communication.</summary>
public abstract record class Response : Message {

	public sealed override bool IsRequest => false;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed override byte MessageType => (byte)Type;

	public abstract ResponseType Type { get; }

}


public sealed record class SyntesisResultResponse : Response {

	public override ResponseType Type => ResponseType.SyntesisResult;

	public byte[] SynthesizedData { get; set; } = Array.Empty<byte>();

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteBufferWithLength(SynthesizedData);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		SynthesizedData = payloadReader.ReadBufferWithLength();
	}
}


#region //// Voices


public sealed record class AllVoicesResponse : Response {

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


public sealed record class VoiceSetResponse : Response {

	public override ResponseType Type => ResponseType.VoiceSetConfirmation;

	public bool WasSet { get; set; } = false;
	public VoiceInfo CurrentVoice { get; set; } = VoiceInfo.Empty;

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(WasSet);
		payloadWriter.Write(CurrentVoice);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		WasSet = payloadReader.ReadBoolean();
		CurrentVoice = payloadReader.ReadVoiceInfo();
	}
}


public sealed record class DefaultVoiceSetResponse : Response {

	public override ResponseType Type => ResponseType.DefaultVoiceSetConfirmation;

	public VoiceInfo DefaultVoice { get; set; } = VoiceInfo.Empty;

	public override void ReadContents(BinaryReader payloadReader) {
		DefaultVoice = payloadReader.ReadVoiceInfo();
	}

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(DefaultVoice);
	}

}

#endregion


public sealed record class UnknwonRequestResponse : Response {
	public override ResponseType Type => ResponseType.UnknwonRequestType;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public sealed record class TerminateAcceptedResponse : Response {
	public override ResponseType Type => ResponseType.TerminationAccepted;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public sealed record class HeartbeatEchoResponse : Response {

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
