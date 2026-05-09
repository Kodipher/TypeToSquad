using System.IO;


namespace WinRTSpeechSynthServer.Protocol.Messages;


/// <summary>Base class for all structures passed during communication.</summary>
public abstract record class Message {

	/// <summary>
	/// Whether this message is a request or a response. 
	/// Affects what <see cref="MessageType"/> means.
	/// </summary>
	public abstract bool IsRequest { get; }

	/// <summary>The identification of this request/responce object.</summary>
	public abstract byte MessageType { get; }

	/// <summary>
	/// Writes the contents (payload) of this object using a borrowed writer,
	/// so that this object can be reconstructed using 
	/// <see cref="ReadContents(BinaryReader)"/> of the current class.
	/// </summary>
	public abstract void WriteContents(BinaryWriter payloadWriter);

	/// <summary>
	/// Reads the contents (payload) using a borrowed reader into this object,
	/// i.e. reconstructs this object from data written using 
	/// <see cref="WriteContents(BinaryWriter)"/> of the current class.
	/// </summary>
	public abstract void ReadContents(BinaryReader payloadReader);

}
