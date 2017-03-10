using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExSsdp.Util;

namespace ExSsdp.Http
{
	internal sealed class HttpDeviceInfoPublisher
	{
		private readonly int _accepts = 4;
		private readonly HttpListener _httpListener;

		public HttpDeviceInfoPublisher()
		{
			_httpListener = new HttpListener();
			_accepts *= Environment.ProcessorCount;
		}

		public void Run(string uriPrefix, CancellationToken cancellationToken)
		{
			_httpListener.Prefixes.Add(uriPrefix);

			try
			{
				_httpListener.Start();
			}
			catch (HttpListenerException hlex)
			{
				//todo write to log
				Console.Error.WriteLine(hlex.Message);
				throw new InvalidOperationException();
			}

			var semaphore = new Semaphore(_accepts, _accepts);

			var listenerAction = new Action(async delegate
			{
				semaphore.WaitOne();

				var context = await _httpListener.GetContextAsync();

				try
				{
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

		private static async Task ProcessListenerContextAsync(HttpListenerContext listenerContext)
		{
			try
			{
				byte[] data = Encoding.UTF8.GetBytes("");

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
				// TODO: better exception handling
				Trace.WriteLine(ex.ToString());
			}
		}
	}
}
