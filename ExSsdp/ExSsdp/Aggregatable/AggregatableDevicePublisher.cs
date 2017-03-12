using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ExSsdp.Http;
using ExSsdp.Network;
using ExSsdp.Publisher;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDevicePublisher : IAggregatableDevicePublisher
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly Dictionary<string, ISsdpDevicePublisher> _ssdpDevicePublishers = new Dictionary<string, ISsdpDevicePublisher>();
		private readonly HttpDeviceInfoPublisher _httpDeviceInfoPublisher;
		private readonly int _port;

		public AggregatableDevicePublisher(INetworkInfoProvider networkInfoProvider,
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			HttpDeviceInfoPublisher httpDeviceInfoPublisher,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));
			if (httpDeviceInfoPublisher == null) throw new ArgumentNullException(nameof(httpDeviceInfoPublisher));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			_httpDeviceInfoPublisher = httpDeviceInfoPublisher;
			_port = port;

			AddPublisher(ssdpDevicePublisherFactory, networkInfoProvider.GetIpAddressesFromAdapters(), port);

			_httpDeviceInfoPublisher.Run(_cancellationTokenSource.Token);
		}

		public AggregatableDevicePublisher(List<string> unicastAddresses,
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			HttpDeviceInfoPublisher httpDeviceInfoPublisher,
			int port)
		{
			if (unicastAddresses == null) throw new ArgumentNullException(nameof(unicastAddresses));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			_httpDeviceInfoPublisher = httpDeviceInfoPublisher;
			_port = port;

			AddPublisher(ssdpDevicePublisherFactory, unicastAddresses, port);

			_httpDeviceInfoPublisher.Run(_cancellationTokenSource.Token);
		}

		public void Dispose()
		{
			if (!_cancellationTokenSource.IsCancellationRequested)
				_cancellationTokenSource.Cancel();

			foreach (var ssdpDevicePublisher in _ssdpDevicePublishers.Values)
			{
				//todo maybe 'byebye notification' should be sent atomatically for each device 
				//in other words need remove all pu blished devices, when Dispose has been called
				//todo interface of publisher should be disposable
				((SsdpDevicePublisher)ssdpDevicePublisher).Dispose();
			}

			_ssdpDevicePublishers.Clear();

			_httpDeviceInfoPublisher.Dispose();
		}

		public IEnumerable<ISsdpDevicePublisher> Publishers => _ssdpDevicePublishers.Select(s => s.Value);

		public IEnumerable<SsdpRootDevice> Devices
		{
			get
			{
				var allPublishedDevices = new List<SsdpRootDevice>();

				foreach (var ssdpDevicePublisher in Publishers)
					allPublishedDevices.AddRange(ssdpDevicePublisher.Devices);

				return allPublishedDevices;
			}
		}

		public static AggregatableDevicePublisher Create(int port = 0)
		{
			var networkInfoProvider = new NetworkInfoProvider();
			var devicePublisherFactory = new SsdpDevicePublisherFactory();
			var httpDevicePublisher = new HttpDeviceInfoPublisher(networkInfoProvider, port);
			return new AggregatableDevicePublisher(networkInfoProvider, devicePublisherFactory, httpDevicePublisher, port);
		}

		public void AddDevice(SsdpRootDevice ssdpRootDevice)
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublishers)
			{
				var publisherLocation = ssdpDevicePublisher.Key;
				var publisher = ssdpDevicePublisher.Value;

				//TODO: change this
				var ipAddress = IPAddress.Parse(publisherLocation);
				string location;
				switch (ipAddress.AddressFamily)
				{
					case AddressFamily.InterNetwork:
						location = $"{publisherLocation}:{_port}";
						break;

					case AddressFamily.InterNetworkV6:
						location = $"[{publisherLocation}]:{_port}";
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(ipAddress.AddressFamily));
				}

				var rootDeviceWithLocation = new SsdpRootDevice(new Uri("http://" + location + "/upnp/description/"), ssdpRootDevice.CacheLifetime, ssdpRootDevice.ToDescriptionDocument());

				_httpDeviceInfoPublisher.AddDeviceInfo(location, rootDeviceWithLocation.ToDescriptionDocument());
				publisher.AddDevice(rootDeviceWithLocation);
			}
		}

		public void RemoveDevice(SsdpRootDevice ssdpRootDevice)
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublishers.Values)
				ssdpDevicePublisher.RemoveDevice(ssdpRootDevice);
		}

		private void AddPublisher(ISsdpDevicePublisherFactory ssdpDevicePublisherFactory, IEnumerable<string> availableUnicastAddresses, int port)
		{
			foreach (var availableUnicastAddress in availableUnicastAddresses)
			{
				var ssdpDevicePublisher = ssdpDevicePublisherFactory.Create(availableUnicastAddress, port);
				_ssdpDevicePublishers[availableUnicastAddress] = ssdpDevicePublisher;
			}
		}
	}
}
