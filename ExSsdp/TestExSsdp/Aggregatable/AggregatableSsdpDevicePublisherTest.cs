using System;
using System.Collections.Generic;
using System.Linq;
using ExSsdp;
using ExSsdp.Aggregatable;
using ExSsdp.Network;
using ExSsdp.Publisher;
using Moq;
using Rssdp;
using Rssdp.Infrastructure;
using Xunit;

namespace TestExSsdp.Aggregatable
{
	public class AggregatableSsdpDevicePublisherTest
	{
		[Fact]
		public void Ctor_WhenFirstArgumentIsNull_ThrowArgumentNullException1()
		{
			INetworkInfoProvider networkInfoProvider = null;
			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();

			Assert.Throws<ArgumentNullException>(() => new AggregatableDevicePublisher(networkInfoProvider, ssdpDevicePublisherFactoryMock.Object, 0));
		}

		[Fact]
		public void Ctor_WhenSecondArgumentIsNull_ThrowArgumentNullException1()
		{
			var networkInfoProvider = new Mock<INetworkInfoProvider>();
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory = null;

			Assert.Throws<ArgumentNullException>(() => new AggregatableDevicePublisher(networkInfoProvider.Object, ssdpDevicePublisherFactory, 0));
		}

		[Fact]
		public void Ctor_WhenFirstArgumentIsNull_ThrowArgumentNullException2()
		{
			List<string> unicastAddresses = null;
			var ssdpDevicePublisherFactory = new Mock<ISsdpDevicePublisherFactory>();

			Assert.Throws<ArgumentNullException>(() => new AggregatableDevicePublisher(unicastAddresses, ssdpDevicePublisherFactory.Object, 0));
		}

		[Fact]
		public void Ctor_WhenSecondArgumentIsNull_ThrowArgumentNullException2()
		{
			var unicastAddresses = new List<string>();
			ISsdpDevicePublisherFactory ssdpDevicePublisherFactory = null;

			Assert.Throws<ArgumentNullException>(() => new AggregatableDevicePublisher(unicastAddresses, ssdpDevicePublisherFactory, 0));
		}

		[Fact]
		public void Ctor_WhenProvidedEmptyListOfUnicastAddresses_PublishersAreNotCreated()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>());
			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();

			//# Act
			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Assert
			Assert.True(!aggregatablePublisher.Publishers.Any());
		}

		[Fact]
		public void Ctor_WhenNoUnicastAddresses_PublishersAreNotCreated()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>());
			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();

			//# Act
			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Assert
			ssdpDevicePublisherFactoryMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
		}

		[Fact]
		public void Ctor1_WhenHasUnicastAddresses_PublishersHasBeenCreatedForEachInterface()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});
			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();

			//# Act
			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Assert
			ssdpDevicePublisherFactoryMock.Verify(f => f.Create("127.0.0.1", 0), Times.Once);
			ssdpDevicePublisherFactoryMock.Verify(f => f.Create("::1", 0), Times.Once);
		}

		[Fact]
		public void Ctor2_WhenHasUnicastAddresses_PublishersHasBeenCreatedForEachInterface()
		{
			//# Arrange
			var unicastAddresses = new List<string>
			{
				"127.0.0.1",
				"::1"
			};
			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();

			//# Act
			var aggregatablePublisher = new AggregatableDevicePublisher(unicastAddresses, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Assert
			ssdpDevicePublisherFactoryMock.Verify(f => f.Create("127.0.0.1", 0), Times.Once);
			ssdpDevicePublisherFactoryMock.Verify(f => f.Create("::1", 0), Times.Once);
		}

		[Fact]
		public void Dtor_WhenHasPublishers_EachPublisherHasBeenDisposed()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});

			var devicePublisherFirstMock = new Mock<ISsdpDevicePublisher>();
			var devicePublisherSecondMock = new Mock<ISsdpDevicePublisher>();

			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();
			ssdpDevicePublisherFactoryMock.SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<int>()))
				.Returns(devicePublisherFirstMock.Object)
				.Returns(devicePublisherSecondMock.Object);

			IAggregatableDevicePublisher aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Act
			aggregatablePublisher.Dispose();

			//# Assert
			//devicePublisherFirstMock.Verify(p => p.Dispose());
			//devicePublisherSecondMock.Verify(p => p.Dispose());
		}

		[Fact]
		public void AddDevice_HasBeenAddedDeviceForEachPublisher()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});

			var ssdpRootDevice = new SsdpRootDevice();

			var devicePublisherFirstMock = new Mock<ISsdpDevicePublisher>();
			var devicePublisherSecondMock = new Mock<ISsdpDevicePublisher>();

			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();
			ssdpDevicePublisherFactoryMock.SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<int>()))
				.Returns(devicePublisherFirstMock.Object)
				.Returns(devicePublisherSecondMock.Object);

			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Act
			aggregatablePublisher.AddDevice(ssdpRootDevice);

			//# Assert
			devicePublisherFirstMock.Verify(p => p.AddDevice(ssdpRootDevice));
			devicePublisherSecondMock.Verify(p => p.AddDevice(ssdpRootDevice));
		}

		[Fact]
		public void RemoveDevice_HasBeenRemovedDeviceFromEachPublisher()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});

			var ssdpRootDevice = new SsdpRootDevice();

			var devicePublisherFirstMock = new Mock<ISsdpDevicePublisher>();
			var devicePublisherSecondMock = new Mock<ISsdpDevicePublisher>();

			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();
			ssdpDevicePublisherFactoryMock.SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<int>()))
				.Returns(devicePublisherFirstMock.Object)
				.Returns(devicePublisherSecondMock.Object);

			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Act
			aggregatablePublisher.RemoveDevice(ssdpRootDevice);

			//# Assert
			devicePublisherFirstMock.Verify(p => p.RemoveDevice(ssdpRootDevice));
			devicePublisherSecondMock.Verify(p => p.RemoveDevice(ssdpRootDevice));
		}

		[Fact]
		public void Device_WhenPublishersHasDevices_ReturnsAggregatedDevices()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});

			var ssdpRootDevice = new SsdpRootDevice();

			var devicePublisherFirstMock = new Mock<ISsdpDevicePublisher>();
			var devicePublisherSecondMock = new Mock<ISsdpDevicePublisher>();
			devicePublisherFirstMock.Setup(p => p.Devices).Returns(() => new List<SsdpRootDevice> { ssdpRootDevice });
			devicePublisherSecondMock.Setup(p => p.Devices).Returns(() => new List<SsdpRootDevice> { ssdpRootDevice });

			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();
			ssdpDevicePublisherFactoryMock.SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<int>()))
				.Returns(devicePublisherFirstMock.Object)
				.Returns(devicePublisherSecondMock.Object);

			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Act
			aggregatablePublisher.AddDevice(ssdpRootDevice);

			//# Assert
			Assert.Equal(2, aggregatablePublisher.Devices.Count());
		}

		[Fact]
		public void Publishers_WhenHasSeveralPublishers_ReturnsPublishers()
		{
			//# Arrange
			var networkInfoProviderMock = new Mock<INetworkInfoProvider>();
			networkInfoProviderMock.Setup(n => n.GetIpAddressesFromAdapters()).Returns(new List<string>
			{
				"127.0.0.1",
				"::1"
			});

			var devicePublisherFirstMock = new Mock<ISsdpDevicePublisher>();
			var devicePublisherSecondMock = new Mock<ISsdpDevicePublisher>();

			var ssdpDevicePublisherFactoryMock = new Mock<ISsdpDevicePublisherFactory>();
			ssdpDevicePublisherFactoryMock.SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<int>()))
				.Returns(devicePublisherFirstMock.Object)
				.Returns(devicePublisherSecondMock.Object);


			//# Act
			var aggregatablePublisher = new AggregatableDevicePublisher(networkInfoProviderMock.Object, ssdpDevicePublisherFactoryMock.Object, 0);

			//# Assert
			Assert.Equal(2, aggregatablePublisher.Publishers.Count());
			Assert.Equal(devicePublisherFirstMock.Object, aggregatablePublisher.Publishers.ElementAt(0));
			Assert.Equal(devicePublisherSecondMock.Object, aggregatablePublisher.Publishers.ElementAt(1));
		}
	}
}
