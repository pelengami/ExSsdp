using System;
using System.Net;
using System.Net.Sockets;

namespace ExSsdp.Util
{
	public static class IpAddressExtensions
	{
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public static string ToUriAddress(this string ipAddr, int port)
		{
			IPAddress ipAddress;

			if (!IPAddress.TryParse(ipAddr, out ipAddress))
				throw new ArgumentException(nameof(ipAddress));

			string location;

			switch (ipAddress.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					location = $"{ipAddr}:{port}";
					break;

				case AddressFamily.InterNetworkV6:
					location = $"[{ipAddr}]:{port}";
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(ipAddress.AddressFamily));
			}

			return location;
		}
	}
}
