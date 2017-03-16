using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExSsdp.Http;
using ExSsdp.Network;
using ExSsdp.Publisher;
using ExSsdp.Util;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDevicePublisher : IAggregatableDevicePublisher
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly Dictionary<string, ISsdpDevicePublisher> _ssdpDevicePublishers = new Dictionary<string, ISsdpDevicePublisher>();
		private readonly IHttpDeviceInfoPublisher _httpDeviceInfoPublisher;
		private readonly int _port;

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public AggregatableDevicePublisher(INetworkInfoProvider networkInfoProvider,
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			IHttpDeviceInfoPublisher httpDeviceInfoPublisher,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));
			if (httpDeviceInfoPublisher == null) throw new ArgumentNullException(nameof(httpDeviceInfoPublisher));
			if (port < 0) throw new ArgumentOutOfRangeException(nameof(port));

			_httpDeviceInfoPublisher = httpDeviceInfoPublisher;
			_port = port;

			AddPublisher(ssdpDevicePublisherFactory, networkInfoProvider.GetIpAddressesFromAdapters(), port);

			_httpDeviceInfoPublisher.Run(_cancellationTokenSource.Token);
		}

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public AggregatableDevicePublisher(List<string> unicastAddresses,
		ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			IHttpDeviceInfoPublisher httpDeviceInfoPublisher,
			int port)
		{
			if (unicastAddresses == null) throw new ArgumentNullException(nameof(unicastAddresses));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));
			if (httpDeviceInfoPublisher == null) throw new ArgumentNullException(nameof(httpDeviceInfoPublisher));
			if (port < 0) throw new ArgumentException(nameof(port));

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
				//((SsdpDevicePublisher)ssdpDevicePublisher).Dispose();
			}

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

		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static AggregatableDevicePublisher Create(int port = 0)
		{
			var networkInfoProvider = new NetworkInfoProvider();
			var devicePublisherFactory = new SsdpDevicePublisherFactory();
			var httpDevicePublisher = new HttpDeviceInfoPublisher(port);

			return new AggregatableDevicePublisher(networkInfoProvider, devicePublisherFactory, httpDevicePublisher, port);
		}

		/// <exception cref="ArgumentException"></exception>
		public void AddDevice(SsdpRootDevice ssdpRootDevice)
		{
			if (string.IsNullOrEmpty(ssdpRootDevice.Uuid))
				throw new ArgumentException(nameof(ssdpRootDevice.Uuid));

			foreach (var ssdpDevicePublisher in _ssdpDevicePublishers)
			{
				var publisherLocation = ssdpDevicePublisher.Key;
				var publisher = ssdpDevicePublisher.Value;

				string ipAddressForUri = publisherLocation.ToUriAddress(_port);

				var deviceDescriptionXml = ssdpRootDevice.ToDescriptionDocument();
				var rootDeviceWithLocation = new SsdpRootDevice(new Uri($"http://{ipAddressForUri}/upnp/description/{ssdpRootDevice.Uuid}"), ssdpRootDevice.CacheLifetime, deviceDescriptionXml);

				_httpDeviceInfoPublisher.AddDeviceInfo(rootDeviceWithLocation.Uuid, rootDeviceWithLocation.ToDescriptionDocument());
				publisher.AddDevice(rootDeviceWithLocation);
			}
		}

		public void RemoveDevice(SsdpRootDevice ssdpRootDevice)
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublishers.Values)
			{
				_httpDeviceInfoPublisher.RemoveDeviceInfo(ssdpRootDevice.Uuid);
				ssdpDevicePublisher.RemoveDevice(ssdpRootDevice);
			}
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
