using System;
using System.Text;

namespace QSenseDotNet
{
    internal class MemMap
    {
        internal const uint MEM_MAP_CTRL_ADDR = 0x00000000;
        internal const uint MEM_MAP_CONF_ADDR = 0x00000100;
        internal const uint MEM_MAP_FILE_ADDR = 0x20000000;
        internal const uint MEM_MAP_CTRL_SIZE = 0x00000055;
        internal const uint MEM_MAP_CONF_SIZE_V1 = 213;
        internal const uint MEM_MAP_CONF_SIZE_V2 = 237;
        internal const uint MEM_MAP_FILE_SIZE = 0x008ADE00;
        internal const uint MEM_MAP_WHOAMI = 0x324D5351; /* "QSM2"*/
        internal const uint MEM_MAP_PIN = 0x65766F6c;

        internal const uint MEM_MAP_ADDR_whoami = 0x00000000;
        internal const uint MEM_MAP_ADDR_id = 0x00000004;
        internal const uint MEM_MAP_ADDR_address = 0x0000000C;
        internal const uint MEM_MAP_ADDR_version = 0x00000014;
        internal const uint MEM_MAP_ADDR_battery = 0x00000018;
        internal const uint MEM_MAP_ADDR_pin = 0x00000020;
        #region V2 and higher
        internal const uint MEM_MAP_ADDR_motion_level_v2 = 0x00000019;
        internal const uint MEM_MAP_ADDR_offset_compensated_v2 = 0x0000001A;
        internal const uint MEM_MAP_ADDR_mag_field_mapped_v2 = 0x0000001B;
        internal const uint MEM_MAP_ADDR_mag_field_mapping_progress_v2 = 0x0000001C;
        internal const uint MEM_MAP_ADDR_conn_interval_v2 = 0x0000001D;
        internal const uint MEM_MAP_ADDR_100hz_ticks_v2 = 0x0000001F;
        internal const uint MEM_MAP_ADDR_time_v2 = 0x00000024;
        internal const uint MEM_MAP_ADDR_annotation_v2 = 0x00000028;
        internal const uint MEM_MAP_ADDR_logging_v2 = 0x00000029;
        internal const uint MEM_MAP_ADDR_state_v2 = 0x0000002A;
        internal const uint MEM_MAP_ADDR_ui_state_v2 = 0x0000002C;
        internal const uint MEM_MAP_ADDR_device_name_v2 = 0x00000030;
        internal const uint MEM_MAP_ADDR_data_mode_v2 = 0x0000003C;
        internal const uint MEM_MAP_ADDR_timesync_v2 = 0x0000003D;
        internal const uint MEM_MAP_ADDR_timesync_ui_v2 = 0x0000003E;
        internal const uint MEM_MAP_ADDR_algorithm_selection_v2 = 0x0000003F;
        internal const uint MEM_MAP_ADDR_erase_file_v2 = 0x00000050;
        internal const uint MEM_MAP_ADDR_packet_count_v2 = 0x00000051;
        #endregion
    }

    internal class MemMapCtrl
    {
        internal UInt32 WhoAmI { get; set; }
        internal UInt64 Id { get; set; }
        internal UInt64 Address { get; set; }
        internal UInt32 Version { get; set; }
        internal UInt32 Battery { get; set; }
        internal float MotionLevel { get; set; }
        internal UInt32 Pin { get; set; }
        internal byte[] State { get; set; } = new byte[0];
        internal UInt32 UiState { get; set; }
        internal string Name { get; set; } = "";
        internal DataMode DataMode { get; set; }
        internal UInt32 Time { get; set; }
        internal UInt16 Milliseconds { get; set; }
        internal byte Timesync { get; set; }
        internal byte TimesyncUi { get; set; }
        internal byte Annotation { get; set; }
        internal byte AlgorithmSelection { get; set; }
        internal byte Logging { get; set; }
        internal UInt32 PacketCount { get; set; }

        internal MemMapCtrl(Byte[] buffer)
        {
            if (buffer.Length >= 4) WhoAmI = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_whoami);
            if (buffer.Length >= 12) Id = BitConverter.ToUInt64(buffer, (int)MemMap.MEM_MAP_ADDR_id);
            if (buffer.Length >= 20) Address = BitConverter.ToUInt64(buffer, (int)MemMap.MEM_MAP_ADDR_address);
            if (buffer.Length >= 24) Version = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_version);
            if (buffer[(int)MemMap.MEM_MAP_ADDR_version + 2] == 1) throw new Exception("Unsupported");
            else ParseV2MemMap(buffer);
        }

        private void ParseV2MemMap(byte[] buffer)
        {
            if (buffer.Length >= 25) Battery = buffer[(int)MemMap.MEM_MAP_ADDR_battery];
            if (buffer.Length >= 26) MotionLevel = buffer[(int)MemMap.MEM_MAP_ADDR_motion_level_v2] / 255.0f;
            if (buffer.Length >= 30)
            {
                State = new byte[6];
                Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_offset_compensated_v2, State, 0, 4);
            }
            if (buffer.Length >= 32) Milliseconds = (UInt16)(buffer[(int)MemMap.MEM_MAP_ADDR_100hz_ticks_v2] * 10);
            if (buffer.Length >= 36) Pin = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_pin);
            if (buffer.Length >= 40) Time = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_time_v2);
            if (buffer.Length >= 41) Annotation = buffer[(int)MemMap.MEM_MAP_ADDR_annotation_v2];
            if (buffer.Length >= 42) Logging = buffer[(int)MemMap.MEM_MAP_ADDR_logging_v2];
            if (buffer.Length >= 44) Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_state_v2, State, 4, 2);
            if (buffer.Length >= 48) UiState = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_ui_state_v2);
            if (buffer.Length >= 48) Name = Encoding.ASCII.GetString(buffer, (int)MemMap.MEM_MAP_ADDR_device_name_v2, 12);
            if (buffer.Length >= 61) DataMode = (DataMode)buffer[(int)MemMap.MEM_MAP_ADDR_data_mode_v2];
            if (buffer.Length >= 62) Timesync = buffer[(int)MemMap.MEM_MAP_ADDR_timesync_v2];
            if (buffer.Length >= 63) TimesyncUi = buffer[(int)MemMap.MEM_MAP_ADDR_timesync_ui_v2];
            if (buffer.Length >= 64) AlgorithmSelection = buffer[(int)MemMap.MEM_MAP_ADDR_algorithm_selection_v2];
            if (buffer.Length >= 85) PacketCount = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_packet_count_v2);
        }
    }

    internal class MemMapData
    {
        internal StreamPacket? Packet { get; set; }

        internal MemMapData()
        {
            Packet = null;
        }

        internal void AddData(Byte[] buffer, float accScale, float gyrScale)
        {
            Packet = new StreamPacket(buffer, accScale, gyrScale);
        }
    }
}