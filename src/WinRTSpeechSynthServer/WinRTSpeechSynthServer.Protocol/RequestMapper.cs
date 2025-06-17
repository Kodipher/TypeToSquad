using System;
using System.Collections.Generic;
using System.IO;
using WinRTSpeechSynthServer.Protocol.Messages;


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
	/// Invoked when the <see cref="Request.Type"/> has been read.
	/// The rest of the request is read imedeatly after.
	/// </summary>
	public event Action<RequestType> OnRequestReadStart = delegate { };

	/// <summary>
	/// Reads one request from the input stream and writes one response to the output stream.
	/// Borrows the reader and a writer.
	/// Does <b>not</b> flush the writer.
	/// </summary>
	public void HandleSingleRequest(BinaryReader requestReader, BinaryWriter responseWriter) {

		RequestType requestType = (RequestType)requestReader.ReadByte();
		Response response;

		OnRequestReadStart(requestType);

		if (registeredReaderWithHandler.TryGetValue(requestType, out var handler)) {
			// Handle request if known
			response = handler(requestReader);
		} else {
			// Default response if unknown
			response = new UnknwonRequestResponse();
		}

		// Write response
		responseWriter.Write(response);
	}

	/// <summary>
	/// A storage of all readers with handlers.
	/// Each delegate here reads the stream to construct a <see cref="Request"/>> object,
	/// invokes the inner registerd handler and returns a response.
	/// </summary>
	readonly Dictionary<RequestType, Func<BinaryReader, Response>> registeredReaderWithHandler = new();

	/// <summary>Register a handler for a particular type of <see cref="Request"/>.</summary>
	/// <returns>this</returns>
	public RequestMapper Register<TRequest>(Func<TRequest, Response> handle) where TRequest : Request, new() {
		RequestType keyByte = new TRequest().Type;

		var readerWithHandler = (BinaryReader requestReader) => {
			// Read the object
			TRequest requestObject = new TRequest();
			requestObject.ReadContents(requestReader);

			// Call the handler
			Response response = handle(requestObject);

			// Return response
			return response;
		};

		registeredReaderWithHandler[keyByte] = readerWithHandler;

		return this;
	}

}
