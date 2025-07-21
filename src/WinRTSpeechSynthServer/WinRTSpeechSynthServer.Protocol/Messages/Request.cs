using System;
using System.ComponentModel;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol.Messages;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


public enum RequestType : byte {
	Unknown = 0x00,
	GetVoices = 0x10,
	Synthesize = 0x20,
	Heartbeat = 0xE0,
	Terminate = 0xFD
}


/// <summary>Base class for all request messages passed during communication.</summary>
public abstract record class Request : Message {

	public sealed override bool IsRequest => true;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed override byte MessageType => (byte)Type;

	public abstract RequestType Type { get; }

}


public sealed record class SynthesizeRequest : Request {

	public override RequestType Type => RequestType.Synthesize;
	
	/// <summary>
	/// The name of the voice to use to synthesize text.
	/// If the name is invalid, the default voice will be used.
	/// </summary>
	public string VoiceName { get; set; } = "";

	/// <summary>The text message to speak out.</summary>
	public string InputString { get; set; } = "";

	/// <summary>
	/// Wether the <see cref="InputString"/> is written in SSML (true),
	/// or plain text (false).
	/// </summary>
	public bool IsSsml { get; set; } = false;

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.WriteUtf8WithLength(VoiceName);
		payloadWriter.WriteUtf8WithLength(InputString);
		payloadWriter.Write(IsSsml);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		VoiceName = payloadReader.ReadUtf8WithLength();
		InputString = payloadReader.ReadUtf8WithLength();
		IsSsml = payloadReader.ReadBoolean();
	}

}


public sealed record class GetVoicesRequest : Request {
	public override RequestType Type => RequestType.GetVoices;
	public override void WriteContents(BinaryWriter payloadWriter) { }
	public override void ReadContents(BinaryReader payloadReader) { }
}


public sealed record class TerminateRequest : Request {
	public override RequestType Type => RequestType.Terminate;
	public override void WriteContents(BinaryWriter payloadWriter) { }
	public override void ReadContents(BinaryReader payloadReader) { }
}


public sealed record class HeartbeatRequest : Request {

	public override RequestType Type => RequestType.Heartbeat;

	public byte EchoByte { get; set; }

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(EchoByte);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		EchoByte = payloadReader.ReadByte();
	}
}


#pragma warning restore CS1591
