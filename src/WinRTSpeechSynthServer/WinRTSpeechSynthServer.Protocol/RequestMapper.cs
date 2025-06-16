using System;
using System.Collections.Generic;
using System.IO;


namespace WinRTSpeechSynthServer.Protocol;


/// <summary>
/// Maps incoming requsts to registed delegates.
/// </summary>
/// <remarks>
/// Each <see cref="Request"/> has a <see cref="RequestType"/> under <see cref="Request.Type"/> 
/// which is to be written to the stream first.
/// That <see cref="RequestType"/> is read to then construct a particular
/// <see cref="Request"/> decendant and call a delegate corresponding to it.
/// </remarks>
public class RequestMapper {

	/// <summary>
	/// Reads one request from the input stream and writes one response to the output stream.
	/// Borrows the reader and a writer.
	/// Does <b>not</b> flush the writer.
	/// </summary>
	public void HandleSingleRequest(BinaryReader requestReader, BinaryWriter responceWriter) {

		RequestType requestType = (RequestType)requestReader.ReadByte();
		Responce responce;

		if (registeredReaderWithHandler.TryGetValue(requestType, out var handler)) {
			// Handle request if known
			responce = handler(requestReader);
		} else {
			// Default responce if unknown
			responce = new UnknwonRequestResponce();
		}

		// Write responce
		responceWriter.Write((byte)responce.Type);
		responce.WriteContents(responceWriter);

	}

	/// <summary>
	/// A storage of all readers with handlers.
	/// Each delegate here reads the stream to construct a <see cref="Request"/>> object,
	/// invokes the inner registerd handler and returns a responce.
	/// </summary>
	readonly Dictionary<RequestType, Func<BinaryReader, Responce>> registeredReaderWithHandler = new();

	/// <summary>Register a handler for a particular type of <see cref="Request"/>.</summary>
	/// <returns>this</returns>
	public RequestMapper Register<TRequest>(Func<TRequest, Responce> handle) where TRequest : Request, new() {
		RequestType keyByte = new TRequest().Type;

		var readerWithHandler = (BinaryReader requestReader) => {
			// Read the object
			TRequest requestObject = new TRequest();
			requestObject.ReadContents(requestReader);

			// Call the handler
			Responce responce = handle(requestObject);

			// Return responce
			return responce;
		};

		registeredReaderWithHandler[keyByte] = readerWithHandler;

		return this;
	}

}
