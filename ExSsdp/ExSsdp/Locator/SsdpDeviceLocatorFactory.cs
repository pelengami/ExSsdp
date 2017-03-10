using System;
using Rssdp;
using Rssdp.Infrastructure;

namespace ExSsdp.Locator
{
	public sealed class SsdpDeviceLocatorFactory : ISsdpDeviceLocatorFactory
	{
		public ISsdpDeviceLocator Create(string ipAddress, int port)
		{
			if (string.IsNullOrEmpty(ipAddress)) throw new InvalidOperationException(nameof(ipAddress));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			var socketFactory = new SocketFactory(ipAddress);
			var ssdpCommunicationsServer = new SsdpCommunicationsServer(socketFactory, port);
			var deviceLocator = new SsdpDeviceLocator(ssdpCommunicationsServer);
			return deviceLocator;
		}
	}
}