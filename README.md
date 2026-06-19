# QSense-Examples  

This repository contains documentation, APIs and code examples to help you integrate the **QSense wearable IMU motion sensor platform** into your solution.

## Sensor Interfaces  Doc

The **QSense Motion Sensor Interfaces** document provides a complete reference for integrating QSense IMU sensors into software applications through both Bluetooth Low Energy (BLE) and USB serial communication. 
It describes the device communication protocols, packet-based **Core and Serial interfaces**, memory map structure, configuration registers, time synchronization features, sensor calibration controls, and data logging capabilities. 

The guide also details how to configure sampling rates, streaming behavior, and sensor operating modes, as well as how to parse the different stream data formats (raw sensor data, quaternions, optimized packets, and mixed modes) for real-time or recorded motion analysis. 
This document serves as the primary developer reference for accessing, configuring, streaming, and downloading data from QSense Motion sensors.

## Dongle Interface  

This document describes the serial communication interface of the **QSense USB BLE Dongle**, which simplifies the integration of multiple QSense Motion sensors by exposing up to 13 BLE-connected sensors through a single USB serial connection. 
It explains how to configure and use the dongle, manage sensor discovery and connections, send and receive Core Interface commands, maintain device whitelists, monitor connection status, and control scanning operations. 

The guide serves as a companion to the QSense Sensor Interfaces documentation and provides the information required to build applications that communicate with and manage multiple QSense sensors through the QSense Dongle.

## C# DLLs  

This folder contains the source code for the QSense .NET libraries, which provide ready-to-use implementations of the QSense communication interfaces:

- **QSenseDotNet** – A C# implementation of the QSense Motion Sensor Interface. The communication layer is abstracted through a minimal interface, allowing developers to integrate their preferred BLE or serial communication libraries.
- **QSenseDotNet.Dongle** – A C# implementation of the QSense USB BLE Dongle Interface, enabling communication with multiple QSense sensors through a single USB connection.
- **QSenseDotNet.Uart** – A C# implementation of the QSense Motion Sensor Serial Interface for direct communication with sensors over USB.

Both the QSenseDotNet.Dongle and QSenseDotNet.Uart DLLs are examples of how the communication interface can be implemented.

These libraries are intended to accelerate development by providing reference implementations of the QSense interfaces, eliminating the need to implement the communication protocols from scratch.

## Swift SDK 

This folder contains the source code of the QSenseSwift SDK. Unlike the C# DLLs, this library targets BLE communication only.

### Python examples  

These examples demonstrate how to implement parts of the QSense Motion Sensor Interfaces and the QSense USB BLE Dongle interface.