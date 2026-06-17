//
//  BleApi.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 19/07/2024.
//

import Foundation
import swift_event

internal class BleApi
{
    internal enum Status : Int
    {
        case Idle = 0
        case Reading
        case Writing
        case Streaming
    }
    
    private var state : Status;
    private var readBuffer : [UInt8] = [UInt8]();
    private var readAddress : UInt32 = 0;
    private var buffer : [UInt8] = [UInt8]();
    private var timeoutTimer : Timer?;

    internal var Connected : Bool = false;
    internal var MaxPacketSize : Int = Int.init(MemMap.MEM_MAP_CTRL_SIZE) + 7;
    internal var BleQueue : Queue<BleQueueData> = Queue<BleQueueData>();

    public let Ble2MTxEvent = Event<Ble2MTxEventArgs>.create();
    internal let Ble2MDataEvent = Event<Ble2MDataEventArgs>.create();
    internal let Ble2MWriteCompletEvent = Event<Ble2MDataEventArgs>.create();

    internal init()
    {
        state = .Idle;
        Connected = true;
    }
    
    @objc func fireTimer() {
        state = .Idle;
        BleDequeue();
    }
    
    internal func BleDequeue()
    {
        if (state == .Idle && BleQueue.Count > 0)
        {
            let data : BleQueueData = BleQueue.dequeue()!;
            switch (data.PacketType)
            {
            case .Read:
                ReadMemory(address: data.Address, length: data.Length);
                break;
            case .Data:
                WriteMemory(address: data.Address, data: data.Data);
                break;
            case .Stream:
                StreamMemory(address: data.Address, length: data.Length);
                break;
            default:
                break;
            }
        }
    }

    internal func ReadMemory(address : UInt32, length : UInt16)
    {
        if (state != .Idle)
        {
            BleQueue.enqueue(newElement: BleQueueData(packetType : .Read, address : address, data : [UInt8](), length : length))
        }
        else
        {
            state = .Reading;
            let args = Ble2MTxEventArgs(
                packet: (Packet(type: .Read, address: address, length: length, data: [ 0x00 ])).ToArray()
            );
            readAddress = address;
            readBuffer = [UInt8](repeating: 0, count: Int.init(length));
            Ble2MTxEvent.invoke(self, args);
            self.timeoutTimer?.invalidate();
            self.timeoutTimer = nil;
            timeoutTimer = Timer.scheduledTimer(timeInterval: 5.0, target: self, selector: #selector(fireTimer), userInfo: nil, repeats: false);
        }
    }

    internal func StreamMemory(address : UInt32, length : UInt16)
    {
        if (state != .Idle)
        {
            BleQueue.enqueue(newElement: BleQueueData(packetType : .Stream, address : address, data : [UInt8](), length : length))
        }
        else
        {
            state = .Streaming;
            let args = Ble2MTxEventArgs(
                packet: (Packet(type: .Stream, address: address, length: length, data: [ 0x00 ])).ToArray()
            );
            readAddress = address;
            readBuffer = [UInt8](repeating: 0, count: Int.init(length));
            Ble2MTxEvent.invoke(self, args);
            self.timeoutTimer?.invalidate();
            self.timeoutTimer = nil;
            timeoutTimer = Timer.scheduledTimer(timeInterval: 5.0, target: self, selector: #selector(fireTimer), userInfo: nil, repeats: false);
        }
    }

    internal func Abort()
    {
        state = .Idle;
        let args = Ble2MTxEventArgs(
            packet: (Packet(type: .Abort, address: 0, length: 0, data: [ 0x00 ])).ToArray()
        );
        readAddress = 0;
        readBuffer = [UInt8]();
        Ble2MTxEvent.invoke(self, args);
    }

    internal func WriteMemory(address : UInt32, data : [UInt8])
    {
        if (state != .Idle)
        {
            BleQueue.enqueue(newElement: BleQueueData(packetType : .Data, address : address, data : data, length : UInt16.init(data.count)))
        }
        else
        {
            state = .Writing;
            readAddress = address;
            readBuffer = [UInt8](repeating: 0, count: 0);
            readBuffer.insert(contentsOf: data, at: 0)
            
            let length : Int = min(MaxPacketSize - 7, readBuffer.count);
            var packetData = [UInt8](repeating: 0, count: 0);
            packetData.append(contentsOf: readBuffer[0..<length])
            
            let args = Ble2MTxEventArgs(
                packet: (Packet(type: .Data, address: readAddress, length: UInt16.init(packetData.count), data: packetData)).ToArray()
            );
            Ble2MTxEvent.invoke(self, args);
            self.timeoutTimer?.invalidate();
            self.timeoutTimer = nil;
            timeoutTimer = Timer.scheduledTimer(timeInterval: 5.0, target: self, selector: #selector(fireTimer), userInfo: nil, repeats: false);
        }
    }

    internal func Ble2MRxEvent(data : [UInt8]) throws
    {
        let rxPacket : Packet = Packet.init(array: data);
        if (rxPacket.Address < readAddress ||
            rxPacket.Address + UInt32.init(rxPacket.Length) > readAddress + UInt32.init(readBuffer.count))
        {
            return;//throw NSError.init(domain: "StreamPacket", code: 0, userInfo: ["Description" : "Corrupt packet received"]);
        }
        
        self.timeoutTimer?.invalidate();
        self.timeoutTimer = nil;
        
        switch (state)
        {
            case .Reading:
                let startIndex : Int = Int.init(rxPacket.Address - readAddress);
                readBuffer[startIndex..<startIndex+Int.init(rxPacket.Length)] = ArraySlice(rxPacket.Data[0..<Int.init(rxPacket.Length)]);

                if (Int.init(rxPacket.Address) + Int.init(rxPacket.Length) == Int.init(readAddress) + readBuffer.count)
                {
                    state = .Idle;
                    let args = Ble2MDataEventArgs(address: readAddress, data: readBuffer);
                    
                    Ble2MDataEvent.invoke(self, args);
                }
                else if (Int.init(readAddress) + buffer.count + Int.init(rxPacket.Length) == Int.init(readAddress) +    readBuffer.count)
                {
                    buffer.insert(contentsOf: readBuffer, at: buffer.count);
                    state = .Idle;
                    let args = Ble2MDataEventArgs(address: readAddress, data: buffer);
                    buffer.removeAll();
                    Ble2MDataEvent.invoke(self, args);
                }
                else
                {
                    buffer.insert(contentsOf: readBuffer, at: buffer.count);
                }
            break;

            case .Writing:
                if (Int.init(rxPacket.Address) + Int.init(rxPacket.Length) == Int.init(readAddress) + readBuffer.count)
                {
                    state = .Idle;
                    let args = Ble2MDataEventArgs(address: readAddress, data: readBuffer);
                    Ble2MWriteCompletEvent.invoke(self, args);
                }
                else if(Int.init(rxPacket.Address) + Int.init(rxPacket.Length) < Int.init(readAddress) + readBuffer.count)
                {
                    let index : Int = Int.init(rxPacket.Address) + Int.init(rxPacket.Length) - Int.init(readAddress);
                    let length : Int = min(MaxPacketSize - 7, Int.init(readBuffer.count) - index);
                    var packetData = [UInt8](repeating: 0, count: Int.init(length));
                    packetData.insert(contentsOf: readBuffer[index..<index+length], at: 0);

                    let args = Ble2MTxEventArgs(
                        packet: (Packet(type: .Data, address: readAddress + UInt32(index), length: UInt16.init(packetData.count), data: packetData)).ToArray()
                    );
                    Ble2MTxEvent.invoke(self, args);
                }
                break;

            case .Streaming:
                if (rxPacket.Address >= readAddress)
                {
                    readBuffer[Int.init(rxPacket.Address - readAddress)..<Int.init(rxPacket.Address - readAddress)+Int.init(rxPacket.Length)] = ArraySlice(rxPacket.Data);
                    
                    if (Int.init(rxPacket.Address) + Int.init(rxPacket.Length) == Int.init(readAddress) + readBuffer.count)
                    {
                        let args = Ble2MDataEventArgs(address: readAddress, data: readBuffer);
                        Ble2MDataEvent.invoke(self, args);
                    }
                }
                else
                {
                    print("Corrupt BLE Packet");
                }
                break;
        default:
            break;

        }
        BleDequeue();
        
        if (state != .Idle)
        {
            self.timeoutTimer?.invalidate();
            self.timeoutTimer = nil;
            timeoutTimer = Timer.scheduledTimer(timeInterval: 5.0, target: self, selector: #selector(fireTimer), userInfo: nil, repeats: false);
        }
    }
}

internal class Ble2MTxEventArgs
{
    internal var Packet : [UInt8] = [UInt8]();
    
    init(packet : [UInt8])
    {
        Packet = packet;
    }
}

internal class Ble2MDataEventArgs
{
    internal var Address : UInt32;
    internal var Data : [UInt8] = [UInt8]();
    
    init(address : UInt32, data : [UInt8])
    {
        Address = address;
        Data = data
    }
}

internal class BleQueueData
{
    public var PacketType : Packet.Opcode;
    public var Address : UInt32;
    public var Data : [UInt8] = [UInt8]();
    public var Length : UInt16;
    
    public init(packetType : Packet.Opcode, address : uint, data : [UInt8], length : UInt16)
    {
        PacketType = packetType;
        Address = address;
        Data = data;
        Length = length;
    }
}
