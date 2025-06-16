using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol;


public enum ResponceType : byte {
	Unknown = 0x00,
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

