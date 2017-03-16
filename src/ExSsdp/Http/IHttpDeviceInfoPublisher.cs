using System;
using System.Threading;

namespace ExSsdp.Http
{
	public interface IHttpDeviceInfoPublisher : IDisposable
	{
		void AddDeviceInfo(string deviceUuid, string xmlDocument);
		void RemoveDeviceInfo(string deviceUuid);
		void Run(CancellationToken cancellationToken);
	}
}