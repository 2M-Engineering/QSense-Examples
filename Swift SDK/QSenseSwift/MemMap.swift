//
//  MemMap.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 19/07/2024.
//

import Foundation

internal struct MemMap
{
    static internal let MEM_MAP_CTRL_ADDR : UInt32 = 0x00000000;
    static internal let MEM_MAP_CONF_ADDR : UInt32 = 0x00000100;
    static internal let MEM_MAP_CTRL_SIZE : UInt32 = 0x00000055;
    static internal let MEM_MAP_CONF_SIZE : UInt32 = 0x000000ED;
    static internal let MEM_MAP_WHOAMI : UInt32 = 0x50474345;
    static internal let MEM_MAP_PIN : UInt32 = 0x65766F6c;

    static internal let MEM_MAP_ADDR_whoami : UInt32 = 0x00000000;
    static internal let MEM_MAP_ADDR_id : UInt32 = 0x00000004;
    static internal let MEM_MAP_ADDR_address : UInt32 = 0x0000000C;
    static internal let MEM_MAP_ADDR_version : UInt32 = 0x00000014;
    static internal let MEM_MAP_ADDR_battery : UInt32 = 0x00000018;
    static internal let MEM_MAP_ADDR_motion_level : UInt32 = 0x00000019;
    static internal let MEM_MAP_ADDR_offset_compensated : UInt32 = 0x0000001A;
    static internal let MEM_MAP_ADDR_mag_field_mapped : UInt32 = 0x0000001B;
    static internal let MEM_MAP_ADDR_mag_field_mapping_progress : UInt32 = 0x0000001C;
    static internal let MEM_MAP_ADDR_con_interval : UInt32 = 0x0000001D;
    static internal let MEM_MAP_ADDR_sync_status : UInt32 = 0x0000001E;
    static internal let MEM_MAP_ADDR_100Hz_ticks : UInt32 = 0x0000001F;
    static internal let MEM_MAP_ADDR_pin : UInt32 = 0x00000020;
    static internal let MEM_MAP_ADDR_time : UInt32 = 0x00000024;
    static internal let MEM_MAP_ADDR_annotation : UInt32 = 0x00000028;
    static internal let MEM_MAP_ADDR_logging : UInt32 = 0x00000029;
    static internal let MEM_MAP_ADDR_device_state : UInt32 = 0x0000002A;
    static internal let MEM_MAP_ADDR_ui_state : UInt32 = 0x0000002C;
    static internal let MEM_MAP_ADDR_device_name : UInt32 = 0x00000030;
    static internal let MEM_MAP_ADDR_data_mode : UInt32 = 0x0000003C;
    static internal let MEM_MAP_ADDR_timesync : UInt32 = 0x0000003D;
    static internal let MEM_MAP_ADDR_timesync_ui : UInt32 = 0x0000003E;
    static internal let MEM_MAP_ADDR_algorithm_selection : UInt32 = 0x0000003F;
    static internal let MEM_MAP_ADDR_erase_file : UInt32 = 0x00000050;
    static internal let MEM_MAP_ADDR_packet_count : UInt32 = 0x00000051;
}

internal class MemMapCtrl
{
    internal var WhoAmI : UInt32 = 0;
    internal var Id : UInt64 = 0;
    internal var Address : UInt64 = 0;
    internal var Version : UInt32 = 0;
    internal var Battery : UInt8 = 0;
    internal var MotionLevel : Float = 0;
    internal var OffsetCompensated : Bool = false;
    internal var MagFieldMapped : Bool = false;
    internal var MagFieldMappingProgress : UInt8 = 0;
    internal var ConnInterval : UInt8 = 0;
    internal var SyncStatus : UInt8 = 0;
    internal var Milliseconds : UInt16 = 0;
    internal var Pin : UInt32 = 0;
    internal var Time : UInt32 = 0;
    internal var Annotation : UInt8 = 0;
    internal var DeviceState : UInt16 = 0;
    internal var UiState : UInt32 = 0;
    internal var DeviceName : String = "";
    internal var DataMode : UInt8 = 0;
    internal var Timesync : UInt8 = 0;
    internal var TimesyncUi : UInt8 = 0;
    internal var AlgorithmSelection : UInt8 = 0;
    internal var Logging : UInt8 = 0;
    internal var PacketCount : UInt32 = 0;

    internal init(buffer : [UInt8]) throws
    {
        if (buffer.count != MemMap.MEM_MAP_CTRL_SIZE)
        {
            throw NSError.init(domain: "DeiceMemMapCtrl", code: 0, userInfo: ["Description" : "wrong input size"]);
        }
        WhoAmI = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_whoami));
        Id = Utilities.ToUInt64(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_id));
        Address = Utilities.ToUInt64(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_address));
        Version = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_version));
        Battery = buffer[Int.init(MemMap.MEM_MAP_ADDR_battery)];
        MotionLevel = Float.init(buffer[Int.init(MemMap.MEM_MAP_ADDR_motion_level)]) / 255.0;
        OffsetCompensated = buffer[Int.init(MemMap.MEM_MAP_ADDR_offset_compensated)] != 0x00;
        MagFieldMapped = buffer[Int.init(MemMap.MEM_MAP_ADDR_mag_field_mapped)] != 0x00;
        MagFieldMappingProgress = buffer[Int.init(MemMap.MEM_MAP_ADDR_mag_field_mapping_progress)];
        ConnInterval = buffer[Int.init(MemMap.MEM_MAP_ADDR_con_interval)];
        SyncStatus = buffer[Int.init(MemMap.MEM_MAP_ADDR_sync_status)];
        Milliseconds = UInt16.init(buffer[Int.init(MemMap.MEM_MAP_ADDR_100Hz_ticks)]) * 10;
        Pin = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_pin));
        Time = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_time));
        Annotation = buffer[Int.init(MemMap.MEM_MAP_ADDR_annotation)];
        DeviceState = Utilities.ToUInt16(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_device_state));
        UiState = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_ui_state));
        DeviceName = String(bytes: buffer[Int.init(MemMap.MEM_MAP_ADDR_device_name)..<Int.init(MemMap.MEM_MAP_ADDR_device_name+8)], encoding: .utf8) ?? "";
        DataMode = buffer[Int.init(MemMap.MEM_MAP_ADDR_data_mode)];
        Timesync = buffer[Int.init(MemMap.MEM_MAP_ADDR_timesync)];
        TimesyncUi = buffer[Int.init(MemMap.MEM_MAP_ADDR_timesync_ui)];
        AlgorithmSelection = buffer[Int.init(MemMap.MEM_MAP_ADDR_algorithm_selection)];
        Logging = buffer[Int.init(MemMap.MEM_MAP_ADDR_logging)];
        PacketCount = Utilities.ToUInt32(data: buffer, startIndex: Int.init(MemMap.MEM_MAP_ADDR_packet_count));
    }
}

internal class MemMapData
{
    internal var Packet : StreamPacket?;

    internal init()
    {
        Packet = nil;
    }

    internal func AddData(buffer : [UInt8])
    {
        do 
        {
            try Packet = StreamPacket.init(buffer: buffer);
        }
        catch
        {
            
        }
    }
}
