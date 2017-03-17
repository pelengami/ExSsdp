using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExSsdp.Locator;
using ExSsdp.Monitoring;
using ExSsdp.Network;
using ExSsdp.Util;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDeviceLocator : IAggregatableDeviceLocator
	{
		private readonly IList<ISsdpDeviceLocator> _ssdpDeviceLocators = new List<ISsdpDeviceLocator>();
		private readonly DeviceMonitoring _deviceMonitoring = new DeviceMonitoring();

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public AggregatableDeviceLocator(INetworkInfoProvider networkInfoProvider,
			ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDeviceLocatorFactory == null) throw new ArgumentNullException(nameof(ssdpDeviceLocatorFactory));
			if (port < 0) throw new ArgumentOutOfRangeException(nameof(port));

			var unicastAddresses = networkInfoProvider.GetIpAddressesFromAdapters();
			AddLocator(ssdpDeviceLocatorFactory, unicastAddresses, port);

			_deviceMonitoring.DeviceUnvailable += OnMonitoringDeviceUnvailable;
		}

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public AggregatableDeviceLocator(IEnumerable<string> unicastAddresses,
			ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory,
			int port)
		{
			if (unicastAddresses == null) throw new ArgumentNullException(nameof(unicastAddresses));
			if (ssdpDeviceLocatorFactory == null) throw new ArgumentNullException(nameof(ssdpDeviceLocatorFactory));
			if (port < 0) throw new ArgumentException(nameof(port));

			AddLocator(ssdpDeviceLocatorFactory, unicastAddresses, port);

			_deviceMonitoring.DeviceUnvailable += OnMonitoringDeviceUnvailable;
		}

		public event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;

		public event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

		public IEnumerable<ISsdpDeviceLocator> Locators => _ssdpDeviceLocators;

		public bool IsMonitoringEnabled { get; set; }

	    /// <exception cref="ArgumentNullException"/>
	    /// <exception cref="ArgumentOutOfRangeException"/>
	    public static AggregatableDeviceLocator Create(int port = 0)
		{
			var networkInfoProvider = new NetworkInfoProvider();
			var locatorFactory = new SsdpDeviceLocatorFactory();
			var deviceLocator = new AggregatableDeviceLocator(networkInfoProvider, locatorFactory, port);
            return deviceLocator;
        }

        /// <exception cref="InvalidOperationException"></exception>
	    /// <exception cref="ArgumentNullException"/>
	    /// <exception cref="ArgumentOutOfRangeException"/>
	    public static AggregatableDeviceLocator Create()
		{
		    int availablePort;
		    if (!UdpPortChecker.TryGetFirstAvailableUdpPort(out availablePort))
		        throw new InvalidOperationException();

            var networkInfoProvider = new NetworkInfoProvider();
			var locatorFactory = new SsdpDeviceLocatorFactory();
			var deviceLocator = new AggregatableDeviceLocator(networkInfoProvider, locatorFactory, availablePort);

			return deviceLocator;
		}

		public async Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync()
		{
			var allDevices = new List<DiscoveredSsdpDevice>();
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
			{
				try
				{
					var devices = await ssdpDeviceLocator.SearchAsync();
					allDevices.AddRange(devices);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}
			}
			return allDevices;
		}

		public void StartListeningForNotifications()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
				ssdpDeviceLocator.StartListeningForNotifications();

			if (IsMonitoringEnabled)
				_deviceMonitoring.Run();
		}

		public void StopListening()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
				ssdpDeviceLocator.StopListeningForNotifications();

			_deviceMonitoring.Stop();
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

			_deviceMonitoring.Dispose();
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
			if (IsMonitoringEnabled && deviceAvailableEventArgs.IsNewlyDiscovered)
				_deviceMonitoring.AddDevice(deviceAvailableEventArgs.DiscoveredDevice);

			DeviceAvailable?.Invoke(this, deviceAvailableEventArgs);
		}

		private void OnDeviceUnavailable(object sender, DeviceUnavailableEventArgs deviceUnavailableEventArgs)
		{
			if (IsMonitoringEnabled)
				_deviceMonitoring.RemoveDevice(deviceUnavailableEventArgs.DiscoveredDevice);

			DeviceUnavailable?.Invoke(this, deviceUnavailableEventArgs);
		}

		private void OnMonitoringDeviceUnvailable(object sender, DeviceUnavailableEventArgs deviceUnavailableEventArgs)
		{
			if (IsMonitoringEnabled)
				_deviceMonitoring.RemoveDevice(deviceUnavailableEventArgs.DiscoveredDevice);

			DeviceUnavailable?.Invoke(this, deviceUnavailableEventArgs);
		}
	}
}

