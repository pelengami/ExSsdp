using System;
using System.Net;
using System.Net.Sockets;

namespace ExSsdp.Util
{
	internal static class IpAddressExtensions
	{
		/// <exception cref="ArgumentOutOfRangeException"/>
		public static string ToUriAddress(this string ipAddr, int port)
		{
			var ipAddress = IPAddress.Parse(ipAddr);
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
