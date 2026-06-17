//
//  StreamPacket.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 18/07/2024.
//

import Foundation
import simd

internal class StreamPacket
{
    private let PacketSize : Int = 237;
    private let accScaleFactors : [Float] = [ 0.000061, 0.000488, 0.000122, 0.000244 ];
    private let gyrScaleFactors : [Float]  = [ 0.00875, 0.004375, 0.0175, 0.0, 0.035, 0.0, 0.07 ];
    
    internal var Raw : [Raw9Dof];
    internal var Quaternion : [simd_quatf];
    internal var FreeAcceleration : [Float];
    
    internal var Seconds : UInt32 = 0;
    internal var Milliseconds : Float = 0;
    internal var Battery : UInt8 = 0;
    internal var Annotation : UInt8 = 0;
    internal var SyncOk : Bool = false;
    internal var DataMode : DataModes;
    internal var Interference : UInt8;
    internal var Buffering : UInt8;


    internal init(buffer : [UInt8]) throws
    {
        if (buffer.count != PacketSize)
        {
            throw NSError.init(domain: "StreamPacket", code: 0, userInfo: ["Description" : "Wrong PacketSize"]);
        }
        DataMode = DataModes(rawValue: Int.init(buffer[0] & 0x0F)) ?? .Mixed;
        Buffering = buffer[0] >> 4;
        Seconds = Utilities.ToUInt32(data: buffer, startIndex: 1);
        Milliseconds = Float.init(Utilities.ToUInt16(data: buffer, startIndex: 5)) * 1.25;
        Interference = buffer[7] & 0x07;
        Battery = buffer[7] >> 3;
        Annotation = buffer[8];
        SyncOk = (buffer[9] & 0x01) == 1;
        let accScale : Float = accScaleFactors[Int.init((buffer[9] & 0x30) >> 4)];
        let gyrScale : Float = gyrScaleFactors[Int.init((buffer[9] & 0x0E) >> 1)];

        switch (DataMode)
        {
            case .Mixed:
                FreeAcceleration = [Float](repeating: 0, count: 3);
                Quaternion = [simd_quatf](repeating: simd_quatf.init(), count: 1);
                Raw = [Raw9Dof](repeating: Raw9Dof.init(), count: Int.init(Buffering));
            break;
            case .Raw:
                FreeAcceleration = [Float](repeating: 0, count: 0);
                Raw = [Raw9Dof](repeating: Raw9Dof.init(), count: Int.init(Buffering));
                Quaternion = [simd_quatf](repeating: simd_quatf.init(), count: 0);
            break;
            case .Quat:
                Raw = [Raw9Dof](repeating: Raw9Dof.init(), count: 0);
                FreeAcceleration = [Float](repeating: 0, count: 0);
                Quaternion = [simd_quatf](repeating: simd_quatf.init(), count: Int.init(Buffering));
            break;
            case .Optimized:
                FreeAcceleration = [Float](repeating: 0, count: 0);
                Raw = [Raw9Dof](repeating: Raw9Dof.init(), count: Int.init(Buffering));
                Quaternion = [simd_quatf](repeating: simd_quatf.init(), count: Int.init(Buffering));
            break;
            case .Quat6Dof:
                FreeAcceleration = [Float](repeating: 0, count: 0);
                Raw = [Raw9Dof](repeating: Raw9Dof.init(), count: Int.init(Buffering));
                Quaternion = [simd_quatf](repeating: simd_quatf.init(), count: Int.init(Buffering));
                break;
        }
        var i : Int = 0;
        
        if (DataMode == .Optimized)
        {
            for j : Int in 0..<Quaternion.count
            {
                Quaternion[j] = simd_quatf.init(
                    ix: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 2)) / 32767.0,
                    iy: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 4)) / 32767.0,
                    iz: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 6)) / 32767.0,
                    r: Float.init(Utilities.ToInt16(data: buffer, startIndex: i)) / 32767.0
                );
                i += 8;
            }
            i += 8 * (10 - Int.init(Buffering));
            for j : Int in 0..<Raw.count
            {
                Raw[j] = Raw9Dof.init(buffer: buffer, position: i, accScale: accScale, gyrScale: gyrScale);
                i += 12;
            }
        }
        else if (DataMode == .Quat6Dof)
        {
            for j : Int in 0..<Quaternion.count
            {
                Quaternion[j] = simd_quatf.init(
                    ix: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 2)) / 32767.0,
                    iy: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 4)) / 32767.0,
                    iz: Float.init(Utilities.ToInt16(data: buffer, startIndex: i + 6)) / 32767.0,
                    r: Float.init(Utilities.ToInt16(data: buffer, startIndex: i)) / 32767.0
                );
                i += 8;
            }
            i += 8 * (10 - Int.init(Buffering));
            for j : Int in 0..<Raw.count
            {
                Raw[j] = Raw9Dof.init(buffer: buffer, position: i, accScale: 0, gyrScale: 0, includeMag: true, includeAccGyr: false);
                i += 6;
            }
        }
        else
        {
            for j : Int in 0..<Quaternion.count
            {
                Quaternion[j] = simd_quatf.init(
                    ix: Utilities.ToSingle(data: buffer, startIndex: i + 4),
                    iy: Utilities.ToSingle(data: buffer, startIndex: i + 8),
                    iz: Utilities.ToSingle(data: buffer, startIndex: i + 12),
                    r: Utilities.ToSingle(data: buffer, startIndex: i)
                );
                i += 16;
            }

            for j : Int in 0..<FreeAcceleration.count
            {
                FreeAcceleration[j] = Utilities.ToSingle(data: buffer, startIndex: i);
                i += 4;
            }

            for j : Int in 0..<Raw.count
            {
                Raw[j] = Raw9Dof.init(buffer: buffer, position: i, accScale: accScale, gyrScale: gyrScale, includeMag: true);
                i += 18;
            }
        }
    }
}

internal class Raw9Dof
{
    private let MAG_SCALE : Float = 0.0015;
    public var AccX : Float = 0;
    public var AccY : Float = 0;
    public var AccZ : Float = 0;
    public var GyrX : Float = 0;
    public var GyrY : Float = 0;
    public var GyrZ : Float = 0;
    public var MagX : Float = 0;
    public var MagY : Float = 0;
    public var MagZ : Float = 0;

    internal init()
    {
        
    }
    
    internal init(buffer: [UInt8], position : Int, accScale : Float, gyrScale : Float, includeMag : Bool = false, includeAccGyr : Bool = true)
    {
        if (includeAccGyr)
        {
            AccX = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 0))  * accScale;
            AccY = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 2))  * accScale;
            AccZ = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 4))  * accScale;
            GyrX = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 6))  * gyrScale;
            GyrY = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 8))  * gyrScale;
            GyrZ = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + 10)) * gyrScale;
        }
        if (includeMag)
        {
            let offset : Int = includeAccGyr ? 12 : 0;
            MagX = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + offset)) * MAG_SCALE;
            MagY = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + offset + 2)) * MAG_SCALE;
            MagZ = Float.init(Utilities.ToInt16(data: buffer, startIndex: position + offset + 3)) * MAG_SCALE;
        }
    }
}
