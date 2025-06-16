using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol;


public enum RequestType : byte {
	Unknown = 0x00,
}


/// <summary>Base class for all request messages passed during communication.</summary>
public abstract record class Request : Message {
	public sealed override bool IsRequest => true;
	public sealed override byte MessageType => (byte)Type;
	public abstract RequestType Type { get; }
}

