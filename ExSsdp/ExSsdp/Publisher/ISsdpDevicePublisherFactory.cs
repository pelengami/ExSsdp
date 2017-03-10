using System;
using Rssdp.Infrastructure;

namespace ExSsdp.Publisher
{
	public interface ISsdpDevicePublisherFactory
	{
		ISsdpDevicePublisher Create(string ipAddress, int port);
	}
}
