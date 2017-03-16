using System;
using ExSsdp.Util;
using Xunit;

namespace TestExSsdp.Util
{
	public sealed class IpAddressExtensionsTest
	{
		[Fact]
		private void ToUriAddress_WhenAddressIsIpv4_ReturnsUriWithoudBrackets()
		{
			var expectedUriAddress = "127.0.0.1:3333";
			var ipAddress = "127.0.0.1";
			var port = 3333;

			var actualUriAddress = ipAddress.ToUriAddress(port);

			Assert.Equal(expectedUriAddress, actualUriAddress);
		}

		[Fact]
		private void ToUriAddress_WhenAddressIsIpv6_ReturnsUriWithBrackets()
		{
			var expectedUriAddress = "[::1]:3333";
			var ipAddress = "::1";
			var port = 3333;

			var actualUriAddress = ipAddress.ToUriAddress(port);

			Assert.Equal(expectedUriAddress, actualUriAddress);
		}

		[Fact]
		private void ToUriAddress_WhenItsNotIpAddress_ThrowsInvalidOperationExceptionException()
		{
			var ipAddress = "any";
			var port = 3333;

			Assert.Throws<ArgumentException>(() => ipAddress.ToUriAddress(port));
		}
	}
}
