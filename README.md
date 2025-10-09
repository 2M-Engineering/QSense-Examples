# QSense-Examples

This repository contains code examples to help you integrate the **QSense wearable IMU motion sensor platform** into your solution.

## Sensor Interfaces

This folder contains **C# examples** and **Python examples** demonstrating how to work with the QSense Sensor interfaces. These examples show how to:

- Create Core and Serial interface packets.
- Parse incoming packets from the QSense sensors.

## Dongle Interface

This folder includes a **C# example** and **Python examples** illustrating how to implement the **Dongle Serial interface** for communicating with QSense devices.

## C# APIs

The examples in this folder are intended for developers who prefer not to implement the QSense Sensor and Dongle interfaces from scratch. Instead, you can use our **ready-to-use Dynamic Link Libraries (DLLs)**.
> **Note:** The use of `Plugin.BLE` is **not mandatory**. You are free to implement your own BLE communication layer or integrate any other third-party BLE library that suits your project requirements.
### QSenseDotNet DLL

These examples demonstrate how to use the `QSenseDotNet` DLL, which implements both the Sensor and Dongle interfaces. It uses an external library, [Plugin.BLE](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le), to handle wireless BLE communication. 

### QSenseDotNet.Dongle DLL

This example shows how to use the `QSenseDotNet.Dongle` DLL to control multiple devices via the dongle interface.