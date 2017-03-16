using System;
using ExSsdp;
using ExSsdp.Locator;
using Xunit;

namespace TestExSsdp.Factory
{
	public sealed class SsdpDeviceLocatorFactoryTest
	{
		[Fact]
		private void Create_WhenArgumentIsNull_ThrowArgumentException()
		{
			var deviceLocatorFactory = new SsdpDeviceLocatorFactory();

			Assert.Throws<ArgumentException>(() => deviceLocatorFactory.Create(null, 0));
		}

		[Fact]
		private void Create_WhenArgumentIsEmpty_ThrowArgumentException()
		{
			var deviceLocatorFactory = new SsdpDeviceLocatorFactory();

			Assert.Throws<ArgumentException>(() => deviceLocatorFactory.Create(string.Empty, 0));
		}

		[Fact]
		private void Create_WhenPortIsLessZero_ThrowArgumentOutOfRangeException()
		{
			var deviceLocatorFactory = new SsdpDeviceLocatorFactory();

			Assert.Throws<ArgumentOutOfRangeException>(() => deviceLocatorFactory.Create("127.0.0.1", -1));
		}

		[Fact]
		private void Create_WhenArgumentIsNotNullAndNotEmpty_DevicePublisherHasBeenCreated()
		{
			var deviceLocatorFactory = new SsdpDeviceLocatorFactory();
			var publisher = deviceLocatorFactory.Create("127.0.0.1", 0);

			Assert.NotNull(publisher);
		}
	}
}
