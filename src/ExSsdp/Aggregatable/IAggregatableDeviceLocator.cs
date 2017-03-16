using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public interface IAggregatableDeviceLocator : IDisposable
	{
		event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;

		event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

		IEnumerable<ISsdpDeviceLocator> Locators { get; }

		Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync();

		void StartListeningForNotifications();

		void StopListening();
	}
}