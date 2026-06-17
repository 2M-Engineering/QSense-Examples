//
//  ICommunication.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 19/07/2024.
//

import Foundation
import swift_event
import CoreBluetooth

internal class BleParser : NSObject, CBPeripheralDelegate
{
    private let SERVICE_UUID : CBUUID = CBUUID(string: "6e400001-b5a3-f393-e0a9-e50e24dcca9e");
    private let RX_CHAR_UUID : CBUUID = CBUUID(string: "6e400002-b5a3-f393-e0a9-e50e24dcca9e");
    private let TX_CHAR_UUID : CBUUID = CBUUID(string: "6e400003-b5a3-f393-e0a9-e50e24dcca9e");
    private var _device : CBPeripheral;
    private var _rxCharacteristic : CoreBluetooth.CBCharacteristic?;
    private var _txCharacteristic : CoreBluetooth.CBCharacteristic?;
    
    internal var DataReceived = Event<DataReceivedEventArgs>.create();
    internal var isReady = Event<BleParser>.create();
    
    internal var peripheral : CBPeripheral { get { return _device; } }
    
    internal init(peripheral : CBPeripheral)
    {
        _device = peripheral;
        super.init();
        _device.delegate = self;
        _device.discoverServices([SERVICE_UUID]);
    }
    
    private func discoverCharacteristics(_ service: CBService) 
    {
        _device.discoverCharacteristics([RX_CHAR_UUID, TX_CHAR_UUID], for: service)
    }
    
    private func enableNotifications(for characteristic: CBCharacteristic)
    {
        if characteristic.properties.contains(.notify) 
        {
            _device.setNotifyValue(true, for: characteristic)
        }
    }
    
    public func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?)
    {
        if characteristic == _txCharacteristic
        {
            if let value = characteristic.value
            {
                DataReceived.invoke(self, DataReceivedEventArgs(data: value));
            }
        }
    }
    
    public func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?)
    {
        if let services = peripheral.services
        {
            for service in services 
            {
                if service.uuid == SERVICE_UUID
                {
                    discoverCharacteristics(service);
                    return;
                }
            }
        }
    }
    
    public func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?)
    {
        if let characteristics = service.characteristics
        {
            for characteristic in characteristics 
            {
                if characteristic.uuid == RX_CHAR_UUID
                {
                    _rxCharacteristic = characteristic;
                } 
                else if characteristic.uuid == TX_CHAR_UUID
                {
                    _txCharacteristic = characteristic;
                }
            }
        }
        
        // If Button caracteristic was found, try to enable notifications on it.
        if let _txCharacteristic = _txCharacteristic
        {
            enableNotifications(for: _txCharacteristic)
        }
    }
    
    public func peripheral(_ peripheral: CBPeripheral, didUpdateNotificationStateFor characteristic: CBCharacteristic, error: Error?) {
        if characteristic == _txCharacteristic
        {
            isReady.invoke(self, self);
        }
    }
    
    func Write(data : [UInt8])
    {
        if _rxCharacteristic!.properties.contains(.write)
        {
            _device.writeValue(Data(data), for: _rxCharacteristic!, type: .withResponse)
        }
        else if _rxCharacteristic!.properties.contains(.writeWithoutResponse)
        {
            _device.writeValue(Data(data), for: _rxCharacteristic!, type: .withoutResponse)
        }
    }
}

internal class DataReceivedEventArgs
{
    public init(data : Data)
    {
        Data = data;
    }
    public var Data: Data;
}
