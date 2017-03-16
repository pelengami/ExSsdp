using System;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Publisher
{
	public sealed class SsdpDevicePublisherFactory : ISsdpDevicePublisherFactory
	{
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public ISsdpDevicePublisher Create(string ipAddress, int port)
		{
			if (string.IsNullOrEmpty(ipAddress)) throw new ArgumentException(ipAddress);
			if (port < 0) throw new ArgumentOutOfRangeException(nameof(port));

			var socketFactory = new SocketFactory(ipAddress);
			var ssdpCommunicationsServer = new SsdpCommunicationsServer(socketFactory, port);
			var ssdpDevicePublisher = new SsdpDevicePublisher(ssdpCommunicationsServer);
			return ssdpDevicePublisher;
		}
	}
}
