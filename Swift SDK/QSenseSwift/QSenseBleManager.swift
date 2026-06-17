//
//  QSenseBleManager.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 23/07/2024.
//

import Foundation
import CoreBluetooth
import swift_event

public class QSenseBleManager : NSObject, CBCentralManagerDelegate, CBPeripheralDelegate
{
    private let SERVICE_UUID : CBUUID = CBUUID(string: "6e400001-b5a3-f393-e0a9-e50e24dcca9e");
    private let _centralManager : CBCentralManager;
    private var _connectedDevices : [QSenseDevice];
    private var _discoveredDevices : [String:CBPeripheral];
    private var _discoveredParsers : [BleParser];
    
    public var isScanning : Bool = false;
    
    public let onManagerStateUpdate = Event<ManagerState>.create();
    public let onScanCompleted = Event<Any?>.create();
    public let onDiscoverDevice = Event<String>.create();
    public let onDeviceConnected = Event<QSenseDevice>.create();
    public let onConnectionFailed = Event<String>.create();
    public let onDeviceDisconnected = Event<String>.create();
    
    public override init()
    {
        _centralManager = CBCentralManager();
        _connectedDevices = [QSenseDevice]();
        _discoveredDevices = [String:CBPeripheral]();
        _discoveredParsers = [BleParser]();
        super.init();
        _centralManager.delegate = self;
    }
    
    public func scan()
    {
        if _centralManager.state == .poweredOn
        {
            _centralManager.scanForPeripherals(withServices: [SERVICE_UUID], options: [CBCentralManagerScanOptionAllowDuplicatesKey : false])
            
            isScanning = true;
        }
        
    }
    
    public func stopScan()
    {
        _centralManager.stopScan();
        isScanning = false;
    }
    
    public func connect(to serialNumber : String)
    {
        if (_discoveredDevices[serialNumber] != nil)
        {
            _centralManager.connect(_discoveredDevices[serialNumber]!);
        }
    }
    
    public func disconnect(from device : QSenseDevice)
    {
        _centralManager.cancelPeripheralConnection(device.peripheral);
    }
    
    public func managerStateIsPoweredOn() -> Bool
    {
        return _centralManager.state == .poweredOn;
    }
    
    public func managerState() -> ManagerState
    {
        return _centralManager.state;
    }
    
    // CBCentralManagerDelegate
    
    /**
     *  @method centralManager:didDiscoverPeripheral:advertisementData:RSSI:
     *
     *  @param central              The central manager providing this update.
     *  @param peripheral           A <code>CBPeripheral</code> object.
     *  @param advertisementData    A dictionary containing any advertisement and scan response data.
     *  @param RSSI                 The current RSSI of <i>peripheral</i>, in dBm. A value of <code>127</code> is reserved and indicates the RSSI
     *                                was not available.
     *
     *  @discussion                 This method is invoked while scanning, upon the discovery of <i>peripheral</i> by <i>central</i>. A discovered peripheral must
     *                              be retained in order to use it; otherwise, it is assumed to not be of interest and will be cleaned up by the central manager. For
     *                              a list of <i>advertisementData</i> keys, see {@link CBAdvertisementDataLocalNameKey} and other similar constants.
     *
     *  @seealso                    CBAdvertisementData.h
     *
     */
    public func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral, advertisementData: [String : Any], rssi RSSI: NSNumber)
    {
        if peripheral.name == "QSense"
        {
            var data : [UInt8] = [UInt8]();
            if (advertisementData[CBAdvertisementDataManufacturerDataKey] != nil)
            {
                let packet : Data = advertisementData[CBAdvertisementDataManufacturerDataKey] as! Data;
                for i : Int in 0..<packet.count
                {
                    data.append(packet[i]);
                }
                var serialNumber : String = parseManufacturerData(from: data).uppercased();
                _discoveredDevices[serialNumber] = peripheral;
                onDiscoverDevice.invoke(self, serialNumber);
            }
        }
    }
    
    private func parseManufacturerData(from buffer : [UInt8]) -> String
    {
        return String(bytes: buffer[2..<buffer.count], encoding: .utf8) ?? "";
    }

    
    /**
     *  @method centralManager:didConnectPeripheral:
     *
     *  @param central      The central manager providing this information.
     *  @param peripheral   The <code>CBPeripheral</code> that has connected.
     *
     *  @discussion         This method is invoked when a connection initiated by {@link connectPeripheral:options:} has succeeded.
     *
     */
    public func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral)
    {
        var i : (key: String, value: CBPeripheral)? = nil;
        for d in  _discoveredDevices
        {
            i = d;
            if(d.value == peripheral)
            {
                break;
            }
        }
        if (i != nil && i!.value == peripheral)
        {
            _discoveredDevices.remove(at: _discoveredDevices.index(forKey: i!.key)!);
        }
        var parser : BleParser = BleParser(peripheral: peripheral);
        _discoveredParsers.append(parser);
        parser.isReady.event += EventHandler<BleParser>(handle: { sender, args in
            var device : QSenseDevice = QSenseDevice(parser: args);
            
            device.InitializationDone.event += EventHandler<QSenseDevice>(handle: { sender, args in
                self._connectedDevices.append(args);
                self.onDeviceConnected.invoke(self, args);
            });
        });
    }

    
    /**
     *  @method centralManager:didFailToConnectPeripheral:error:
     *
     *  @param central      The central manager providing this information.
     *  @param peripheral   The <code>CBPeripheral</code> that has failed to connect.
     *  @param error        The cause of the failure.
     *
     *  @discussion         This method is invoked when a connection initiated by {@link connectPeripheral:options:} has failed to complete. As connection attempts do not
     *                      timeout, the failure of a connection is atypical and usually indicative of a transient issue.
     *
     */
    public func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: (any Error)?)
    {
        if (error != nil)
        {
            var i : (key: String, value: CBPeripheral)? = nil;
            for d in  _discoveredDevices
            {
                if(d.value == peripheral)
                {
                    break;
                }
                i = d;
            }
            if (i != nil && i!.value == peripheral)
            {
                _discoveredDevices.remove(at: _discoveredDevices.index(forKey: i!.key)!);
            }
            onConnectionFailed.invoke(self, error!.localizedDescription);
        }
    }

    
    /**
     *  @method centralManager:didDisconnectPeripheral:error:
     *
     *  @param central      The central manager providing this information.
     *  @param peripheral   The <code>CBPeripheral</code> that has disconnected.
     *  @param error        If an error occurred, the cause of the failure.
     *
     *  @discussion         This method is invoked upon the disconnection of a peripheral that was connected by {@link connectPeripheral:options:}. If the disconnection
     *                      was not initiated by {@link cancelPeripheralConnection}, the cause will be detailed in the <i>error</i> parameter. Once this method has been
     *                      called, no more methods will be invoked on <i>peripheral</i>'s <code>CBPeripheralDelegate</code>.
     *
     */
    public func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: (any Error)?)
    {
        deviceDisconnected(peripheral);
    }

    
    /**
     *  @method centralManager:didDisconnectPeripheral:timestamp:isReconnecting:error
     *
     *  @param central      The central manager providing this information.
     *  @param peripheral   The <code>CBPeripheral</code> that has disconnected.
     *  @param timestamp        Timestamp of the disconnection, it can be now or a few seconds ago.
     *  @param isReconnecting      If reconnect was triggered upon disconnection.
     *  @param error        If an error occurred, the cause of the failure.
     *
     *  @discussion         This method is invoked upon the disconnection of a peripheral that was connected by {@link connectPeripheral:options:}. If perihperal is
     *                      connected with connect option {@link CBConnectPeripheralOptionEnableAutoReconnect}, once this method has been called, the system
     *                      will automatically invoke connect to the peripheral. And if connection is established with the peripheral afterwards,
     *                      {@link centralManager:didConnectPeripheral:} can be invoked. If perihperal is connected without option
     *                      CBConnectPeripheralOptionEnableAutoReconnect, once this method has been called, no more methods will be invoked on
     *                       <i>peripheral</i>'s <code>CBPeripheralDelegate</code> .
     *
     */
    public func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, timestamp: CFAbsoluteTime, isReconnecting: Bool, error: (any Error)?)
    {
        deviceDisconnected(peripheral);
    }

    
    /**
     *  @method centralManager:connectionEventDidOccur:forPeripheral:
     *
     *  @param central      The central manager providing this information.
     *  @param event        The <code>CBConnectionEvent</code> that has occurred.
     *  @param peripheral   The <code>CBPeripheral</code> that caused the event.
     *
     *  @discussion         This method is invoked upon the connection or disconnection of a peripheral that matches any of the options provided in {@link registerForConnectionEventsWithOptions:}.
     *
     */
    public func centralManager(_ central: CBCentralManager, connectionEventDidOccur event: CBConnectionEvent, for peripheral: CBPeripheral)
    {
        if event == CBConnectionEvent.peerConnected
        {
            var i : (key: String, value: CBPeripheral)? = nil;
            for d in  _discoveredDevices
            {
                i = d;
                if(d.value == peripheral)
                {
                    break;
                }
            }
            if (i != nil && i!.value == peripheral)
            {
                _discoveredDevices.remove(at: _discoveredDevices.index(forKey: i!.key)!);
            }
            var parser : BleParser = BleParser(peripheral: peripheral);
            _discoveredParsers.append(parser);
            parser.isReady.event += EventHandler<BleParser>(handle: { sender, args in
                var device : QSenseDevice = QSenseDevice(parser: args);
                
                device.InitializationDone.event += EventHandler<QSenseDevice>(handle: { sender, args in
                    self._connectedDevices.append(args);
                    self.onDeviceConnected.invoke(self, args);
                });
            });
        }
        else if event == CBConnectionEvent.peerDisconnected
        {
            deviceDisconnected(peripheral);
        }
    }
    
    /**
     *  @method centralManagerDidUpdateState:
     *
     *  @param central  The central manager whose state has changed.
     *
     *  @discussion     Invoked whenever the central manager's state has been updated. Commands should only be issued when the state is
     *                  <code>CBCentralManagerStatePoweredOn</code>. A state below <code>CBCentralManagerStatePoweredOn</code>
     *                  implies that scanning has stopped and any connected peripherals have been disconnected. If the state moves below
     *                  <code>CBCentralManagerStatePoweredOff</code>, all <code>CBPeripheral</code> objects obtained from this central
     *                  manager become invalid and must be retrieved or discovered again.
     *
     *  @see            state
     *
     */
    public func centralManagerDidUpdateState(_ central: CBCentralManager)
    {
        if isScanning && !_centralManager.isScanning
        {
            isScanning = false;
            onScanCompleted.invoke(self,nil);
        }
        onManagerStateUpdate.invoke(self, _centralManager.state);
    }
    
    private func deviceDisconnected(_ peripheral : CBPeripheral)
    {
        let device : QSenseDevice;
        for i : Int in 0..<_connectedDevices.count
        {
            if _connectedDevices[i].peripheral == peripheral
            {
                device = _connectedDevices[i];
                _connectedDevices.remove(at: i);
                onDeviceDisconnected.invoke(self, device.SerialNumber);
                return;
            }
        }
    }
}
