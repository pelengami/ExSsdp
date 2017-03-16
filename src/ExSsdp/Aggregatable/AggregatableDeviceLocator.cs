using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExSsdp.Http;
using ExSsdp.Locator;
using ExSsdp.Network;
using ExSsdp.Util;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDeviceLocator : IAggregatableDeviceLocator
	{
		private readonly IList<ISsdpDeviceLocator> _ssdpDeviceLocators = new List<ISsdpDeviceLocator>();
		private readonly ConcurrentDictionary<string, DiscoveredSsdpDevice> _discoveredSsdpDevices = new ConcurrentDictionary<string, DiscoveredSsdpDevice>();
		private readonly HttpAvailabilityChecker _availabilityChecker = new HttpAvailabilityChecker();
		private CancellationTokenSource _httpAvailabilyTokenSource = new CancellationTokenSource();

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public AggregatableDeviceLocator(INetworkInfoProvider networkInfoProvider,
			ISsdpDeviceLocatorFactory ssdpDeviceLocatorFactory,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDeviceLocatorFactory == null) throw new ArgumentNullException(nameof(ssdpDeviceLocatorFactory));
			if (port < 0) throw new ArgumentException(nameof(port));

			var unicastAddresses = networkInfoProvider.GetIpAddressesFromAdapters();
			AddLocator(ssdpDeviceLocatorFactory, unicastAddresses, port);
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

			if (!_httpAvailabilyTokenSource.IsCancellationRequested)
				_httpAvailabilyTokenSource.Cancel();
		}

		public event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;

		public event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

		public event EventHandler<DiscoveredSsdpDevice> HttpLocationDeviceUnavailable;

		public IEnumerable<ISsdpDeviceLocator> Locators => _ssdpDeviceLocators;

		public bool CheckDevicesForAvailable { get; set; }

		public static AggregatableDeviceLocator Create(int port = 0)
		{
			var networkInfoProvider = new NetworkInfoProvider();
			var locatorFactory = new SsdpDeviceLocatorFactory();
			var deviceLocator = new AggregatableDeviceLocator(networkInfoProvider, locatorFactory, port);
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

			if (!CheckDevicesForAvailable)
				return;

			RunMonitoringForAvailability();
		}

		public void StopListening()
		{
			foreach (var ssdpDeviceLocator in _ssdpDeviceLocators)
				ssdpDeviceLocator.StopListeningForNotifications();

			_httpAvailabilyTokenSource.Cancel();
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
			AddToMonitoringIfNecessary(deviceAvailableEventArgs);

			DeviceAvailable?.Invoke(this, deviceAvailableEventArgs);
		}

		private void OnDeviceUnavailable(object sender, DeviceUnavailableEventArgs deviceUnavailableEventArgs)
		{
			RemoveFromMonitoringIfNecessary(deviceUnavailableEventArgs);

			DeviceUnavailable?.Invoke(this, deviceUnavailableEventArgs);
		}

		private void RunMonitoringForAvailability()
		{
			var monitoringAction = new Action(delegate
			{
				foreach (var discoveredSsdpDevice in _discoveredSsdpDevices)
				{
					var location = discoveredSsdpDevice.Key;
					var isAvailable = _availabilityChecker.Check(location);
					if (isAvailable)
						continue;

					var discoveredDevice = discoveredSsdpDevice.Value;

					DiscoveredSsdpDevice tempDevice;
					if (_discoveredSsdpDevices.TryRemove(location, out tempDevice))
						HttpLocationDeviceUnavailable?.Invoke(this, discoveredDevice);
				}
			});

			if (!_httpAvailabilyTokenSource.IsCancellationRequested)
				_httpAvailabilyTokenSource.Cancel();

			_httpAvailabilyTokenSource = new CancellationTokenSource();

			Repeater.DoInfinityAsync(monitoringAction, TimeSpan.FromSeconds(7), _httpAvailabilyTokenSource.Token);
		}

		private void AddToMonitoringIfNecessary(DeviceAvailableEventArgs deviceAvailableEventArgs)
		{
			if (!CheckDevicesForAvailable)
				return;

			var discoveredSsdpDevice = deviceAvailableEventArgs.DiscoveredDevice;
			var location = discoveredSsdpDevice.DescriptionLocation.ToString();

			if (!_discoveredSsdpDevices.ContainsKey(location))
				_discoveredSsdpDevices.TryAdd(location, discoveredSsdpDevice);
		}

		private void RemoveFromMonitoringIfNecessary(DeviceUnavailableEventArgs deviceUnavailableEventArgs)
		{
			if (!CheckDevicesForAvailable)
				return;

			var discoveredSsdpDevice = deviceUnavailableEventArgs.DiscoveredDevice;
			var location = discoveredSsdpDevice.DescriptionLocation.ToString();

			if (_discoveredSsdpDevices.ContainsKey(location))
			{
				DiscoveredSsdpDevice tempDevice;
				_discoveredSsdpDevices.TryRemove(location, out tempDevice);
			}
		}
	}
}
