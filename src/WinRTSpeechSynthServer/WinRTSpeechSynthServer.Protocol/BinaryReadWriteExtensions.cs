using System;
using System.IO;
using System.Text;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace WinRTSpeechSynthServer.Protocol;


public static class BinaryReadWriteExtensions {

	/// <summary>
	/// Writes a <see cref="Message"/> to the steam
	/// and advances stream position.
	/// Does <b>not</b> distinguish <see cref="Message.MessageType"/>
	/// and does not write it to the stream.
	/// </summary>
	public static void Write(this BinaryWriter writer, Message message) {
		writer.Write((byte)message.MessageType);
		message.WriteContents(writer);
	}


	/// <summary>
	/// Writes the buffer's <see cref="Array.Length"/> as <see cref="int"/>, 
	/// then the buffer itself and also advances stream position.
	/// </summary>
	public static void WriteBufferWithLength(this BinaryWriter writer, byte[] buffer) {
		writer.Write((int)buffer.Length);
		writer.Write(buffer);
	}

	/// <summary>
	/// Reads <see cref="Array.Length"/> as <see cref="int"/> 
	/// then the reads a buffer of that length.
	/// Advances stream position.
	/// </summary>
	public static byte[] ReadBufferWithLength(this BinaryReader reader) {
		int inputByteLength = reader.ReadInt32();
		byte[] buffer = new byte[inputByteLength];
		reader.Read(buffer);
		return buffer;
	}


	/// <summary>
	/// Writes a string as a utf8 buffer using <see cref="WriteBufferWithLength(BinaryWriter, byte[])"/>
	/// and advances stream position.
	/// </summary>
	public static void WriteUtf8WithLength(this BinaryWriter writer, string str) {
		byte[] strAsUtf8 = Encoding.UTF8.GetBytes(str);
		writer.WriteBufferWithLength(strAsUtf8);
	}

	/// <summary>
	/// Reads an integer byte length and then the utf8 string of that length
	/// using <see cref="ReadBufferWithLength(BinaryReader)"/>
	/// Advances stream position.
	/// </summary>
	public static string ReadUtf8WithLength(this BinaryReader reader) {
		return Encoding.UTF8.GetString(reader.ReadBufferWithLength());
	}

}
