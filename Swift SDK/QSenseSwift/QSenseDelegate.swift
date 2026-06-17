//
//  QSenseDelegate.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 25/07/2024.
//

import Foundation
import swift_event
import CoreBluetooth

/// <summary>
/// Occurs when the battery level is received.
/// </summary>
///
public typealias BatteryReceivedEventHandler = EventHandler<BatteryReceivedEventArgs>;
/// <summary>
/// Occurs when a stream packet is received.
/// </summary>
public typealias StreamPacketReceivedEventHandler = EventHandler<StreamPacketReceivedEventArgs>;
/// <summary>
/// Occurs when the name of the device changes
/// </summary>
///
public typealias DeviceNameChangedEventHandler = EventHandler<DeviceNameChangedEventArgs>;
public typealias MagFieldMappingDoneEventHandler = EventHandler<MotionLevelReceivedEventArgs>;
/// <summary>
/// Occurs when the energy level is received.
/// </summary>
///
public typealias MotionLevelReceivedEventHandler = EventHandler<MotionLevelReceivedEventArgs>;
/// <summary>
/// Occurs when the device state is received.
/// </summary>
///
public typealias StateReceivedEventHandler = EventHandler<StateReceivedEventArgs>;

public typealias ManagerStateUpdateEventHandler = EventHandler<ManagerState>;
public typealias ScanCompletedEventHandler = EventHandler<Any?>;
public typealias DiscoverDeviceEventHandler = EventHandler<String>;
public typealias DeviceConnectedEventHandler = EventHandler<QSenseDevice>;
public typealias ConnectionFailedEventHandler = EventHandler<String>;
public typealias DeviceDisconnectedEventHandler = EventHandler<String>;
