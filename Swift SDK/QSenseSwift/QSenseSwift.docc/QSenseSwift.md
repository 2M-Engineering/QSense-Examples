#### Framework
# __QSenseSwift__
#### Communicate with QSense Motion BLE devices.
###### iOS 5.0+
___

## __Overview__
The QSenseSwift framework empowers iOS developers by providing a straightforward approach to constructing applications that utilize the QSense device, eliminating the need for in-depth knowledge of the QSense API.
Whether you are building mobile apps, IoT solutions, or any other software that requires seamless connectivity and data exchange, this framework will provide you with the necessary tools and resources to access all the functionalities of the QSense device. It also implements all the neccessary features from the [CoreBluetooth](https://developer.apple.com/documentation/corebluetooth) framework to discovered, connect and communicate with the QSense devices.
___
## __Installation__
###### __Xcode__
1. Copy the QSenseSwift.framework folder into your projects folder. 
2. Select your build target, then select “general” tab, scroll down to “Framework, libraries, and Embedded Content”, then select “+”, "Add Other...", "Add Files..." and finally select the QSenseSwift.framework folder.
3. Click on the "Embed" column and select "Embed & Sign".
___
## __Examples__
##### Scan and connect to QSense device
This example shows how to create a QSenseBluetoothManager, how to start to scan automatically when it is ready, and how to automatically connect to the discovered devices.
```swift
let central : QSenseBleManager = QSenseBleManager();
central.onManagerStateUpdate.event += ManagerStateUpdateEventHandler(handle: { sender, args in
    if args == .poweredOn
    {
        self.central.scan();
    } 
});
central.onDiscoverDevice.event += DiscoverDeviceEventHandler(handle: { sender, args in
    print("[onDiscoverDevice] SerialNumber: \(args)");
    self.central.connect(to: args);
});
central.onDeviceConnected.event += DeviceConnectedEventHandler(handle: { sender, args in
    print("[onDeviceConnected] Connected to \(args.SerialNumber)");
    var device : QSenseDevice = args;
});
```
##### Start data streaming
This example shows how you can start the data streaming and how you can execute your own code when the data is received.
```Swift
device.StreamPacketReceived.event += StreamPacketReceivedEventHandler(handle: { sender, args in
    print("[StreamPacketReceived] packet received: \(args.PacketID)");
    // Add your code here
});
device.StartStreaming();
```
##### Magnetic field mapping
This last example shows how you can start the magnetic field mapping, show the progress and when it is completed.
```Swift
device.StateReceived.event += StateReceivedEventHandler(handle: { sender, args in
    if (args.IsMagFieldMappingOn)
    {
        print("[MagFieldMapping] progress: \(args.MagFieldMappingProgress)");
        DispatchQueue.main.asyncAfter(deadline: .now() + 1) {
            self.device.ReadMemory();
        }
    }
});
device.MagFieldMappingDone.event += StateReceivedEventHandler(handle: { sender, args in
    print("[MagFieldMapping] Completed with quality: \(args)");
});
device.StartMagFieldMapping();
DispatchQueue.main.asyncAfter(deadline: .now() + 1) {
    self.device.ReadMemory();
}
```
___
## __Documentation__
This section includes a deeper description of the classes, enums and delegates implemented in this framework.
## Classes
#### QSenseBleManager
An object that scans for, discovers, connects to, and manages peripherals.
```Swift
public class QSenseBleManager : NSObject, CBCentralManagerDelegate, CBPeripheralDelegate
{
    // Fields
    public var isScanning : Bool;
    // Events
    public let onManagerStateUpdate;
    public let onScanCompleted;
    public let onDiscoverDevice;
    public let onDeviceConnected;
    public let onConnectionFailed;
    public let onDeviceDisconnected;
    // Methods
    public override init();
    public func scan();
    public func stopScan();
    public func connect(to serialNumber : String);
    public func disconnect(from device : QSenseDevice);
    public func managerStateIsPoweredOn() -> Bool;
    public func managerState() -> ManagerState;
}
```
#### QSenseDevice

```Swift
public class QSenseDevice
{
    // Fields
    public var AccSensitivity : SensitivityAcc;
    public var SerialNumber : String;
    public var Battery : Float;
    public var ConnectionInterval : Float;
    public var DataBuffering : Int;
    public var GyrSensitivity : SensitivityGyr;
    public var IsConnected : Bool;
    public var IsLogging : Bool;
    public var IsMagFieldMapped : Bool;
    public var IsMagFieldMappingOn : Bool;
    public var IsOffsetCompensated : Bool;
    public var IsOffsetCompensationOn : Bool;
    public var IsAutoCalibrationOn : Bool;
    public var MagFieldMappingProgress : Int;
    public var MagFieldMapQuality : MagFieldMapQualities;
    public var MaxPacketSize : Int;
    public var MotionLevel : Float;
    public var Name : String;
    public var SamplingRate : SamplingRates;
    public var Version : String;
    public let BatteryReceived;
    public let DeviceNameChanged;
    public let MagFieldMappingDone;
    public let MotionLevelReceived;
    public let StateReceived;
    public let StreamPacketReceived;
    // Methods
    public func ReadMemory();
    public func Reset();
    public func SetAccSensitivity(value : SensitivityAcc);
    public func SetDataBuffering(value : Int);
    public func SetDataMode(value : DataModes);
    public func SetDeviceName(name : String);
    public func SetGyrSensitivity(value : SensitivityGyr);
    public func SetLEDAnimation(red : UInt8, green : UInt8, blue : UInt8, animation : LEDAnimation);
    public func SetSamplingRate(value : SamplingRates);
    public func SetSensorConfig(accSens : SensitivityAcc, gyrSens : SensitivityGyr, sampRate : SamplingRates, buffering : Int);
    public func HibernateMode();
    public func StartMagFieldMapping();
    public func StartOffsetCompensation();
    public func StartStreaming();
    public func StopStreaming();
    public func StartLogging();
    public func StopLogging();
}
```
#### BatteryReceivedEventArgs
Provides data for the BatteryReceivedEvent from the QSenseDevice class.
```Swift
public class BatteryReceivedEventArgs
{
    public var Battery : Float;
}
```
#### DeviceNameChangedEventArgs
Provides data for the DeviceNameChangedEvent from the QSenseDevice class.
```Swift
public class DeviceNameChangedEventArgs
{
    public var Name : String;
}
```
#### BatteryReceivedEventArgs
Provides data for the BatteryReceivedEvent from the QSenseDevice class.
```Swift
public class BatteryReceivedEventArgs
{
    public var Battery : Float;
}
```
#### MotionLevelReceivedEventArgs
Provides data for the EnergyReceivedEvent from the QSenseDevice class.
```Swift
public class MotionLevelReceivedEventArgs
{
    public var MotionLevel : Float;
}
```
#### StateReceivedEventArgs
Provides data for the StateReceivedEvent from the QSenseDevice class.
```Swift
public class StateReceivedEventArgs
{
    public var IsOffsetCompensationOn : Bool;
    public var IsOffsetCompensated : Bool;
    public var IsMagFieldMappingOn : Bool;
    public var IsMagFieldMapped : Bool;
    public var MagFieldMappingProgress : Int32;
    public var MagFieldMapQuality : MagFieldMapQualities;
    public var AccSensitivity : SensitivityAcc;
    public var GyroSensitivity : SensitivityGyr;
    public var IsAutoCalibrationOn : Bool;
    public var SamplingRate : SamplingRates;
    public var DataBuffering : Int32;
    public var ConnectionInterval : Float;
}
```
#### StreamPacketReceivedEventArgs
Provides data for the StreamPacketReceived from the QSenseDevice class.
```Swift
public class StreamPacketReceivedEventArgs
{
    public var DataMode : DataModes;
    public var DeviceAddress : UInt64;
    public var Acc : [[Float]];
    public var Gyro : [[Float]];
    public var Mag : [[Float]];
    public var Quaternion : [simd_quatf];
    public var FreeAcc : [Float];
    public var PacketID : UInt16;
    public var Interference : MagInterference;
}
```
## Delegates
#### BatteryReceivedEventHandler
Delegate for the BatteryReceive event from the QSenseDevice class.
#### DeviceNameChangedEventHandler
Delegate for the MotionLevelReceived event from the QSenseDevice class.
#### MagFieldMappingDoneEventHandler
Delegate for the MagFieldMappingDone event from the QSenseDevice class.
#### MotionLevelReceivedEventHandler
Delegate for the MotionLevelReceived event from the QSenseDevice class.
#### StateReceivedEventHandler
Delegate for the StateReceived event from the QSenseDevice class.
#### StreamPacketReceivedEventHandler
Delegate for the StreamPacketReceived event from the QSenseDevice class.
#### ManagerStateUpdateEventHandler
Delegate for the onManagerStateUpdate event from the QSenseBleManager class.
#### ScanCompletedEventHandler
Delegate for the StateReceived event from the QSenseBleManager class.
#### DiscoverDeviceEventHandler
Delegate for the onDiscoverDevice event from the QSenseBleManager class.
#### DeviceConnectedEventHandler
Delegate for the onDeviceConnected event from the QSenseBleManager class.
#### ConnectionFailedEventHandler
Delegate for the onConnectionFailed event from the QSenseBleManager class.
#### DeviceDisconnectedEventHandler
Delegate for the onDeviceDisconnected event from the QSenseBleManager class.
## Enum types
#### DataModes
Supported data modes of the QSense Motion device.
|values|
|-|
|Mixed|
|Raw|
|Quat|
|Optimized|
|Quat6Dof|
#### LEDAnimation
LED animation patterns available.
|values|
|-|
|Blinking|
|Fixed|
#### MagFieldMapQualities
Quality output of the magnetic field mapping.
|values|
|-|
|Bad|
|Good|
#### MagInterference
Different types of magnetic interferences that the QSense device reports during the data streaming.
|values|
|-|
|None|
|SoftIron|
|HardIron|
|ChangeOfEnvironment|
#### SamplingRates
Supported sampling rates of the QSense device.
|values|
|-|
|Hz50|
|Hz100|
|Hz200|
|Hz400|
|Hz800|
#### SensitivityAcc
Available options for the configuration of the accelerometer's sensitivity, expressed in units of standard gravity.
|values|
|-|
|G2|
|G4|
|G8|
|Optimized|
#### SensitivityGyr
Available options for the configuration of the gyroscope's sensitivity, expressed in degrees per second.
|values|
|-|
|Dps250|
|Dps125|
|Dps500|
|Dps1000|
|Dps2000|
#### ManagerState
Typealias for the [CBManagerState](https://developer.apple.com/documentation/corebluetooth/cbmanagerstate) enum from the CoreBluetooth framework.
