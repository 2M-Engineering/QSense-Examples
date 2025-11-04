# QSense-Examples  

This repository contains documentation, APIs and code examples to help you integrate the **QSense wearable IMU motion sensor platform** into your solution.

## Sensor Interfaces  

The QSense Motion Sensor provides two interfaces: **Core Interface** and **Serial Interface**.  
Both interfaces enable you to read and write to the sensor’s memory, as well as start and stop data streaming.  

The code examples in this folder demonstrate how to:  
- Create Core and Serial interface packets  
- Parse incoming packets from QSense sensors  

## Dongle Interface  

The **Dongle Interface** allows you to use the **QSense USB BLE Dongle** to communicate with and control multiple QSense Motion Sensors through a single serial port.  
This significantly simplifies communication with multiple sensors.  

The examples in this folder show how to create and parse packets using the Dongle Interface.  

## C# APIs  

You can find our C# implementations of the interfaces as **ready-to-use Dynamic Link Libraries (DLLs)**.  
These DLL examples are intended for developers who prefer not to implement the QSense Sensor and Dongle interfaces from scratch.  

The **C# APIs folder** also includes **HTML documentation** for both DLLs, providing detailed information about their classes, methods, and usage.  

### QSenseDotNet DLL  

The `QSenseDotNet` DLL implements the **Core Interface** of the QSense Motion Sensor.  
The communication layer is abstracted into an interface, allowing you to choose your preferred BLE library—or even use your own dongle if desired.  

These examples demonstrate how to use the `QSenseDotNet` DLL, which relies on the external library [Plugin.BLE](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le) to handle BLE communication.  

> **Note:** Using `Plugin.BLE` is **not mandatory**. You are free to implement your own BLE communication layer or integrate any other third-party BLE library that fits your project requirements.  

### QSenseDotNet.Dongle DLL  

The `QSenseDotNet.Dongle` DLL extends `QSenseDotNet` by adding support for serial communication with the **QSense USB BLE Dongle**.  
The examples in this folder demonstrate how to use the `QSenseDotNet.Dongle` DLL to control multiple devices via the Dongle Interface.  
