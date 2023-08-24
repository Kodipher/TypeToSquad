using System;
using System.Collections.Generic;
using System.Linq;
using Kodipher.TypeToSquad.Modules.Speech;


namespace Kodipher.TypeToSquad.Modules.Configuration;


/// <summary>
/// A storage, setter, getter and validator
/// for a field that is a selection of MMDevices.
/// Created to cache device Id, as getting friendly names is slow.
/// Uses FieldOptions<string> for device selection by name compatability.
/// </summary>
public class FieldMMDevice : FieldOptions<string> {

	public Func<IEnumerable<MMDeviceSelectionInfo>> GetDevices { get; private init; }

	public Func<MMDeviceSelectionInfo> GetDefaultDevice { get; private init; }

	#region //// Device ID

	public override void Set(string value) {

		// Set device name in options
		base.Set(value);

		// Cache device ID
		var currentDeiceSearch = GetDevices().Where(device => device.Name == Value).ToArray();

		if (currentDeiceSearch.Length == 0) {
			DeviceID = GetDefaultDevice().ID;
		} else {
			DeviceID = currentDeiceSearch[0].ID;
		}

	}

	public override string ValueForceValid(string value) {
		if (IsValid(value)) return value;
		DeviceID = GetDefaultDevice().ID;
		return GetDefault();
	}

	public string DeviceID { get; protected set; }

	#endregion

	public FieldMMDevice(Func<IEnumerable<MMDeviceSelectionInfo>> getDevices, Func<MMDeviceSelectionInfo> getDefaultDevice)
	: base(
		() => getDevices().Select(device => device.Name),
		() => getDefaultDevice().Name
	) {
		GetDevices = getDevices;
		GetDefaultDevice = getDefaultDevice;
		DeviceID = getDefaultDevice().ID;
	}

	public FieldMMDevice(Func<IEnumerable<MMDeviceSelectionInfo>> getDevices, MMDeviceSelectionInfo defaultDevice)
	: this(getDevices, () => defaultDevice)
	{ }

}
