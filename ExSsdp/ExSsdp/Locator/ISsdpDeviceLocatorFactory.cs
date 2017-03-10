using System;
using Rssdp.Infrastructure;

namespace ExSsdp.Locator
{
	public interface ISsdpDeviceLocatorFactory
	{
		ISsdpDeviceLocator Create(string ipAddress, int port);
	}
}