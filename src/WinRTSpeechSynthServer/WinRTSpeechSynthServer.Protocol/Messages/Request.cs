using System;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol.Messages;


public enum RequestType : byte {
	Unknown = 0x00,
	SynthesizeText = 0x01,
	SynthesizeSsml = 0x02,
	GetVoices = 0x10,
	SetVoice = 0x11,
	SetVoiceToDefault = 0x12,
	Heartbeat = 0xE0,
	Terminate = 0xFD
}


/// <summary>Base class for all request messages passed during communication.</summary>
public abstract record class Request : Message {
	public sealed override bool IsRequest => true;
	public sealed override byte MessageType => (byte)Type;
	public abstract RequestType Type { get; }
}


#region //// Synthesis

public record class SynthesizeTextRequest : Request {

	public override RequestType Type => RequestType.SynthesizeText;

	public string InputString { get; set; } = "";

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteUtf8WithLength(InputString);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		InputString = payloadReader.ReadUtf8WithLength();
	}

}


public record class SynthesizeSsmlRequest : Request {

	public override RequestType Type => RequestType.SynthesizeSsml;

	public string InputString { get; set; } = "";

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteUtf8WithLength(InputString);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		InputString = payloadReader.ReadUtf8WithLength();
	}

}

#endregion


#region //// Voices

public record class GetVoicesRequest : Request {
	public override RequestType Type => RequestType.GetVoices;
	public override void WriteContents(BinaryWriter payloadWriter) { }
	public override void ReadContents(BinaryReader payloadReader) { }
}


public record class SetVoiceRequest : Request {

	public override RequestType Type => RequestType.SetVoice;

	public string VoiceName { get; set; } = "";

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteUtf8WithLength(VoiceName);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		VoiceName = payloadReader.ReadUtf8WithLength();
	}

}


public record class SetVoiceToDefaultRequest : Request {
	public override RequestType Type => RequestType.SetVoiceToDefault;
	public override void WriteContents(BinaryWriter payloadWriter) { }
	public override void ReadContents(BinaryReader payloadReader) { }
}

#endregion


public record class TerminateRequest : Request {
	public override RequestType Type => RequestType.Terminate;
	public override void WriteContents(BinaryWriter payloadWriter) { }
	public override void ReadContents(BinaryReader payloadReader) { }
}


public record class HeartbeatRequest : Request {

	public override RequestType Type => RequestType.Heartbeat;

	public byte EchoByte { get; set; }

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(EchoByte);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		EchoByte = payloadReader.ReadByte();
	}
}
