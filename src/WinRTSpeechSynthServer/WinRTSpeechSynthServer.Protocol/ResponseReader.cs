using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace WinRTSpeechSynthServer.Protocol;


/// <summary>
/// Reads incoming responses as objects.
/// </summary>
/// <remarks>
/// Each <see cref="Response"/> has a <see cref="ResponseType"/> under <see cref="Response.Type"/> 
/// which is to be written to the stream first.
/// That <see cref="ResponseType"/> is read to pick a particular
/// <see cref="Response"/> decendant and construct and fill.
/// </remarks>
public class ResponseReader {


	readonly Dictionary<ResponseType, Func<BinaryReader, Response>> registeredReaders = new();

	/// <summary>
	/// Registers a type so that a <see cref="Response"/> of that type can
	/// be read by this reader.
	/// </summary>
	public ResponseReader Register<TResponse>() where TResponse : Response, new() {
		ResponseType keyByte = new TResponse().Type;

		var reader = (BinaryReader responseReader) => {
			TResponse responseObject = new TResponse();
			responseObject.ReadContents(responseReader);
			return responseObject;
		};

		registeredReaders[keyByte] = reader;

		return this;
	}

	/// <summary>
	/// Runs <see cref="Register{TResponse}"/> for each standard type (subclass)
	/// of <see cref="Response"/>.
	/// </summary>
	public void RegisterAll() {

		IEnumerable<Type> allResponseTypes = typeof(ResponseReader)
												.Assembly
												.GetTypes()
												.Where(type => type.IsSubclassOf(typeof(Response)))
												.Where(type => type.GetConstructor(Type.EmptyTypes) is not null);

		MethodInfo? registerMethod = typeof(ResponseReader)
										.GetMethod(
											nameof(Register), 
											BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
										);

		foreach (var responseType in allResponseTypes) {
			MethodInfo? genericRegisterMethod = registerMethod?.MakeGenericMethod(responseType);
			genericRegisterMethod?.Invoke(this, null);
		}

	}

	/// <summary>
	/// Reads a single <see cref="Response"/> from the borrowed stream.
	/// Throws <see cref="InvalidOperationException"/> if <see cref="ResponseType"/>
	/// is not recognized.
	/// </summary>
	/// <exception cref="InvalidOperationException">No registerd type has a matching <see cref="ResponseType"/>.</exception>
	public Response ReadResponce(BinaryReader responseReader) {

		ResponseType responseType = (ResponseType)responseReader.ReadByte();
	
		if (registeredReaders.TryGetValue(responseType, out var reader)) {
			return reader(responseReader);
		}

		throw new InvalidOperationException($"Reader cannot read response of type {responseType}");
	}

}
