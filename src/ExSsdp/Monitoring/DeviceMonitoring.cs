using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ExSsdp.Http;
using ExSsdp.Util;
using Rssdp;

namespace ExSsdp.Monitoring
{
    internal sealed class DeviceMonitoring : IDisposable
    {
        private readonly ConcurrentDictionary<string, DiscoveredSsdpDevice> _discoveredSsdpDevices = new ConcurrentDictionary<string, DiscoveredSsdpDevice>();
        private readonly HttpAvailabilityChecker _availabilityChecker = new HttpAvailabilityChecker();
        private CancellationTokenSource _httpAvailabilyTokenSource = new CancellationTokenSource();

        public event EventHandler<DeviceUnavailableEventArgs> DeviceUnvailable;

        public void Run()
        {
            if (!_httpAvailabilyTokenSource.IsCancellationRequested)
                _httpAvailabilyTokenSource.Cancel();

            _httpAvailabilyTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(async delegate
            {
                while (!_httpAvailabilyTokenSource.IsCancellationRequested)
                {
                    foreach (var discoveredSsdpDevice in _discoveredSsdpDevices)
                    {
                        var location = discoveredSsdpDevice.Key;
                        var isAvailable = await _availabilityChecker.Check(location);
                        if (isAvailable)
                            continue;

                        var discoveredDevice = discoveredSsdpDevice.Value;

                        DiscoveredSsdpDevice tempDevice;
                        if (_discoveredSsdpDevices.TryRemove(location, out tempDevice))
                            DeviceUnvailable?.Invoke(this, new DeviceUnavailableEventArgs(discoveredDevice, false));
                    }

                    await Task.Delay(10000);
                }
            }, TaskCreationOptions.LongRunning, _httpAvailabilyTokenSource.Token);
        }

        public void Stop()
        {
            if (_httpAvailabilyTokenSource.IsCancellationRequested)
                _httpAvailabilyTokenSource.Cancel();
        }

        public void AddDevice(DiscoveredSsdpDevice device)
        {
            var location = device.DescriptionLocation.ToString();

            if (!_discoveredSsdpDevices.ContainsKey(location))
                _discoveredSsdpDevices.TryAdd(location, device);
        }

        public void RemoveDevice(DiscoveredSsdpDevice device)
        {
            DiscoveredSsdpDevice tempDevice;
            _discoveredSsdpDevices.TryRemove(device.DescriptionLocation.ToString(), out tempDevice);
        }

        public void Dispose()
        {
            if (_httpAvailabilyTokenSource.IsCancellationRequested)
                _httpAvailabilyTokenSource.Cancel();
        }
    }
}
