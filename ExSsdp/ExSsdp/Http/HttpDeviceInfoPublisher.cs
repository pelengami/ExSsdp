using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExSsdp.Util;

namespace ExSsdp.Http
{
	public sealed class HttpDeviceInfoPublisher : IHttpDeviceInfoPublisher
	{
		private readonly int _port;
		private readonly int _accepts = 4;
		private readonly HttpListener _httpListener;
		private readonly ConcurrentDictionary<string, string> _deviceUuidAndInfo = new ConcurrentDictionary<string, string>();

		/// <exception cref="ArgumentOutOfRangeException"/>
		public HttpDeviceInfoPublisher(int port)
		{
			if (port < 0) throw new ArgumentOutOfRangeException(nameof(port));

			_port = port;
			_httpListener = new HttpListener();
			_accepts *= Environment.ProcessorCount;
		}

		public void Dispose()
		{
			_httpListener.Stop();
		}

		public void AddDeviceInfo(string deviceUuid, string xmlDocument)
		{
			if (_deviceUuidAndInfo.ContainsKey(deviceUuid))
				return;

			//todo add log
			_deviceUuidAndInfo.TryAdd(deviceUuid, xmlDocument);
		}

		public void RemoveDeviceInfo(string deviceUuid)
		{
			if (!_deviceUuidAndInfo.ContainsKey(deviceUuid))
				return;

			string tempDeviceInfo;
			//todo add log
			_deviceUuidAndInfo.TryRemove(deviceUuid, out tempDeviceInfo);
		}

		/// <exception cref="InvalidOperationException"/>
		public void Run(CancellationToken cancellationToken)
		{
			Console.WriteLine("http device info publisher, was published: ");

			var locationForUri = $"http://*:{_port}/"; ;

			Console.WriteLine(locationForUri);

			_httpListener.Prefixes.Add(locationForUri);

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

				var segments = listenerContext.Request.Url.Segments;
				var deviceUuid = segments.LastOrDefault();
				if (deviceUuid == default(string))
					return;

				if (!_deviceUuidAndInfo.ContainsKey(deviceUuid))
					return;

				string deviceInfo;
				if (!_deviceUuidAndInfo.TryGetValue(deviceUuid, out deviceInfo))
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
