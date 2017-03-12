using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExSsdp.Network;
using ExSsdp.Util;

namespace ExSsdp.Http
{
	public sealed class HttpDeviceInfoPublisher
	{
		private readonly NetworkInfoProvider _networkInfoProvider;
		private readonly int _port;
		private readonly int _accepts = 4;
		private readonly HttpListener _httpListener;
		private readonly ConcurrentDictionary<string, string> _deviceLocationAndInfo = new ConcurrentDictionary<string, string>();

		public HttpDeviceInfoPublisher(NetworkInfoProvider networkInfoProvider, int port)
		{
			if (networkInfoProvider == null) throw new ArgumentNullException(nameof(networkInfoProvider));
			if (port < 0) throw new InvalidOperationException(nameof(port));

			_networkInfoProvider = networkInfoProvider;
			_port = port;
			_httpListener = new HttpListener();
			_accepts *= Environment.ProcessorCount;
		}

		public void AddDeviceInfo(string location, string xmlDocument)
		{
			if (_deviceLocationAndInfo.ContainsKey(location))
				return;

			_deviceLocationAndInfo.TryAdd(location, xmlDocument);
		}

		public void Dispose()
		{
			_httpListener.Stop();
		}

		public void Run(CancellationToken cancellationToken)
		{
			foreach (var ipAddressesFromAdapter in _networkInfoProvider.GetIpAddressesFromAdapters())
			{
				var ipAddress = IPAddress.Parse(ipAddressesFromAdapter);

				string prefix;

				switch (ipAddress.AddressFamily)
				{
					case AddressFamily.InterNetwork:
						prefix = $"http://{ipAddressesFromAdapter}:{_port}/upnp/description/";
						break;

					case AddressFamily.InterNetworkV6:
						prefix = $"http://[{ipAddressesFromAdapter}]:{_port}/upnp/description/";
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(ipAddress.AddressFamily));
				}

				Console.WriteLine($"published on: {prefix}");

				_httpListener.Prefixes.Add(prefix);
			}

			try
			{
				_httpListener.Start();
			}
			catch (HttpListenerException exception)
			{
				//todo write to log
				Console.Error.WriteLine(exception.Message);
				throw new InvalidOperationException();
			}

			var semaphore = new Semaphore(_accepts, _accepts);

			var listenerAction = new Action(async delegate
			{
				try
				{
					semaphore.WaitOne();

					var context = await _httpListener.GetContextAsync();

					semaphore.Release();
					await ProcessListenerContextAsync(context);
				}
				catch (Exception)
				{
					//ignore
				}
			});

			Repeater.DoInfinityAsync(listenerAction, TimeSpan.Zero, cancellationToken);
		}

		private async Task ProcessListenerContextAsync(HttpListenerContext listenerContext)
		{
			try
			{
				var requestEndPoint = listenerContext.Request.LocalEndPoint;
				if (requestEndPoint == null)
					return;

				string deviceInfo;

				if (!_deviceLocationAndInfo.TryGetValue(requestEndPoint.ToString(), out deviceInfo))
					return;

				byte[] data = Encoding.UTF8.GetBytes(deviceInfo);

				listenerContext.Response.StatusCode = 200;
				listenerContext.Response.KeepAlive = false;
				listenerContext.Response.ContentLength64 = data.Length;

				var output = listenerContext.Response.OutputStream;
				await output.WriteAsync(data, 0, data.Length);

				listenerContext.Response.Close();
			}
			catch (HttpListenerException)
			{
				// Ignored.
			}
			catch (Exception ex)
			{
				//todo write to log
				Console.Error.WriteLine(ex.Message);
			}
		}
	}
}
