using System;
using ExSsdp.Aggregatable;
using Rssdp;

namespace Example
{
	internal class Program
	{
		private static AggregatableDevicePublisher _devicePublisher;
		private static AggregatableDeviceLocator _deviceNotificationListener;
		private static AggregatableDeviceLocator _deviceSeacher;

		private static void Main(string[] args)
		{
			PrintOptions();

			var key = new ConsoleKeyInfo();

			while (key.Key == 0 || string.Compare(key.KeyChar.ToString(), "X", StringComparison.OrdinalIgnoreCase) != 0)
			{
				Console.WriteLine();
				Console.Write("Enter command: ");
				key = Console.ReadKey();
				Console.WriteLine();

				ProcessCommand(key.KeyChar.ToString().ToLowerInvariant());
			}

            _devicePublisher?.Dispose();
		    _deviceNotificationListener?.Dispose();
		    _deviceSeacher?.Dispose();
		}

		private static void PublishDevice()
		{
			_devicePublisher?.Dispose();
			_devicePublisher = AggregatableDevicePublisher.Create();

			var ssdpDevice = CreateSsdpDevice();
			_devicePublisher.AddDevice(ssdpDevice);
		}

		private static async void SearchDevices()
		{
			_deviceSeacher?.Dispose();
			_deviceSeacher = AggregatableDeviceLocator.Create();

			Console.WriteLine("Wait please");

			var devices = await _deviceSeacher.SearchAsync();

			foreach (var discoveredSsdpDevice in devices)
			{
				Console.WriteLine($"Usn: {discoveredSsdpDevice.Usn}");
				Console.WriteLine($"Location: {discoveredSsdpDevice.DescriptionLocation}");
			}

			Console.WriteLine();
			Console.WriteLine("Search completed");
		}

		private static void ListenForNotifications()
		{
			_deviceNotificationListener?.Dispose();

			_deviceNotificationListener = AggregatableDeviceLocator.Create();

			_deviceNotificationListener.IsMonitoringEnabled = true;

			_deviceNotificationListener.DeviceAvailable += OnDeviceAvailable;

			_deviceNotificationListener.DeviceUnavailable += OnDeviceUnavailable;

			_deviceNotificationListener.StartListeningForNotifications();
		}

		private static void OnDeviceAvailable(object sender, DeviceAvailableEventArgs args)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Device available");
			Console.WriteLine($"Usn: {args.DiscoveredDevice.Usn}");
			Console.WriteLine($"Location: {args.DiscoveredDevice.DescriptionLocation}");
			Console.ResetColor();
		}

		private static void OnDeviceUnavailable(object sender, DeviceUnavailableEventArgs args)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Device unvailable");
			Console.WriteLine($"Usn: {args.DiscoveredDevice.Usn}");
			Console.WriteLine($"Location: {args.DiscoveredDevice.DescriptionLocation}");
			Console.ResetColor();
		}

		private static SsdpRootDevice CreateSsdpDevice()
		{
			var ssdpRootDevice = new SsdpRootDevice
			{
				CacheLifetime = TimeSpan.FromSeconds(60),
				DeviceTypeNamespace = "test-namespace",
				DeviceType = "test-device-type",
				FriendlyName = "test-friendly-name",
				Manufacturer = "manufacturer",
				ModelName = "test-model-name",
				Uuid = Guid.NewGuid().ToString(),
				//not needed to set the location, it will be set automatically for each publisher
				//Location = new Uri("location")
			};

			return ssdpRootDevice;
		}

		private static void PrintOptions()
		{
			Console.WriteLine("P to publish device");
			Console.WriteLine("L to listen for notifications");
			Console.WriteLine("S to search for all devices");
			Console.WriteLine("X to exit");
			Console.WriteLine();
		}

		private static void ProcessCommand(string command)
		{
			switch (command)
			{
				case "p":
					PublishDevice();
					break;

				case "l":
					ListenForNotifications();
					break;

				case "s":
					SearchDevices();
					break;
			}
		}
	}
}
