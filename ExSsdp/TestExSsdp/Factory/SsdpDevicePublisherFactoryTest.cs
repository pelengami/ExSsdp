using System;
using ExSsdp;
using ExSsdp.Publisher;
using Xunit;

namespace TestExSsdp.Factory
{
	public sealed class SsdpDevicePublisherFactoryTest
	{
		[Fact]
		private void Create_WhenArgumentIsNull_ThrowInvalidOperationException()
		{
			var devicePublisherFactory = new SsdpDevicePublisherFactory();

			Assert.Throws<InvalidOperationException>(() => devicePublisherFactory.Create(null, 0));

		}

		[Fact]
		private void Create_WhenArgumentIsEmpty_ThrowInvalidOperationException()
		{
			var devicePublisherFactory = new SsdpDevicePublisherFactory();

			Assert.Throws<InvalidOperationException>(() => devicePublisherFactory.Create(string.Empty, 0));

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
