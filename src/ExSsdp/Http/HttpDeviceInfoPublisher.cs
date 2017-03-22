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
            _deviceUuidAndInfo.TryRemove(deviceUuid, out tempDeviceInfo);
        }

        /// <exception cref="InvalidOperationException"/>
        public void Run(CancellationToken cancellationToken)
        {
            Console.WriteLine("Was published: ");

            var locationForUri = $"http://*:{_port}/"; ;

            Console.WriteLine(locationForUri);

            _httpListener.Prefixes.Add(locationForUri);

            try
            {
                _httpListener.Start();
            }
            catch (HttpListenerException exception)
            {
                Console.Error.WriteLine(exception.Message);
                throw new InvalidOperationException();
            }

            var semaphore = new Semaphore(_accepts, _accepts);

            Task.Factory.StartNew(async delegate
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await semaphore.WaitOneAsync(cancellationToken);
                        var context = await _httpListener.GetContextAsync();
                        await ProcessListenerContextAsync(context, cancellationToken);
                        semaphore.Release();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
            }, TaskCreationOptions.LongRunning, cancellationToken);
        }

        public void Dispose()
        {
            _httpListener.Stop();
        }

        private async Task ProcessListenerContextAsync(HttpListenerContext listenerContext, CancellationToken cancellationToken)
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
                await output.WriteAsync(data, 0, data.Length, cancellationToken);

                listenerContext.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Ignored.
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
