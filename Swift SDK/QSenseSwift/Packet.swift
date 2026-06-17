//
//  Packet.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 18/07/2024.
//

import Foundation

internal class Packet
{
    internal enum Opcode : Int
    {
        case NotUsed = 0
        case Read
        case Data
        case Abort
        case Stream
        case Hibernate
    }

    internal var `Type` : Opcode;
    internal var Address : UInt32;
    internal var Length : UInt16;
    internal var Data : [UInt8]

    internal init(type : Opcode, address : UInt32, length : UInt16, data : [UInt8])
    {
        `Type` = type;
        Address = address;
        Length = length;
        Data = [UInt8]();
        Data.append(contentsOf: data)
    }

    internal init(array : [UInt8])
    {
        `Type` = Opcode(rawValue: Int.init(array[0])) ?? .NotUsed;
        if (`Type` == Opcode.Stream)
        {
            Address = MemMap.MEM_MAP_CONF_ADDR;
            Length = Utilities.ToUInt16(data: array, startIndex: 1);
            Data = [UInt8]();
            Data.append(contentsOf: array[3..<array.count])
        }
        else
        {
            Address = Utilities.ToUInt32(data: array, startIndex: 1);
            Length = Utilities.ToUInt16(data: array, startIndex: 5);
            Data = [UInt8]();
            Data.append(contentsOf: array[7..<array.count])
        }
    }

    internal func ToArray() -> [UInt8]
    {
        var result : [UInt8] = [UInt8]();
        result.append(UInt8.init(`Type`.rawValue));
        result.append(contentsOf: withUnsafeBytes(of: Address.littleEndian) { Array($0) })
        result.append(contentsOf: withUnsafeBytes(of: Length.littleEndian) { Array($0) })
        result.append(contentsOf: Data)
        return result;
    }
}
