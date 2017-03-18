# ExSsdp

### Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/w9dgggye8576hsal?svg=true)](https://ci.appveyor.com/project/qine/exssdp)

### License
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

### Nuget
[![NuGet](https://img.shields.io/nuget/v/ExSsdp.svg?style=flat-square)](https://www.nuget.org/packages/ExSsdp/)

### Description

It's an extension for the [RSSDP library](https://github.com/Yortw/RSSDP) (the implementation of the ssdp protocol)

It is not a complete implementation of the UPNP;

In which cases you can use it:
- If you want to publish a description of your device on the Http server
- If you want to publish devices on specified networks
- If you want to search devices on specified networks
- Not all upnp devices send 'byebye' notification to the network, you can activate the monitoring mode to check availability of device description

### Example:

#### Publish Device

Create publisher on all adapters with any available port

```c#
        var devicePublisher = AggregatableDevicePublisher.Create();
        
        devicePublisher.AddDevice(ssdpDevice);
```

Create publisher with specified ip addresses, or on all adapters

```c#
       int port = 8090;
       var unicastAddresses = new List<string> { "fe80::8c8:c972:5205:278e" };
       var ssdpDevicePublisherFactory = new SsdpDevicePublisherFactory();
       var httpDeviceInfoPublisher = new HttpDeviceInfoPublisher(port);
       var devicePublisher = new AggregatableDevicePublisher(unicastAddresses, ssdpDevicePublisherFactory, httpDeviceInfoPublisher, port);
       
       
       int port = 8090;
       var networkInfoProvider = new NetworkInfoProvider();
       var ssdpDevicePublisherFactory = new SsdpDevicePublisherFactory();
       var httpDeviceInfoPublisher = new HttpDeviceInfoPublisher(port);
       var devicePublisher = new AggregatableDevicePublisher(networkInfoProvider, ssdpDevicePublisherFactory, httpDeviceInfoPublisher, port);
```

Description of devices will be available on specified ipaddresses and information about location of the device description will be set in the ssdp packet

### #For example:

The description of the device will be available at this address: http://[fe80::f54c:62dd:f94b:3a9c]:1024/upnp/description/7331cc99-a757-46a5-bd99-05ece173ce38

And will have the following description:


```xml
        <root xmlns="urn:schemas-upnp-org:device-1-0">
        <specVersion>
        <major>1</major>
        <minor>0</minor>
        </specVersion>
        <device>
        <deviceType>urn:test-namespace:device:test-device-type:1</deviceType>
        <friendlyName>test-friendly-name</friendlyName>
        <manufacturer>manufacturer</manufacturer>
        <modelName>test-model-name</modelName>
        <UDN>uuid:7331cc99-a757-46a5-bd99-05ece173ce38</UDN>
        </device>
        </root>
```

#### Search and listen for notifications:

```c#
        var deviceLocator = AggregatableDeviceLocator.Create();
        deviceNotificationListener.IsMonitoringEnabled = true;
     
        var deviceLocator.DeviceAvailable += OnDeviceAvailable;
        var deviceLocator.DeviceUnavailable += OnDeviceUnavailable;
        
        var deviceLocator.StartListeningForNotifications();
        
        var devices = await deviceLocator.SearchAsync();
```
