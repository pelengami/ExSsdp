using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExSsdp.Locator;
using ExSsdp.Network;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDeviceLocator : IAggregatableDeviceLocator
	{
		private readonly IList<ISsdpDeviceLocator> _ssdpDeviceLocators = new List<ISsdpDeviceLocator>();

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="InvalidOperationException"/>
		public AggregatableDeviceLocator(INetworkInfoProvider networkInfoProvider,
		ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDeviceLocatorFactory == null) throw new ArgumentNullException(nameof(ssdpDeviceLocatorFactory));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			var unicastAddresses = networkInfoProvider.GetIpAddressesFromAdapters();
			AddLocator(ssdpDeviceLocatorFactory, unicastAddresses, port);
		}

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="InvalidOperationException"/>
		public AggregatableDeviceLocator(IEnumerable<string> unicastAddresses,
		ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory,
			int port)
		{
			if (unicastAddresses == null) throw new ArgumentNullException(nameof(unicastAddresses));
			if (ssdpDeviceLocatorFactory == null) throw new ArgumentNullException(nameof(ssdpDeviceLocatorFactory));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			AddLocator(ssdpDeviceLocatorFactory, unicastAddresses, port);
		}

		public void Dispose()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
			{
				ssdpDeviceLocator.DeviceAvailable -= OnDeviceAvailable;
				ssdpDeviceLocator.DeviceUnavailable -= OnDeviceUnavailable;
				ssdpDeviceLocator.StopListeningForNotifications();

				//todo interface of locator should be idisposable
				//ssdpDeviceLocator.Dispose();
			}
		}

		public event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;

		public event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

		public IEnumerable<ISsdpDeviceLocator> Locators => _ssdpDeviceLocators;

		public async Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync()
		{
			var allDevices = new List<DiscoveredSsdpDevice>();
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
			{
				var devices = await ssdpDeviceLocator.SearchAsync();
				allDevices.AddRange(devices);
			}
			return allDevices;
		}

		public void StartListeningForNotifications()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
				ssdpDeviceLocator.StartListeningForNotifications();
		}

		public void StopListening()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
				ssdpDeviceLocator.StopListeningForNotifications();
		}

		private void AddLocator(ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory, IEnumerable<string> availableUnicastAddresses, int port)
		{
			foreach (var availableUnicastAddress in availableUnicastAddresses)
			{
				var ssdpDeviceLocator = ssdpDeviceLocatorFactory.Create(availableUnicastAddress, port);

				ssdpDeviceLocator.DeviceAvailable += OnDeviceAvailable;
				ssdpDeviceLocator.DeviceUnavailable += OnDeviceUnavailable;
				_ssdpDeviceLocators.Add(ssdpDeviceLocator);
			}
		}

		private void OnDeviceAvailable(object sender, DeviceAvailableEventArgs deviceAvailableEventArgs)
		{
			DeviceAvailable?.Invoke(this, deviceAvailableEventArgs);
		}

		private void OnDeviceUnavailable(object sender, DeviceUnavailableEventArgs deviceUnavailableEventArgs)
		{
			DeviceUnavailable?.Invoke(this, deviceUnavailableEventArgs);
		}
	}
}
