using System;
using System.Collections.Generic;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public interface IAggregatableDevicePublisher : IDisposable
	{
		IEnumerable<ISsdpDevicePublisher> Publishers { get; }

		IEnumerable<SsdpRootDevice> Devices { get; }

		void AddDevice(SsdpRootDevice ssdpRootDevice);

		void RemoveDevice(SsdpRootDevice ssdpRootDevice);
	}
}