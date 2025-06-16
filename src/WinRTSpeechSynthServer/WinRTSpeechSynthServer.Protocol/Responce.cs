using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol;


public enum ResponceType : byte {
	Unknown = 0x00,
	TerminationAccepted = 0x2F,
	HeartbeatEcho = 0x30,
	UnknwonRequestType = 0xFF,
}


/// <summary>Base class for all responce messages passed during communication.</summary>
public abstract record class Responce : Message {
	public sealed override bool IsRequest => false;
	public sealed override byte MessageType => (byte)Type;
	public abstract ResponceType Type { get; }
}


public record class UnknwonRequestResponce : Responce {
	public override ResponceType Type => ResponceType.UnknwonRequestType;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public record class TerminateAcceptedResponce : Responce {
	public override ResponceType Type => ResponceType.TerminationAccepted;
	public override void ReadContents(BinaryReader payloadReader) { }
	public override void WriteContents(BinaryWriter payloadWriter) { }
}


public record class HeartbeatEchoResponce : Responce {

	public override ResponceType Type => ResponceType.HeartbeatEcho;

	public byte EchoByte { get; set; }

	public override void WriteContents(BinaryWriter payloadWriter) {
		payloadWriter.Write(EchoByte);
	}

	public override void ReadContents(BinaryReader payloadReader) {
		EchoByte = payloadReader.ReadByte();
	}
}
