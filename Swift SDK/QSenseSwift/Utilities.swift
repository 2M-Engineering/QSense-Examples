//
//  Utilities.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 18/07/2024.
//

import Foundation
import simd

internal struct Utilities
{
    /// <summary>
    /// Converts a string of hex values into a byte array.
    /// </summary>
    /// <param name="hexString">The string that will be converted into a byte array.</param>
    /// <returns>A byte array.</returns>
    internal static func HexToByteArray(hexString : String) -> [UInt8]
    {
        var hi_nible : UInt8 = 0;
        var buffer : [UInt8] = [];
        var i : Int = 0;
        for char : Character in hexString
        {
            if (ValidHex(c: char))
            {
                if (i % 2 == 0) 
                {
                    hi_nible = char.utf8.map{UInt8($0)}[0];
                }
                else
                {
                    buffer.append(GetValFromHexChars(hi_nible: hi_nible, lo_nible: char.utf8.map{UInt8($0)}[0]));
                }
            }
            else
            {
                print("Corrupt packet");
                return [];
            }
            i+=1;
        }
        return buffer;
    }
    
    internal static func ToUInt16(data: [UInt8], startIndex : Int) -> UInt16
    {
        return (UInt16.init(data[startIndex + 1]) << 8) + UInt16.init(data[startIndex]);
    }
    
    internal static func ToInt16(data: [UInt8], startIndex : Int) -> Int16
    {
        return Int16(bitPattern: (UInt16.init(data[startIndex + 1]) << 8) + UInt16.init(data[startIndex]));
    }
    
    internal static func ToUInt32(data: [UInt8], startIndex : Int) -> UInt32
    {
        return (UInt32.init(data[startIndex + 3]) << 24) + (UInt32.init(data[startIndex + 2]) << 16) + (UInt32.init(data[startIndex + 1]) << 8) + UInt32.init(data[startIndex]);
    }
    
    internal static func ToInt32(data: [UInt8], startIndex : Int) -> Int32
    {
        return Int32.init((UInt32.init(data[startIndex + 3]) << 24) + (UInt32.init(data[startIndex + 2]) << 16) + (UInt32.init(data[startIndex + 1]) << 8) + UInt32.init(data[startIndex]));
    }
    
    internal static func ToUInt64(data: [UInt8], startIndex : Int) -> UInt64
    {
        return (UInt64.init(ToUInt32(data: data, startIndex: startIndex + 4)) << 32) + UInt64.init(ToUInt32(data: data, startIndex: startIndex));
    }
    
    internal static func ToSingle(data: [UInt8], startIndex : Int) -> Float
    {
        let myInt : UInt32 = (UInt32.init(data[startIndex + 3]) << 24) + (UInt32.init(data[startIndex + 2]) << 16) + (UInt32.init(data[startIndex + 1]) << 8) + UInt32.init(data[startIndex])
        return Float(bitPattern: myInt)
    }

    private static func ValidHex(c : Character) -> Bool
    {
        return (c >= "0" && c <= "9") || (c >= "A" && c <= "F") || (c >= "a" && c <= "f");
    }

    private static func GetValFromHexChars(hi_nible : UInt8, lo_nible : UInt8) -> UInt8
    {
        var result : UInt8 = 0;
        if (hi_nible >= 0x30 && hi_nible <= 0x39)
        {
            result = hi_nible - 0x30;
        }
        else if (hi_nible >= 0x41 && hi_nible <= 0x46)
        {
            result = 10 + hi_nible - 0x41;
        }
        else if (hi_nible >= 0x61 && hi_nible <= 0x66)
        {
            result = 10 + hi_nible - 0x61;
        }
        result = result << 4;
        if (lo_nible >= 0x30 && lo_nible <= 0x39)
        {
            result += lo_nible - 0x30;
        }
        else if (lo_nible >= 0x41 && lo_nible <= 0x46)
        {
            result += 10 + lo_nible - 0x41;
        }
        else if (lo_nible >= 0x61 && lo_nible <= 0x66)
        {
            result += 10 + lo_nible - 0x61;
        }
        return result;
    }
    //#endregion
}
