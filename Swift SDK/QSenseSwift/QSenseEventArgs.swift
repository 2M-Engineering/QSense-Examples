//
//  QSenseEventArgs.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 18/07/2024.
//

import Foundation
import simd

/// <summary>
/// Provides data for the BatteryReceivedEvent
/// </summary>
public class BatteryReceivedEventArgs
{
    internal init(battery : Float)
    {
        Battery = battery;
    }

    public var Battery : Float;
}


/// <summary>
/// Provides data for the DeviceNameChangedEvent
/// </summary>
public class DeviceNameChangedEventArgs
{
    internal init(name : String)
    {
        Name = name;
    }

    public var Name : String;
}

/// <summary>
/// Provides data for the EnergyReceivedEvent
/// </summary>
public class MotionLevelReceivedEventArgs
{
    internal init(motionLevel : Float)
    {
        MotionLevel = motionLevel;
    }

    public var MotionLevel : Float;
}

/// <summary>
/// Provides data for the StateReceivedEvent
/// </summary>
public class StateReceivedEventArgs
{
    internal init(isOffsetCompensationOn : Bool, isOffsetCompensated : Bool, isMagFieldMappingOn : Bool, isMagFieldMapped : Bool, magFieldMappingProgress : Int32, accSensitivity : SensitivityAcc, gyroSensitivity : SensitivityGyr, isAutoCalOn: Bool, samplingRate : SamplingRates, dataBuffering : Int32, connectionInterval : Float)
    {
        IsOffsetCompensationOn = isOffsetCompensationOn;
        IsOffsetCompensated = isOffsetCompensated;
        IsMagFieldMappingOn = isMagFieldMappingOn;
        IsMagFieldMapped = isMagFieldMapped;
        MagFieldMappingProgress = magFieldMappingProgress;
        AccSensitivity = accSensitivity;
        GyroSensitivity = gyroSensitivity;
        IsAutoCalibrationOn = isAutoCalOn;
        SamplingRate = samplingRate;
        DataBuffering = dataBuffering;
        ConnectionInterval = connectionInterval;
    }

    public var IsOffsetCompensationOn : Bool;
    public var IsOffsetCompensated : Bool;
    public var IsMagFieldMappingOn : Bool;
    public var IsMagFieldMapped : Bool;
    public var MagFieldMappingProgress : Int32;
    public var AccSensitivity : SensitivityAcc;
    public var GyroSensitivity : SensitivityGyr;
    public var IsAutoCalibrationOn : Bool;
    public var SamplingRate : SamplingRates;
    public var DataBuffering : Int32;
    public var ConnectionInterval : Float;
}

/// <summary>
/// Provides data for the DataPacketReadyEvent
/// </summary>
public class StreamPacketReceivedEventArgs
{
    internal init(dataMode : DataModes, deviceAddress : UInt64, acc : [[Float]], gyro : [[Float]], mag : [[Float]],
                quaternion : [simd_quatf], freeAcc : [Float], time : Date, battery : UInt8, annotation : UInt8,
                  syncOk : Bool, interference : MagInterference, buffering : UInt8)
    {
        DataMode = dataMode;
        DeviceAddress = deviceAddress;
        Acc = acc;
        Gyro = gyro;
        Mag = mag;
        Quaternion = quaternion;
        FreeAcc = freeAcc;
        Time = time;
        Battery = battery;
        Annotation = annotation;
        SyncOk = syncOk;
        Interference = interference;
        Buffering = buffering;
    }

    public var DeviceAddress : UInt64;
    public var Acc : [[Float]];
    public var Gyro : [[Float]];
    public var Mag : [[Float]];
    public var Quaternion : [simd_quatf];
    public var FreeAcc : [Float];
    public var Time : Date;
    public var Battery : UInt8;
    public var Annotation : UInt8;
    public var SyncOk : Bool;
    public var DataMode : DataModes;
    public var Interference : MagInterference;
    public var Buffering : UInt8;
}
