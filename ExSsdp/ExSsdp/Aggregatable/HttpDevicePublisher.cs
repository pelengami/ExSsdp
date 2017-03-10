using System;
using ExSsdp.Network;
using ExSsdp.Publisher;

namespace ExSsdp.Aggregatable
{
	public sealed class HttpDevicePublisher
	{
		private readonly AggregatableDevicePublisher _aggregatableDevicePublisher;

		public HttpDevicePublisher(INetworkInfoProvider networkInfoProvider, ISsdpDevicePublisherFactory ssdpDevicePublisherFactory)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (ssdpDevicePublisherFactory == null) throw new ArgumentNullException(nameof(ssdpDevicePublisherFactory));

			_aggregatableDevicePublisher = new AggregatableDevicePublisher(networkInfoProvider, ssdpDevicePublisherFactory, 0);
		}

		public static HttpDevicePublisher Create()
		{
			var networkInfoProvider = new NetworkInfoProvider();
			var publisherFactory = new SsdpDevicePublisherFactory();
			return new HttpDevicePublisher(networkInfoProvider, publisherFactory);
		}
	}
}