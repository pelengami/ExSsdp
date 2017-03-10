using System.Collections.Generic;

namespace ExSsdp.Network
{
	public interface INetworkInfoProvider
	{
		IEnumerable<string> GetIpAddressesFromAdapters();
	}
}
