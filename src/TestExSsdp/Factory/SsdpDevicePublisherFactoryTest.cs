using System;
using ExSsdp;
using ExSsdp.Publisher;
using Xunit;

namespace TestExSsdp.Factory
{
	public sealed class SsdpDevicePublisherFactoryTest
	{
		[Fact]
		private void Create_WhenArgumentIsNull_ThrowArgumentException()
		{
			var devicePublisherFactory = new SsdpDevicePublisherFactory();

			Assert.Throws<ArgumentException>(() => devicePublisherFactory.Create(null, 0));
		}

		[Fact]
		private void Create_WhenArgumentIsEmpty_ThrowArgumentException()
		{
			var devicePublisherFactory = new SsdpDevicePublisherFactory();

			Assert.Throws<ArgumentException>(() => devicePublisherFactory.Create(string.Empty, 0));
		}

		[Fact]
		private void Create_WhenPortIsLessZero_ThrowArgumentOutOfRangeException()
		{
			var publisherFactory = new SsdpDevicePublisherFactory();

			Assert.Throws<ArgumentOutOfRangeException>(() => publisherFactory.Create("127.0.0.1", -1));
		}

		[Fact]
		private void Create_WhenArgumentIsNotNullAndNotEmpty_DevicePublisherHasBeenCreated()
		{
			var devicePublisherFactory = new SsdpDevicePublisherFactory();
			var publisher = devicePublisherFactory.Create("127.0.0.1", 0);
			Assert.NotNull(publisher);
		}
	}
}
