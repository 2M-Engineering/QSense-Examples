//
//  Device.swift
//  
//
//  Created by 2M Engineering ltd on 19/07/2024.
//

import Foundation
import CoreBluetooth
import simd
import swift_event

public class QSenseDevice
{
    private enum DeviceState : Int
    {
        case DISCONNECTED
        case CONNECTING
        case CONNECTED
        case OFFSET_COMPENSATION
        case FIELD_MAPPING
        case STREAMING
    }

    //#region Fields
    private var state : DeviceState = .DISCONNECTED;
    private var parser : BleParser?;
    private var bleApi : BleApi?;
    private var ctrl : MemMapCtrl?;
    private var streamingData : Bool = false;
    private var accSensitivity : Float = 0;
    private var gyrSensitivity : Float = 0;
    private var stateDevice : UInt16 = 0;
    //#endregion
    
    
    private var DataReceivedEventHandler : EventHandler<DataReceivedEventArgs>?;
    private var Ble2MDataEventHandler : EventHandler<Ble2MDataEventArgs>?;
    private var Ble2MTxEventHandler : EventHandler<Ble2MTxEventArgs>?;
    private var Ble2MWriteCompletEventHandler : EventHandler<Ble2MDataEventArgs>?;

    internal var peripheral : CBPeripheral { get { return parser!.peripheral; } }
    
    /// <summary>
    /// Sensitivity of the accelerometer
    /// </summary>
    public internal(set) var AccSensitivity : SensitivityAcc = .G2;
    /// <summary>
    /// Device address
    /// </summary>
    public internal(set) var SerialNumber : String = "";
    /// <summary>
    /// Battery level
    /// </summary>
    public internal(set) var Battery : Float = 0;
    /// <summary>
    /// Connection interval in milliseconds
    /// </summary>
    public internal(set) var ConnectionInterval : Float = 0;
    /// <summary>
    /// Number of raw samples that are buffered in each stream packet
    /// </summary>
    public internal(set) var DataBuffering : Int = 0;
    /// <summary>
    /// Sensitivity of the gyroscope
    /// </summary>
    public internal(set) var GyrSensitivity : SensitivityGyr = .Dps250;
    /// <summary>
    /// True if the Device is connected
    /// </summary>
    public var IsConnected : Bool { get { return state != .DISCONNECTED; } }
    /// <summary>
    /// True if the magnetic field mapping has been performed before.
    /// </summary>
    public private(set) var IsMagFieldMapped : Bool = false;
    /// <summary>
    /// True if the magnetic field mapping is on.
    /// </summary>
    public private(set) var IsMagFieldMappingOn : Bool = false;
    /// <summary>
    /// True if the offset has been compensated before.
    /// </summary>
    public private(set) var IsOffsetCompensated : Bool = false;
    /// <summary>
    /// True if the offset compensation is on.
    /// </summary>
    public private(set) var IsOffsetCompensationOn : Bool = false;
    /// <summary>
    /// True if the gyroscope autocalibration  is on.
    /// </summary>
    public private(set) var IsAutoCalibrationOn : Bool = false;
    /// <summary>
    /// Magnetometer calibration progress (percentage)
    /// </summary>
    public private(set) var MagFieldMappingProgress : Int = 0;
    /// <summary>
    ///  Maximum length of data (in bytes) that can be transmitted to the Device
    /// </summary>
    public var MaxPacketSize : Int { get { return bleApi!.MaxPacketSize; } }
    /// <summary>
    ///  Motion level of the device
    /// </summary>
    public private(set) var MotionLevel : Float = 0;
    /// <summary>
    /// Device Name
    /// </summary>
    public private(set) var Name : String = "";
    /// <summary>
    /// Current sampling rate
    /// </summary>
    public private(set) var SamplingRate : SamplingRates = .Hz100;
    /// <summary>
    /// Algorithm Selection
    /// </summary>
    public private(set) var AlgorithmSelection : Algorithms = ._9Dof;
    /// <summary>
    /// Device Version
    /// </summary>
    public private(set) var Version : String = "";
    /// <summary>
    /// Flag indicating if the sensor is logging data
    /// </summary>
    public private(set) var IsLogging : Bool = false;
    /// <summary>
    /// Flag indicating if the sensor is acting as master for the TimeSync
    /// </summary>
    public private(set) var IsTimeSyncMaster : Bool = false;
    /// <summary>
    /// Flag indicating if the TimeSync is enabled
    /// </summary>
    public private(set) var IsTimeSyncEnabled : Bool = false;
    /// <summary>
    /// Device Data Mode
    /// </summary>
    public private(set) var DataMode : DataModes = .Mixed;
    /// <summary>
    /// Sync Status
    /// </summary>
    public private(set) var SyncStatus : UInt8 = 0;
    /// <summary>
    /// Annotation
    /// </summary>
    public private(set) var Marker : UInt8 = 0;

    /// <summary>
    /// Occurs when the battery level is received.
    /// </summary>
    public let BatteryReceived = Event<BatteryReceivedEventArgs>.create();
    /// <summary>
    /// Occurs when the name of the device changes
    /// </summary>
    public let DeviceNameChanged = Event<DeviceNameChangedEventArgs>.create();
    /// <summary>
    /// Occurs when the magnetometer calibration has finished.
    /// </summary>
    public let MagFieldMappingDone = Event<Int>.create();
    /// <summary>
    /// Occurs when the magnetometer calibration times out.
    /// </summary>
    public let MagFieldMappingFailed = Event<Int>.create();
    /// <summary>
    /// Occurs when the energy level is received.
    /// </summary>
    public let MotionLevelReceived = Event<MotionLevelReceivedEventArgs>.create();
    /// <summary>
    /// Occurs when the device state is received.
    /// </summary>
    public let StateReceived = Event<StateReceivedEventArgs>.create();
    /// <summary>
    /// Occurs when a stream packet is received.
    /// </summary>
    public let StreamPacketReceived = Event<StreamPacketReceivedEventArgs>.create();
    /// <summary>
    /// Occurs when the data mode is received.
    /// </summary>
    public let DataModeReceived = Event<DataModes>.create();
    /// <summary>
    /// Occurs when the sync status has changed.
    /// </summary>
    public let SyncStatusChanged = Event<UInt8>.create();

    internal let InitializationDone = Event<QSenseDevice>.create();
    /// <summary>
    /// Initialises the QSense Motion Device and start listening for received data.
    /// </summary>
    internal init(parser : BleParser)
    {
        self.DataReceivedEventHandler = EventHandler<DataReceivedEventArgs>(handle: { sender, args in self.BleParse_DataEvent(e: args)});
        self.Ble2MDataEventHandler = EventHandler<Ble2MDataEventArgs>(handle: { sender, args in self.BleApi_Ble2MDataEvent(e: args)});
        self.Ble2MTxEventHandler = EventHandler<Ble2MTxEventArgs>(handle: { sender, args in self.BleApi_Ble2MTxEvent(e: args)});
        self.Ble2MWriteCompletEventHandler = EventHandler<Ble2MDataEventArgs>(handle: { sender, args in self.BleApi_Ble2MWriteCompletEvent(e: args)});
        
        bleApi = BleApi();
        bleApi!.Ble2MDataEvent.event += self.Ble2MDataEventHandler!;
        bleApi!.Ble2MTxEvent.event += self.Ble2MTxEventHandler!;
        bleApi!.Ble2MWriteCompletEvent.event += self.Ble2MWriteCompletEventHandler!;

        self.parser = parser;
        self.parser!.DataReceived.event += DataReceivedEventHandler!;
        
        bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_pin, data: withUnsafeBytes(of: MemMap.MEM_MAP_PIN.littleEndian) { Array($0) });    //enable memory write in the Device
          
        
        state = DeviceState.CONNECTED;
    }

    /// <summary>
    /// Reads QSense Motion Device memory.
    /// </summary>
    public func ReadMemory() throws
    {
        try AssertIsConnected();

        if (state.rawValue > 1 && state.rawValue < 5) //Connected and not streaming
        {
            bleApi!.ReadMemory(address: MemMap.MEM_MAP_CTRL_ADDR, length: UInt16.init(MemMap.MEM_MAP_CTRL_SIZE));
        }
    }

    public func Reset()
    {
        if (parser != nil)
        {
            parser!.DataReceived.event -= DataReceivedEventHandler!;
            parser = nil;
        }
        bleApi!.Ble2MDataEvent.event -= Ble2MDataEventHandler!;
        bleApi!.Ble2MTxEvent.event -= Ble2MTxEventHandler!;
        bleApi!.Ble2MWriteCompletEvent.event -= Ble2MWriteCompletEventHandler!;


        bleApi = BleApi();
        Name = "";
        ctrl = nil;
        state = DeviceState.DISCONNECTED;
        streamingData = false;
        Battery = 0.0;
        Version = "";
    }

    public func SetAccSensitivity(value : SensitivityAcc)
    {
        var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
        data[0] = ((data[0] & 0xF3) + (UInt8.init(value.rawValue) << 2));
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            switch (value)
            {
                case SensitivityAcc.G2:
                    accSensitivity = 0.000061;
                    break;
                case SensitivityAcc.G16:
                    accSensitivity = 0.000488;
                    break;
                case SensitivityAcc.G4:
                    accSensitivity = 0.000122;
                    break;
                case SensitivityAcc.G8:
                    accSensitivity = 0.000244;
                    break;
            }
            stateDevice = Utilities.ToUInt16(data: data, startIndex: 0);
        }
    }

    public func SetDataBuffering(value : Int)
    {
        var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
        data[1] = ((data[1] & 0xf) + (UInt8.init(value << 4)));
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            stateDevice = Utilities.ToUInt16(data: data, startIndex: 0);
        }
    }

    public func SetDataMode(value : DataModes)
    {
        let data : [UInt8] = withUnsafeBytes(of: value.rawValue.littleEndian) { Array($0) };
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_data_mode, data: data);
            DataMode = value;
        }
    }

    public func SetDeviceName(name : String)
    {
        var data : [UInt8] = [UInt8](repeating: 0x00, count: 8);
        let bytesName : [UInt8] = [UInt8](name.utf8);
        for i in 0..<8
        {
            if (bytesName.count > i) 
            {
                data[i] = bytesName[i];
            }
            else
            {
                data[i] = 0;
            }
        }
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_name, data: data);
            Name = name;
            DeviceNameChanged.invoke(self, DeviceNameChangedEventArgs(name: name));
        }
    }

    public func SetGyrSensitivity(value : SensitivityGyr)
    {
        var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
        data[0] = ((data[0] & 0x0F) + (UInt8.init(value.rawValue) << 4));
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            switch (value)
            {
                case SensitivityGyr.Dps250:
                    gyrSensitivity = 0.008750;
                    break;
                case SensitivityGyr.Dps125:
                    gyrSensitivity = 0.004375;
                    break;
                case SensitivityGyr.Dps500:
                    gyrSensitivity = 0.0175;
                    break;
                case SensitivityGyr.Dps1000:
                    gyrSensitivity = 0.035;
                    break;
                case SensitivityGyr.Dps2000:
                    gyrSensitivity = 0.07;
                    break;
            }
            stateDevice = Utilities.ToUInt16(data: data, startIndex: 0);
        }
    }

    /// <summary>
    /// Sets color and animation of the QSense Motion Device LED.
    /// </summary>
    /// <param name="red">Intensity of red color</param>
    /// <param name="green">Intensity of green color</param>
    /// <param name="blue">Intensity of blue color</param>
    /// <param name="animation">LED animation to display. This parameter accepts two values: 0 (blinking LED) and 1 (fixed LED)</param>
    public func SetLEDAnimation(red : UInt8, green : UInt8, blue : UInt8, animation : LEDAnimation)
    {
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_ui_state, data: [ UInt8.init(animation.rawValue), blue, green, red]);
        }
    }

    public func SetSamplingRate(value : SamplingRates)
    {
        var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
        data[1] = ((data[1] & 0xF0) + UInt8.init(value.rawValue));
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            stateDevice = Utilities.ToUInt16(data: data, startIndex: 0);
        }
    }

    public func SetSensorConfig(accSens : SensitivityAcc, gyrSens : SensitivityGyr, sampRate : SamplingRates, buffering : Int)
    {
        var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
        data[0] &= 0x3;
        data[0] |= (UInt8.init(accSens.rawValue) << 2);
        data[0] |= (UInt8.init(gyrSens.rawValue) << 4);
        data[1] = (UInt8.init(sampRate.rawValue) & 0xf) & (UInt8.init(buffering) << 4);
        if (Utilities.ToUInt16(data: data, startIndex: 0) == stateDevice)
        {
            return;
        }
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            stateDevice = Utilities.ToUInt16(data: data, startIndex: 0);
        }
        switch (accSens)
        {
            case SensitivityAcc.G2:
                accSensitivity = 0.000061;
                break;
            case SensitivityAcc.G16:
                accSensitivity = 0.000488;
                break;
            case SensitivityAcc.G4:
                accSensitivity = 0.000122;
                break;
            case SensitivityAcc.G8:
                accSensitivity = 0.000244;
                break;
        }
        switch (gyrSens)
        {
            case SensitivityGyr.Dps250:
                gyrSensitivity = 0.008750;
                break;
            case SensitivityGyr.Dps125:
                gyrSensitivity = 0.004375;
                break;
            case SensitivityGyr.Dps500:
                gyrSensitivity = 0.0175;
                break;
            case SensitivityGyr.Dps1000:
                gyrSensitivity = 0.035;
                break;
            case SensitivityGyr.Dps2000:
                gyrSensitivity = 0.07;
                break;
        }
    }
    
    public func SetAlgorithm(value : Algorithms)
    {
        let data : [UInt8] = withUnsafeBytes(of: value.rawValue.littleEndian) { Array($0) };
        if (bleApi != nil && bleApi!.Connected)
        {
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_algorithm_selection, data: data);
            AlgorithmSelection = value;
        }
    }

    /// <summary>
    /// Starts QSense Motion magnetic field mapping.
    /// </summary>
    public func StartMagFieldMapping() throws
    {
        try AssertIsConnected();

        state = DeviceState.FIELD_MAPPING;
        let handler : EventHandler<StateReceivedEventArgs> = EventHandler<StateReceivedEventArgs>(handle: { sender, args in self.GetMagFieldMappingState(e: args)});
        StateReceived.event -= handler;
        StateReceived.event += handler;
        
        if (bleApi!.Connected)
        {
            var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
            data[0] |= 0x2;
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
            MagFieldMappingProgress = 0;
        }
    }

    /// <summary>
    /// Starts QSense Motion offset compensation.
    /// </summary>
    public func StartOffsetCompensation() throws
    {
        try AssertIsConnected();

        state = DeviceState.OFFSET_COMPENSATION;
        if (bleApi!.Connected)
        {
            var data : [UInt8] = withUnsafeBytes(of: stateDevice.littleEndian) { Array($0) };
            data[0] |= 0x01;
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_device_state, data: data);
        }
    }

    /// <summary>
    /// Starts QSense Motion Device data streaming.
    /// </summary>
    public func StartStreaming() throws
    {
        try AssertIsConnected();

        state = DeviceState.STREAMING;
        if (bleApi!.Connected)
        {
            bleApi!.StreamMemory(address: MemMap.MEM_MAP_CONF_ADDR, length: UInt16.init(MemMap.MEM_MAP_CONF_SIZE));
        }
        streamingData = true;
    }

    /// <summary>
    /// Stops QSense Motion Device data streaming.
    /// </summary>
    public func StopStreaming() throws
    {
        try AssertIsConnected();

        state = DeviceState.CONNECTED;
        if (bleApi!.Connected)
        {
            bleApi!.Abort();
        }
        streamingData = false;
    }

    /// <summary>
    /// Starts QSense Motion Device data streaming.
    /// </summary>
    public func StartLogging() throws
    {
        try AssertIsConnected();

        state = DeviceState.STREAMING;
        if (bleApi!.Connected)
        {
            let data: [UInt8] = [0x01];
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_logging, data: data);
        }
    }

    /// <summary>
    /// Stops QSense Motion Device data streaming.
    /// </summary>
    public func StopLogging() throws
    {
        try AssertIsConnected();

        state = DeviceState.CONNECTED;
        if (bleApi!.Connected)
        {
            let data: [UInt8] = [0x00];
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_logging, data: data);
        }
    }
    /// <summary>
    /// Starts TimeSync.
    /// </summary>
    public func StartSync(networkKey : UInt8, enableLEDs : Bool, isMaster : Bool = false) throws
    {
        try AssertIsConnected();

        if (bleApi!.Connected)
        {
            var data : [UInt8] = [ 0x7f & networkKey];
            if (isMaster)
            {
                data[0] |= 0x80;
            }
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_timesync, data: data);
            var data1 : [UInt8] = [ 0 ];
            if (enableLEDs)
            {
                data1[0] = 1;
            }
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_timesync_ui, data: data1);
        }
    }
    
    /// <summary>
    /// Stop TimeSync.
    /// </summary>
    public func StopSync() throws
    {
        try AssertIsConnected();

        state = DeviceState.OFFSET_COMPENSATION;
        if (bleApi!.Connected)
        {
            let data : [UInt8] = [ 0 ];
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_timesync, data: data);
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_timesync_ui, data: data);
        }
    }
    
    public func SetAnnotation(value : UInt8) throws
    {
        try AssertIsConnected();

        if (bleApi!.Connected)
        {
            let data : [UInt8] = [ value ];
            bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_annotation, data: data);
        }
    }
    
    private func AssertIsConnected() throws
    {
        if (state == DeviceState.DISCONNECTED)
        {
            throw NSError.init(domain: "Device", code: 0, userInfo: ["Description" : "The QSense Motion Device is not connected. Make sure that you call Device.Init() before calling this method."]);
        }
    }

    private func BleParse_DataEvent(e : DataReceivedEventArgs)
    {
        var buffer : [UInt8] = [UInt8]();
        
        for i : Int in 0..<e.Data.count
        {
            buffer.append(e.Data[i]);
        }
        
        do
        {
            try bleApi!.Ble2MRxEvent(data: buffer);
        }
        catch
        {
            
        }
    }

    //#region Private methods
    private func BleApi_Ble2MDataEvent(e : Ble2MDataEventArgs)
    {
        if (e.Address == UInt32.init(MemMap.MEM_MAP_CTRL_ADDR) && e.Data.count == MemMap.MEM_MAP_CTRL_SIZE)
        {
            do
            {
                try ctrl = MemMapCtrl(buffer: e.Data);
                if (ctrl != nil && bleApi!.Connected)
                {
                    let sn = SerialNumber;
                    SerialNumber = "\(String(format: "%llX", ctrl!.Address))-\(String(format: "%llX", ctrl!.Id).prefix(4))";
                    if (sn != SerialNumber)
                    {
                        let now : Int32 = Int32(Date.now.timeIntervalSince1970.rounded());
                        bleApi!.WriteMemory(address: MemMap.MEM_MAP_ADDR_time,
                                            data: withUnsafeBytes(of: now.littleEndian) { Array($0) });
                    }
                    else
                    {
                        let version : [UInt8] = withUnsafeBytes(of: ctrl!.Version.littleEndian) { Array($0) };
                        Version = "v\(version[2]).\(version[1]).\(version[0])";
                        let state : StateReceivedEventArgs = ExtractState(state: ctrl!.DeviceState);
                        if (self.state == .OFFSET_COMPENSATION && state.IsOffsetCompensated)
                        {
                            self.state = .CONNECTED;
                        }
                        let oldName : String = Name;
                        Name = ctrl!.DeviceName;
                        let oldBattery : Float = Battery;
                        Battery = Float.init(ctrl!.Battery);
                        let oldMotion : Float = MotionLevel;
                        MotionLevel = ctrl!.MotionLevel;
                        let oldMode = DataMode;
                        DataMode = DataModes.init(rawValue: Int.init(ctrl!.DataMode)) ?? .Mixed;
                        
                        AlgorithmSelection = Algorithms.init(rawValue: Int.init(ctrl!.AlgorithmSelection)) ?? ._9Dof;
                        let oldsync = SyncStatus;
                        SyncStatus = ctrl!.SyncStatus;
                        IsLogging = ctrl!.Logging == 0x01;
                        IsTimeSyncEnabled = (ctrl!.Timesync & 0x7F) != 0;
                        IsTimeSyncMaster = IsTimeSyncEnabled && ((ctrl!.Timesync & 0x80) != 0);
                        IsOffsetCompensated = ctrl!.OffsetCompensated;
                        IsMagFieldMapped = ctrl!.MagFieldMapped;
                        let progress : Int = Int.init(ctrl!.MagFieldMappingProgress);
                        MagFieldMappingProgress = progress * 10;
                        ConnectionInterval = Float.init(Int.init(ctrl!.ConnInterval)) * 1.25;
                        StateUpdate();
                        Marker = ctrl!.Annotation;
                        if (oldName != Name)
                        {
                            DeviceNameChanged.invoke(self, DeviceNameChangedEventArgs(name: Name));
                        }
                        if (oldBattery != Battery)
                        {
                            BatteryReceived.invoke(self, BatteryReceivedEventArgs(battery: Float.init(ctrl!.Battery)));
                        }
                        if (oldMotion != MotionLevel)
                        {
                            MotionLevelReceived.invoke(self, MotionLevelReceivedEventArgs(motionLevel: ctrl!.MotionLevel));
                        }
                        if (oldMode != DataMode)
                        {
                            DataModeReceived.invoke(self, DataMode);
                        }
                        if (oldsync != SyncStatus)
                        {
                            SyncStatusChanged.invoke(self, SyncStatus);
                        }
                    }
                }
            }
            catch { }
        }

        else if (e.Address == MemMap.MEM_MAP_CONF_ADDR && streamingData)
        {
            if (ctrl == nil)
            {
                return;
            }
            let data = MemMapData();
            data.AddData(buffer: e.Data);
            if (data.Packet == nil)
            {
                return;
            }
            let deviceAddress = ctrl?.Address;
            var acc = [[Float]]();
            var gyro = [[Float]]();
            var mag = [[Float]]();
            for i in 0..<(data.Packet?.Raw.count)!
            {
                let sample : Raw9Dof = (data.Packet?.Raw[i])!;
                acc.insert([sample.AccX, sample.AccY, sample.AccZ], at: i);
                gyro.insert([sample.GyrX, sample.GyrY, sample.GyrZ], at: i);
                if (data.Packet?.DataMode != DataModes.Optimized)
                {
                    mag.insert([sample.MagX, sample.MagY, sample.MagZ], at: i);
                }
            }
            let quat : [simd_quatf] = data.Packet!.Quaternion;
            let dataMode : DataModes = data.Packet!.DataMode;
            let freeAcc  : [Float] = data.Packet!.FreeAcceleration;
            let timestamp  : Date = Date.init(timeIntervalSince1970: Double.init(data.Packet!.Seconds) + Double.init(data.Packet!.Milliseconds) / 1000.0);
            let magInterf : MagInterference = MagInterference.init(rawValue: Int.init(data.Packet!.Interference)) ?? .None;
            StreamPacketReceived.invoke(self, StreamPacketReceivedEventArgs(dataMode: dataMode, deviceAddress: deviceAddress!,
                acc: acc, gyro: gyro, mag: mag, quaternion: quat, freeAcc: freeAcc,
                time : timestamp, battery : data.Packet!.Battery, annotation : data.Packet!.Annotation,
                syncOk : data.Packet!.SyncOk, interference : magInterf, buffering : data.Packet!.Buffering));
        }
    }

    private func BleApi_Ble2MTxEvent(e : Ble2MTxEventArgs)
    {
        var message : [UInt8] = [UInt8]();
        message.append(contentsOf: e.Packet);
        parser!.Write(data: message);
    }

    private func BleApi_Ble2MWriteCompletEvent(e : Ble2MDataEventArgs)
    {
        if (e.Address == MemMap.MEM_MAP_ADDR_pin && e.Data.count == 4)
        {
            bleApi!.ReadMemory(address: MemMap.MEM_MAP_CTRL_ADDR, length: UInt16.init(MemMap.MEM_MAP_CTRL_SIZE));
        }
        else if (e.Address == MemMap.MEM_MAP_ADDR_time && e.Data.count == 4)
        {
            InitializationDone.invoke(self, self);
            do
            {
                try ReadMemory();
            }
            catch { }
        }
    }

    private func GetMagFieldMappingState(e : StateReceivedEventArgs)
    {
        MagFieldMappingProgress = Int.init(e.MagFieldMappingProgress);
        if (MagFieldMappingProgress == 100)
        {
            state = DeviceState.CONNECTED;
            MagFieldMappingDone.invoke(self, 1);
        }
        if (MagFieldMappingProgress > 100)
        {
            state = DeviceState.CONNECTED;
            MagFieldMappingFailed.invoke(self, 0);
        }
    }

    private func ExtractState(state : UInt16) -> StateReceivedEventArgs
    {
        let bytes : [UInt8] = withUnsafeBytes(of: state.littleEndian) { Array($0) };
        IsOffsetCompensationOn = (bytes[0] & 0x01) == 0x01;
        IsMagFieldMappingOn = (bytes[0] & 0x02) == 0x02;
        AccSensitivity = SensitivityAcc.init(rawValue: Int.init((bytes[0] & 0x0C) >> 2)) ?? .G2;
        GyrSensitivity = SensitivityGyr.init(rawValue: Int.init((bytes[0] & 0x70) >> 4)) ?? .Dps250;
        IsAutoCalibrationOn = (bytes[0] & 0x80) == 0x80;
        SamplingRate = SamplingRates.init(rawValue: Int.init(bytes[1] & 0x0F)) ?? .Hz100;
        DataBuffering = Int.init((bytes[1] & 0xF0) >> 4);

        let stateArgs : StateReceivedEventArgs = StateReceivedEventArgs(isOffsetCompensationOn: IsOffsetCompensationOn, isOffsetCompensated: IsOffsetCompensated, isMagFieldMappingOn: IsMagFieldMappingOn, isMagFieldMapped: IsMagFieldMapped, magFieldMappingProgress: Int32(MagFieldMappingProgress), accSensitivity: AccSensitivity, gyroSensitivity: GyrSensitivity, isAutoCalOn: IsAutoCalibrationOn, samplingRate: SamplingRate, dataBuffering: Int32(DataBuffering), connectionInterval: ConnectionInterval);
        return stateArgs;
    }

    private func StateUpdate()
    {
        if (ctrl == nil)
        {
            return;
        }

	let oldState : UInt16 = stateDevice;
        stateDevice = UInt16(ctrl?.DeviceState ?? 0);
        
        if (oldState == stateDevice)
        {
            return;
        }

        let state : StateReceivedEventArgs = ExtractState(state: UInt16(ctrl?.DeviceState ?? 0));

        switch (state.AccSensitivity)
        {
            case SensitivityAcc.G2:
                accSensitivity = 0.000061;
                break;
            case SensitivityAcc.G16:
                accSensitivity = 0.000488;
                break;
            case SensitivityAcc.G4:
                accSensitivity = 0.000122;
                break;
            case SensitivityAcc.G8:
                accSensitivity = 0.000244;
                break;
        }
        switch (state.GyroSensitivity)
        {
            case SensitivityGyr.Dps250:
                gyrSensitivity = 0.008750;
                break;
            case SensitivityGyr.Dps125:
                gyrSensitivity = 0.004375;
                break;
            case SensitivityGyr.Dps500:
                gyrSensitivity = 0.0175;
                break;
            case SensitivityGyr.Dps1000:
                gyrSensitivity = 0.035;
                break;
            case SensitivityGyr.Dps2000:
                gyrSensitivity = 0.07;
                break;
        }
        StateReceived.invoke(self, state);
    }
    //#endregion
}
