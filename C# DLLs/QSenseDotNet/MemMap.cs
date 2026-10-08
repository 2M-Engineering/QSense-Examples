using System;
using System.Text;

namespace QSenseDotNet
{
    internal class MemMap
    {
        internal const uint MEM_MAP_CTRL_ADDR = 0x00000000;
        internal const uint MEM_MAP_CONF_ADDR = 0x00000100;
        internal const uint MEM_MAP_DATA_ADDR = 0x10000000;
        internal const uint MEM_MAP_FILE_ADDR = 0x20000000;
        internal const uint MEM_MAP_CTRL_SIZE = 0x00000055;
        internal const uint MEM_MAP_CTRL_SIZE_HUB = 0x000000C0;
        internal const uint MEM_MAP_CONF_SIZE = 237;
        internal const uint MEM_MAP_DATA_SIZE = 0x000003D0;
        internal const uint MEM_MAP_FILE_SIZE = 0x008ADE00;
        internal const uint MEM_MAP_WHOAMI = 0x324D5351; /* "QSM2"*/
        internal const uint MEM_MAP_PIN = 0x65766F6c;
        internal const uint MEM_MAP_PIN_TEST = 0x74736574;

        internal const uint MEM_MAP_ADDR_whoami = 0x00000000;
        internal const uint MEM_MAP_ADDR_id = 0x00000004;
        internal const uint MEM_MAP_ADDR_address = 0x0000000C;
        internal const uint MEM_MAP_ADDR_version = 0x00000014;
        internal const uint MEM_MAP_ADDR_battery = 0x00000018;
        internal const uint MEM_MAP_ADDR_motion_level = 0x00000019;
        internal const uint MEM_MAP_ADDR_offset_compensated = 0x0000001A;
        internal const uint MEM_MAP_ADDR_mag_field_mapped = 0x0000001B;
        internal const uint MEM_MAP_ADDR_mag_field_mapping_progress = 0x0000001C;
        internal const uint MEM_MAP_ADDR_conn_interval = 0x0000001D;
        internal const uint MEM_MAP_ADDR_100hz_ticks = 0x0000001F;
        internal const uint MEM_MAP_ADDR_pin = 0x00000020;
        internal const uint MEM_MAP_ADDR_time = 0x00000024;
        internal const uint MEM_MAP_ADDR_annotation = 0x00000028;
        internal const uint MEM_MAP_ADDR_logging = 0x00000029;
        internal const uint MEM_MAP_ADDR_state = 0x0000002A;
        internal const uint MEM_MAP_ADDR_ui_state = 0x0000002C;
        internal const uint MEM_MAP_ADDR_device_name = 0x00000030;
        internal const uint MEM_MAP_ADDR_data_mode = 0x0000003C;
        internal const uint MEM_MAP_ADDR_esb_radio = 0x0000003D;
        internal const uint MEM_MAP_ADDR_hub_mode = 0x0000003E;
        internal const uint MEM_MAP_ADDR_algorithm_selection = 0x0000003F;
        internal const uint MEM_MAP_ADDR_set_quaternion = 0x00000040;
        internal const uint MEM_MAP_ADDR_erase_file = 0x00000050;
        internal const uint MEM_MAP_ADDR_packet_count = 0x00000051;

        // Hub only
        internal const uint MEM_MAP_ADDR_sync_status = 0x0000001E;
        internal const uint MEM_MAP_ADDR_button_start_enable = 0x00000029;
        internal const uint MEM_MAP_ADDR_data_size = 0x00000050;
        internal const uint MEM_MAP_ADDR_status = 0x00000054;
        internal const uint MEM_MAP_ADDR_start_time = 0x00000058;
        internal const uint MEM_MAP_ADDR_stop_time = 0x0000005C;
        internal const uint MEM_MAP_ADDR_whitelist = 0x00000060;
        internal const uint MEM_MAP_ADDR_sampling_counter = 0x00000098;
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
        internal byte[] State { get; set; }
        internal UInt32 UiState { get; set; }
        internal string Name { get; set; } = "";
        internal DataMode DataMode { get; set; }
        internal UInt32 Time { get; set; }
        internal UInt16 Milliseconds { get; set; }
        internal byte EsbRadio { get; set; }
        internal byte HubMode { get; set; }
        internal byte Annotation { get; set; }
        internal byte AlgorithmSelection { get; set; }
        internal bool Logging { get; set; }
        internal UInt32 PacketCount { get; set; }

        // Hub Only
        internal byte HubSyncStatus { get; set; }
        internal byte HubButtonStartEnable { get; set; }
        internal UInt32 HubDataSize { get; set; }
        internal UInt32 HubStatus { get; set; }
        internal UInt32 HubStartTime { get; set; }
        internal UInt32 HubStopTime { get; set; }
        internal UInt64[] HubWhitelist { get; set; } = new UInt64[0];
        internal UInt32[] HubSamplingCounter { get; set; } = new UInt32[0];

        internal MemMapCtrl(Byte[] buffer)
        {
            if (buffer.Length >= 4) WhoAmI = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_whoami);
            if (buffer.Length >= 12) Id = BitConverter.ToUInt64(buffer, (int)MemMap.MEM_MAP_ADDR_id);
            if (buffer.Length >= 20) Address = BitConverter.ToUInt64(buffer, (int)MemMap.MEM_MAP_ADDR_address);
            if (buffer.Length >= 24) Version = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_version);
            if (WhoAmI == MemMap.MEM_MAP_WHOAMI) ParseMemMapSensor(buffer);
            else ParseMemMapHub(buffer);
        }

        private void ParseMemMapSensor(byte[] buffer)
        {
            if (buffer.Length >= 25) Battery = buffer[(int)MemMap.MEM_MAP_ADDR_battery];
            if (buffer.Length >= 26) MotionLevel = buffer[(int)MemMap.MEM_MAP_ADDR_motion_level] / 255.0f;
            if (buffer.Length >= 30)
            {
                State = new byte[6];
                Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_offset_compensated, State, 0, 4);
            }
            if (buffer.Length >= 32) Milliseconds = (UInt16)(buffer[(int)MemMap.MEM_MAP_ADDR_100hz_ticks] * 10);
            if (buffer.Length >= 36) Pin = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_pin);
            if (buffer.Length >= 40) Time = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_time);
            if (buffer.Length >= 41) Annotation = buffer[(int)MemMap.MEM_MAP_ADDR_annotation];
            if (buffer.Length >= 42) Logging = buffer[(int)MemMap.MEM_MAP_ADDR_logging] == 0x01;
            if (buffer.Length >= 44) Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_state, State, 4, 2);
            if (buffer.Length >= 48) UiState = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_ui_state);
            if (buffer.Length >= 60) Name = Encoding.ASCII.GetString(buffer, (int)MemMap.MEM_MAP_ADDR_device_name, 12);
            if (buffer.Length >= 61) DataMode = (DataMode)buffer[(int)MemMap.MEM_MAP_ADDR_data_mode];
            if (buffer.Length >= 62) EsbRadio = buffer[(int)MemMap.MEM_MAP_ADDR_esb_radio];
            if (buffer.Length >= 63) HubMode = buffer[(int)MemMap.MEM_MAP_ADDR_hub_mode];
            if (buffer.Length >= 64) AlgorithmSelection = buffer[(int)MemMap.MEM_MAP_ADDR_algorithm_selection];
            if (buffer.Length >= 85) PacketCount = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_packet_count);
            if (Version < ((2 << 16) | 3 << 8 | 2))
            {
                PacketCount = 0;
            }
        }

        private void ParseMemMapHub(byte[] buffer)
        {
            if (buffer.Length >= 25) Battery = buffer[(int)MemMap.MEM_MAP_ADDR_battery];
            if (buffer.Length >= 26) MotionLevel = buffer[(int)MemMap.MEM_MAP_ADDR_motion_level] / 255.0f;
            if (buffer.Length >= 30)
            {
                State = new byte[6];
                Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_offset_compensated, State, 0, 4);
            }
            if (buffer.Length >= 31) HubSyncStatus = buffer[MemMap.MEM_MAP_ADDR_sync_status];
            if (buffer.Length >= 32) Milliseconds = (UInt16)(buffer[(int)MemMap.MEM_MAP_ADDR_100hz_ticks] * 10);
            if (buffer.Length >= 36) Pin = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_pin);
            if (buffer.Length >= 40) Time = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_time);
            if (buffer.Length >= 41) Annotation = buffer[(int)MemMap.MEM_MAP_ADDR_annotation];
            if (buffer.Length >= 42) HubButtonStartEnable = buffer[MemMap.MEM_MAP_ADDR_button_start_enable];
            if (buffer.Length >= 44) Array.Copy(buffer, (int)MemMap.MEM_MAP_ADDR_state, State, 4, 2);
            if (buffer.Length >= 48) UiState = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_ui_state);
            if (buffer.Length >= 60) Name = Encoding.ASCII.GetString(buffer, (int)MemMap.MEM_MAP_ADDR_device_name, 12).Replace("\0", string.Empty);
            if (buffer.Length >= 61) DataMode = (DataMode)buffer[(int)MemMap.MEM_MAP_ADDR_data_mode];
            if (buffer.Length >= 62) EsbRadio = buffer[(int)MemMap.MEM_MAP_ADDR_esb_radio];
            if (buffer.Length >= 63) HubMode = buffer[(int)MemMap.MEM_MAP_ADDR_hub_mode];
            if (buffer.Length >= 64) AlgorithmSelection = buffer[(int)MemMap.MEM_MAP_ADDR_algorithm_selection];
            if (buffer.Length >= 85) PacketCount = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_packet_count);
            if (buffer.Length == MemMap.MEM_MAP_CTRL_SIZE_HUB)
            {
                HubDataSize = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_data_size);
                HubStatus = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_status);
                HubStartTime = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_start_time);
                HubStopTime = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_stop_time);
                HubWhitelist = new UInt64[Math.Min(9, (int)BitConverter.ToUInt16(buffer, (int)MemMap.MEM_MAP_ADDR_whitelist + 54))];
                HubSamplingCounter = new uint[10];
                for (int i = 0; i < HubWhitelist.Length; i++)
                {
                    HubWhitelist[i] = 0;
                    for (int j = 0; j < 6; j++)
                    {
                        HubWhitelist[i] += (ulong)buffer[i * 6 + (int)MemMap.MEM_MAP_ADDR_whitelist + j] << (j * 8);
                    }
                }
                for (int i = 0; i < HubSamplingCounter.Length; i++)
                {
                    HubSamplingCounter[i] = BitConverter.ToUInt32(buffer, (int)MemMap.MEM_MAP_ADDR_sampling_counter + 4 * i);
                }
            }
            if (string.IsNullOrEmpty(Name))
            {
                Name = "QSMHub-" + Id.ToString("X").Substring(0, 4);
            }
        }

        internal byte[] ToByteArray()
        {
            Byte[] result = new Byte[MemMap.MEM_MAP_CTRL_SIZE_HUB];
            Array.Copy(BitConverter.GetBytes(WhoAmI), 0, result, MemMap.MEM_MAP_ADDR_whoami, 4);
            Array.Copy(BitConverter.GetBytes(Id), 0, result, MemMap.MEM_MAP_ADDR_id, 8);
            Array.Copy(BitConverter.GetBytes(Address), 0, result, MemMap.MEM_MAP_ADDR_address, 8);
            Array.Copy(BitConverter.GetBytes(Version), 0, result, MemMap.MEM_MAP_ADDR_version, 4);
            result[MemMap.MEM_MAP_ADDR_battery] = (byte)Battery;
            result[MemMap.MEM_MAP_ADDR_motion_level] = (byte)(MotionLevel * 255);
            Array.Copy(State, 0, result, (int)MemMap.MEM_MAP_ADDR_state, 4);
            result[MemMap.MEM_MAP_ADDR_sync_status] = HubSyncStatus;
            result[MemMap.MEM_MAP_ADDR_100hz_ticks] = 0;
            result[MemMap.MEM_MAP_ADDR_100hz_ticks + 1] = 0;
            Array.Copy(BitConverter.GetBytes(Pin), 0, result, MemMap.MEM_MAP_ADDR_pin, 4);
            Array.Copy(BitConverter.GetBytes(Utilities.UnixTimeStampFromDateTime(DateTime.Now)), 0, result, MemMap.MEM_MAP_ADDR_time, 4);
            result[MemMap.MEM_MAP_ADDR_annotation] = Annotation;
            result[MemMap.MEM_MAP_ADDR_button_start_enable] = HubButtonStartEnable;
            Array.Copy(State, 4, result, MemMap.MEM_MAP_ADDR_state, 2);
            Array.Copy(BitConverter.GetBytes(UiState), 0, result, MemMap.MEM_MAP_ADDR_ui_state, 4);
            Array.Copy(Encoding.ASCII.GetBytes(Name), 0, result, MemMap.MEM_MAP_ADDR_device_name, Name.Length);
            result[MemMap.MEM_MAP_ADDR_data_mode] = (byte)DataMode;
            result[MemMap.MEM_MAP_ADDR_esb_radio] = EsbRadio;
            result[MemMap.MEM_MAP_ADDR_hub_mode] = HubMode;
            result[MemMap.MEM_MAP_ADDR_algorithm_selection] = AlgorithmSelection;
            Array.Copy(BitConverter.GetBytes(HubDataSize), 0, result, MemMap.MEM_MAP_ADDR_data_size, 4);
            Array.Copy(BitConverter.GetBytes(HubStatus), 0, result, MemMap.MEM_MAP_ADDR_status, 4);
            Array.Copy(BitConverter.GetBytes(HubStartTime), 0, result, MemMap.MEM_MAP_ADDR_start_time, 4);
            Array.Copy(BitConverter.GetBytes(HubStopTime), 0, result, MemMap.MEM_MAP_ADDR_stop_time, 4);
            Array.Copy(BitConverter.GetBytes((UInt16)HubWhitelist.Length), 0, result, (int)MemMap.MEM_MAP_ADDR_whitelist + 54, 2);
            for (int i = 0; i < HubWhitelist.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(HubWhitelist[i]), 0, result, i * 6 + (int)MemMap.MEM_MAP_ADDR_whitelist, 6);
            }
            for (int i = 0; i < HubSamplingCounter.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(HubSamplingCounter[i]), 0, result, MemMap.MEM_MAP_ADDR_sampling_counter + 4 * i, 4);
            }
            return result;
        }

    }

    internal class MemMapData
    {
        internal StreamPacket? Packet { get; set; }

        internal MemMapData()
        {
            Packet = null;
        }

        internal void AddData(Byte[] buffer)
        {
            Packet = new StreamPacket(buffer);
        }
    }
}