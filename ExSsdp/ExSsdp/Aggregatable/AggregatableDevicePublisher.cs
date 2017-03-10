using System;
using System.Collections.Generic;
using ExSsdp.Network;
using ExSsdp.Publisher;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Aggregatable
{
	public sealed class AggregatableDevicePublisher : IAggregatableDevicePublisher
	{
		private readonly IList<ISsdpDevicePublisher> _ssdpDevicePublisherses = new List<ISsdpDevicePublisher>();

		public AggregatableDevicePublisher(INetworkInfoProvider networkInfoProvider,
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));

			var unicastAddresses = networkInfoProvider.GetIpAddressesFromAdapters();

			AddPublisher(ssdpDevicePublisherFactory, unicastAddresses, port);
		}

		public AggregatableDevicePublisher(IEnumerable<string> unicastAddresses,
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory,
			int port)
		{
			if (unicastAddresses == null) throw new ArgumentNullException(nameof(unicastAddresses));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));

			AddPublisher(ssdpDevicePublisherFactory, unicastAddresses, port);
		}

		public void Dispose()
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublisherses)
			{
				//todo maybe 'byebye notification' should be sent atomatically for each device 
				//in other words need remove all published devices, when Dispose has been called
				//todo interface of publisher should be disposable
				//ssdpDevicePublisher.Dispose();
			}
		}

		public IEnumerable<ISsdpDevicePublisher> Publishers => _ssdpDevicePublisherses;

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

		public void AddDevice(SsdpRootDevice ssdpRootDevice)
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublisherses)
				ssdpDevicePublisher.AddDevice(ssdpRootDevice);
		}

		public void RemoveDevice(SsdpRootDevice ssdpRootDevice)
		{
			foreach (var ssdpDevicePublisher in _ssdpDevicePublisherses)
				ssdpDevicePublisher.RemoveDevice(ssdpRootDevice);
		}

		private void AddPublisher(ISsdpDevicePublisherFactory ssdpDevicePublisherFactory, IEnumerable<string> availableUnicastAddresses, int port)
		{
			foreach (var availableUnicastAddress in availableUnicastAddresses)
			{
				var ssdpDevicePublisher = ssdpDevicePublisherFactory.Create(availableUnicastAddress, port);
				_ssdpDevicePublisherses.Add(ssdpDevicePublisher);
			}
		}
	}
}
