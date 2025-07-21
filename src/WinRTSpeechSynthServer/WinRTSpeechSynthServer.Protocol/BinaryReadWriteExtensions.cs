using System;
using System.IO;
using System.Text;
using WinRTSpeechSynthServer.Protocol.Messages;


namespace WinRTSpeechSynthServer.Protocol;


/// <summary>
/// Extensions for <see cref="BinaryWriter"/> and <see cref="BinaryReader"/>
/// to abstract away reading and writing exact bytes of some classes/structs.
/// </summary>
public static class BinaryReadWriteExtensions {

	/// <summary>
	/// Writes a <see cref="Message"/> to the steam
	/// and advances stream position.
	/// Writes <see cref="Message.MessageType"/> byte first.
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
		int length = reader.ReadInt32();
		byte[] buffer = new byte[length];
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


	/// <summary>
	/// Writes a <see cref="VoiceInfo"/> in a format
	/// <see cref="ReadVoiceInfo"/> can understand and advances stream position.
	/// </summary>
	public static void Write(this BinaryWriter writer, VoiceInfo voice) {
		writer.WriteUtf8WithLength(voice.Id);
		writer.WriteUtf8WithLength(voice.Name);
		writer.WriteUtf8WithLength(voice.Language);
		writer.Write((byte)voice.Gender);
	}

	/// <summary>
	/// Reads a <see cref="VoiceInfo"/> that was written by
	/// <see cref="Write(BinaryWriter, VoiceInfo)"/> and advances stream position.
	/// </summary>
	public static VoiceInfo ReadVoiceInfo(this BinaryReader reader) {
		return new VoiceInfo() {
			Id = reader.ReadUtf8WithLength(),
			Name = reader.ReadUtf8WithLength(),
			Language = reader.ReadUtf8WithLength(),
			Gender = (VoiceGender)reader.ReadByte(),
		};
	}


	/// <summary>
	/// Writes an array of <see cref="VoiceInfo"/>s in a format
	/// <see cref="ReadVoiceInfoArray"/> can understand and advances stream position.
	/// </summary>
	public static void Write(this BinaryWriter writer, VoiceInfo[] voices) {
		writer.Write((int)voices.Length);
		for (int i = 0; i < voices.Length; i++) {
			writer.Write(voices[i]);
		}
	}

	/// <summary>
	/// Reads an array of <see cref="VoiceInfo"/>s that was written by
	/// <see cref="Write(BinaryWriter, VoiceInfo[])"/> and advances stream position.
	/// </summary>
	public static VoiceInfo[] ReadVoiceInfoArray(this BinaryReader reader) {
		int length = reader.ReadInt32();
		VoiceInfo[] voices = new VoiceInfo[length];
		for (int i = 0; i < length; i++) {
			voices[i] = reader.ReadVoiceInfo();
		}
		return voices;
	}

}
