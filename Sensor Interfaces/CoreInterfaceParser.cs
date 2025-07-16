using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace QSenseExamples
{
    public static class CoreInterfaceParser
    {
        public enum Opcode
        {
            Read = 1,
            Data = 2,
            Abort = 3,
            Stream = 4
        }

        public enum PacketFieldAddress
        {
            Opcode = 0,
            Address = 1,
            Length = 5,
            Data = 7
        }

        public enum MemoryAddress
        {
            WhoAmI = 0x00000000,
            Id = 0x00000004,
            MacAddress = 0x0000000C,
            Version = 0x00000014,
            Battery = 0x00000018,
            MotionLevel = 0x00000019,
            OffsetCompensated = 0x0000001A,
            MagneticFieldMapped = 0x0000001B,
            MagneticFieldProgress = 0x0000001C,
            ConnectionInterval = 0x0000001D,
            SyncStatus = 0x0000001E,
            Ticks100Hz = 0x0000001F,
            Pin = 0x00000020,
            Time = 0x00000024,
            Annotation = 0x00000028,
            DeviceState = 0x0000002A,
            UiAnimation = 0x0000002C,
            DeviceName = 0x00000030,
            DataMode = 0x0000003C,
            Timesync = 0x0000003D,
            AlgorithmSelection = 0x0000003F
        }

        public enum DataMode
        {
            Mixed = 0,
            Raw = 1,
            Quat = 2,
            Optimized = 3,
            QuatMag = 4
        }

        const UInt32 CONTROL_MEMORY_ADDRESS = 0x00000000;
        const UInt32 CONTROL_MEMORY_SIZE = 0x00000040;
        const UInt32 STREAM_MEMORY_ADDRESS = 0x00000100;
        const UInt32 STREAM_MEMORY_SIZE = 237;
        const UInt32 WHOAMI_VALUE = 0x324D5351; /* "QSM2"*/
        const UInt32 PIN_VALUE = 0x65766F6c;
        const int HEADER_LENGTH = 10;

        public static byte[] CreateReadPacket(UInt32 address, UInt16 length)
        {
            byte[] packet = new byte[7];    // Opcode (1B) + Address (4B) + Length (2B) = 7B 
            packet[(int)PacketFieldAddress.Opcode] = (byte)Opcode.Read;
            Array.Copy(BitConverter.GetBytes(address), 0, packet, (int)PacketFieldAddress.Address, sizeof(UInt32));
            Array.Copy(BitConverter.GetBytes(length), 0, packet, (int)PacketFieldAddress.Length, sizeof(UInt16));
            return packet;
        }

        public static byte[] CreateDataPacket(UInt32 address, byte[] data)
        {
            UInt16 length = (UInt16)data.Length;
            byte[] packet = new byte[7 + data.Length];
            packet[(int)PacketFieldAddress.Opcode] = (int)Opcode.Read;
            Array.Copy(BitConverter.GetBytes(address), 0, packet, (int)PacketFieldAddress.Address, sizeof(UInt32));
            Array.Copy(BitConverter.GetBytes(length), 0, packet, (int)PacketFieldAddress.Length, sizeof(UInt16));
            Array.Copy(data, 0, packet, (int)PacketFieldAddress.Data, data.Length);
            return packet;
        }

        public static byte[] CreateAbortPacket()
        {
            byte[] packet = new byte[1];
            packet[(int)PacketFieldAddress.Opcode] = (int)Opcode.Abort;
            return packet;
        }

        public static byte[] CreateStreamPacket()
        {
            byte[] packet = new byte[7];
            packet[(int)PacketFieldAddress.Opcode] = (int)Opcode.Stream;
            Array.Copy(BitConverter.GetBytes(STREAM_MEMORY_ADDRESS), 0, packet, (int)PacketFieldAddress.Address, sizeof(UInt32));
            Array.Copy(BitConverter.GetBytes(STREAM_MEMORY_SIZE), 0, packet, (int)PacketFieldAddress.Length, sizeof(UInt16));
            return packet;
        }

        public static void ParsePacket(byte[] packet)
        {
            Opcode opcode = (Opcode)packet[(int)PacketFieldAddress.Opcode];
            UInt32 address = BitConverter.ToUInt32(packet, (int)PacketFieldAddress.Address);
            UInt16 length = BitConverter.ToUInt16(packet, (int)PacketFieldAddress.Length);

            string packetInfo = $"Opcode: {opcode}\tAddress: {address}\tLength: {length}\r\n";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(packetInfo);
#else
            Console.WriteLine(packetInfo);
#endif

            if (address + length <= CONTROL_MEMORY_SIZE)
            {
                byte[] data = new byte[length];
                Array.Copy(packet, (int)PacketFieldAddress.Data, data, 0, length);
                ParseControlMemory(address, length, data);
            }
            else if (address == STREAM_MEMORY_ADDRESS && length == STREAM_MEMORY_SIZE)
            {
                byte[] data = new byte[length];
                Array.Copy(packet, (int)PacketFieldAddress.Data, data, 0, length);
                ParseStreamData(data);
            }
        }

        private static void ParseControlMemory(UInt32 address, UInt16 length, byte[] data)
        {
            string dataInfo = "";
            if (address <= (UInt32)MemoryAddress.WhoAmI &&
                address + length >= (UInt32)MemoryAddress.WhoAmI + 4)
            {
                dataInfo += $"WhoAmI: {BitConverter.ToUInt32(data, (int)((UInt32)MemoryAddress.WhoAmI - address))}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Id &&
                address + length >= (UInt32)MemoryAddress.Id + 8)
            {
                dataInfo += $"Id: {BitConverter.ToUInt64(data, (int)((UInt32)MemoryAddress.Id - address))}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.MacAddress &&
                address + length >= (UInt32)MemoryAddress.MacAddress + 8)
            {
                dataInfo += $"Address: {BitConverter.ToUInt64(data, (int)((UInt32)MemoryAddress.MacAddress - address))}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Version &&
                address + length >= (UInt32)MemoryAddress.Version + 4)
            {

                dataInfo += $"Version: v{data[(UInt32)MemoryAddress.Version - address + 2]}.{data[(UInt32)MemoryAddress.Version - address + 1]}.{data[(UInt32)MemoryAddress.Version - address]}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Battery &&
                address + length >= (UInt32)MemoryAddress.Battery + 1)
            {
                dataInfo += $"Battery: {data[(UInt32)MemoryAddress.Battery - address]}%\r\n";
            }
            if (address <= (UInt32)MemoryAddress.MotionLevel &&
                address + length >= (UInt32)MemoryAddress.MotionLevel + 1)
            {
                dataInfo += $"Motion Level: {data[(UInt32)MemoryAddress.MotionLevel - address] / 255.0f}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.OffsetCompensated &&
                address + length >= (UInt32)MemoryAddress.OffsetCompensated + 1)
            {
                dataInfo += $"Offset Compensated: {data[(UInt32)MemoryAddress.OffsetCompensated - address] == 1}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.MagneticFieldMapped &&
                address + length >= (UInt32)MemoryAddress.MagneticFieldMapped + 1)
            {
                dataInfo += $"Magnetic Field Mapped: {data[(UInt32)MemoryAddress.MagneticFieldMapped - address] == 1}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.MagneticFieldProgress &&
                address + length >= (UInt32)MemoryAddress.MagneticFieldProgress + 1)
            {
                dataInfo += $"Magnetic Field Progress: {data[(UInt32)MemoryAddress.MagneticFieldProgress - address] * 10}%\r\n";
            }
            if (address <= (UInt32)MemoryAddress.ConnectionInterval &&
                address + length >= (UInt32)MemoryAddress.ConnectionInterval + 1)
            {
                dataInfo += $"Connection Interval: {data[(UInt32)MemoryAddress.ConnectionInterval - address] * 1.25}ms\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Ticks100Hz &&
                address + length >= (UInt32)MemoryAddress.Ticks100Hz + 1)
            {
                dataInfo += $"Milliseconds: {data[(UInt32)MemoryAddress.Ticks100Hz - address] * 10}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Pin &&
                address + length >= (UInt32)MemoryAddress.Pin + 4)
            {
                dataInfo += $"Pin: {BitConverter.ToUInt32(data, (int)((UInt32)MemoryAddress.Pin - address))}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Time &&
                address + length >= (UInt32)MemoryAddress.Time + 4)
            {
                dataInfo += $"Time: {BitConverter.ToUInt32(data, (int)((UInt32)MemoryAddress.Time - address))}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Annotation &&
                address + length >= (UInt32)MemoryAddress.Annotation + 1)
            {
                dataInfo += $"Annotation: {data[(UInt32)MemoryAddress.Annotation - address]}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.DeviceState &&
                address + length >= (UInt32)MemoryAddress.DeviceState + 2)
            {
                string[] samplingRates = new string[] { "1Hz", "2Hz", "4Hz", "5Hz", "10Hz", "20Hz", "25Hz", "50Hz", "100Hz", "200Hz", "400Hz", "800Hz" };
                string[] accRanges = new string[] { "2g", "16g", "4g", "8g" };
                string[] gyroRanges = new string[] { "250dps", "125dps", "500dps", "", "1000dps", "", "2000dps" };

                dataInfo += $"Enable Offset Compensation: {(data[(UInt32)MemoryAddress.DeviceState - address] & 0x01) == 1}\r\n";
                dataInfo += $"Enable Magnetic Field Mapping: {(data[(UInt32)MemoryAddress.DeviceState - address] & 0x02) == 1}\r\n";
                dataInfo += $"Accelerometer Range: {accRanges[(data[(UInt32)MemoryAddress.DeviceState - address] & 0x0C) >> 2]}\r\n";
                dataInfo += $"Gyroscope range: {gyroRanges[(data[(UInt32)MemoryAddress.DeviceState - address] & 0x70) >> 4]}\r\n";
                dataInfo += $"Sampling rate: {samplingRates[data[(UInt32)MemoryAddress.DeviceState + 1 - address] & 0x0F]}\r\n";
                dataInfo += $"Buffering: {data[(UInt32)MemoryAddress.DeviceState + 1 - address] >> 4}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.DeviceName &&
                address + length >= (UInt32)MemoryAddress.DeviceName + 1)
            {
                string name = Encoding.ASCII.GetString(data, (int)((UInt32)(UInt32)MemoryAddress.DeviceName - address), 12);
                dataInfo += $"Device Name: {name}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.DataMode &&
                address + length >= (UInt32)MemoryAddress.DataMode + 1)
            {
                string[] dataModes = new string[] { "Mixed", "Raw", "Quaternion", "Optimized", "Quat+Mag" };
                dataInfo += $"Data Mode: {dataModes[data[(UInt32)MemoryAddress.DataMode - address]]}\r\n";
            }
            if (address <= (UInt32)MemoryAddress.Timesync &&
                address + length >= (UInt32)MemoryAddress.Timesync + 1)
            {
                bool isEnabled = (data[(UInt32)MemoryAddress.Timesync] & 0x7F) != 0x00;
                bool isMaster = isEnabled && (data[(UInt32)MemoryAddress.Timesync] & 0x80) != 0x00;
                byte networkKey = (byte)(data[(UInt32)MemoryAddress.Timesync] & 0x7F);
                if (!isEnabled) dataInfo += $"Timesync: Disabled\r\n";
                else dataInfo += $"Timesync:\tIs master: {isMaster}\tNetwork Key: {networkKey}\r\n";

            }
            if (address <= (UInt32)MemoryAddress.AlgorithmSelection &&
                address + length >= (UInt32)MemoryAddress.AlgorithmSelection + 1)
            {
                string algorithm = data[(UInt32)MemoryAddress.AlgorithmSelection - address] == 0 ? "9 DoF" : "6 DoF";
                dataInfo += $"Algorithm election: {algorithm}\r\n";
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }

        private static void ParseStreamData(byte[] data)
        {
            string[] interferenceLevels = new string[] { "", "None", "Soft-iron interference", "Hard-iron interference", "Change of environment detected" };
            string[] dataModes = new string[] { "Mixed", "Raw", "Quaternion", "Optimized", "Quat+Mag" };
            string[] accRanges = new string[] { "2g", "16g", "4g", "8g" };
            string[] gyroRanges = new string[] { "250dps", "125dps", "500dps", "", "1000dps", "", "2000dps" };
            float[] accScaleFactors = new float[4] { (float)0.000061, (float)0.000488, (float)0.000122, (float)0.000244 };
            float[] gyrScaleFactors = new float[7] { (float)0.008750, (float)0.004375, (float)0.0175, 0.0f, (float)0.035, 0.0f, (float)0.07 };

            DataMode mode = (DataMode)(data[0] & 0x0F);
            string header = $"Stream Data Header:\r\nData Mode: {dataModes[(int)mode]}\r\n";
            int buffering = data[0] >> 4;
            header += $"Buffering: {buffering}\r\n";
            UInt32 seconds = BitConverter.ToUInt32(data, 1);
            float milliseconds = BitConverter.ToUInt16(data, 5) * 1.25f;
            DateTime timestamp = new DateTime(1970, 1, 1).AddSeconds(seconds).AddMilliseconds(milliseconds);
            header += $"Timestamp: {timestamp.ToString("MM/dd/yyyy,HH:mm:ss.fffff")}\r\n";
            header += $"Interference: {interferenceLevels[data[7] & 0x07]}\r\n";
            header += $"Battery: {(data[7] >> 3) * 6.25f}\r\n";
            header += $"Annotation: {data[8]}\r\n";
            header += $"Sync. Status: {(data[9] & 0x01) == 1}\r\n";
            header += $"Accelerometer range: {accRanges[(data[9] & 0x30) >> 4]}\r\n";
            header += $"Gyroscope range: {gyroRanges[(data[9] & 0x0E) >> 1]}\r\n";
            float accScale = accScaleFactors[(data[9] & 0x30) >> 4];
            float gyrScale = gyrScaleFactors[(data[9] & 0x0E) >> 1];

            string payload = "Stream Data Payload\r\n";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(header);
            System.Diagnostics.Debug.WriteLine(payload);
#else
            Console.WriteLine(header);
            Console.WriteLine(payload);
#endif 

            switch (mode)
            {
                case DataMode.Mixed:
                    ParseMixedPacket(data, buffering, accScale, gyrScale);
                    break;
                case DataMode.Raw:
                    ParseRawPacket(data, buffering, accScale, gyrScale);
                    break;
                case DataMode.Quat:
                    ParseQuatPacket(data, buffering);
                    break;
                case DataMode.Optimized:
                    ParseOptimizedPacket(data, buffering, accScale, gyrScale);
                    break;
                case DataMode.QuatMag:
                    ParseQuatMagPacket(data, buffering);
                    break;
                default:
                    break;
            }
        }

        private static void ParseQuatMagPacket(byte[] buffer, int buffering)
        {
            Raw9Dof[] rawData = new Raw9Dof[buffering];
            Quaternion[] quaternionData = new Quaternion[buffering];

            int index = HEADER_LENGTH;
            for (int j = 0; j < quaternionData.Length; j++)
            {
                quaternionData[j] = new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, index)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, index + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, index + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, index + 6)) / 32767.0f
                };
                index += 8;
            }
            index += 8 * (10 - buffering);
            for (int j = 0; j < rawData.Length; j++)
            {
                rawData[j] = new Raw9Dof(buffer, index);
                index += 6;
            }

            string dataInfo = "q.w\tq.x\tq.y\tq.z" +
                "\tmag.x\tmag.y\tmag.z\r\n";
            for (int i = 0; i < buffering; i++)
            {
                dataInfo += $"{quaternionData[i].W}\t{quaternionData[i].X}\t{quaternionData[i].Y}\t{quaternionData[i].Z}" +
                    $"\t{rawData[i].MagX}\t{rawData[i].MagY}\t{rawData[i].MagZ}\r\n";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }

        private static void ParseOptimizedPacket(byte[] buffer, int buffering, float accScale, float gyrScale)
        {
            Raw9Dof[] rawData = new Raw9Dof[buffering];
            Quaternion[] quaternionData = new Quaternion[buffering];

            int index = HEADER_LENGTH;
            for (int j = 0; j < quaternionData.Length; j++)
            {
                quaternionData[j] = new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, index)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, index + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, index + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, index + 6)) / 32767.0f
                };
                index += 8;
            }
            index += 8 * (10 - buffering);
            for (int j = 0; j < rawData.Length; j++)
            {
                rawData[j] = new Raw9Dof(buffer, index, accScale, gyrScale);
                index += 12;
            }

            string dataInfo = "q.w\tq.x\tq.y\tq.z" +
                "\tacc.x\tacc.y\tacc.z" +
                "\tgyro.x\tgyro.y\tgyro.z\r\n";
            for (int i = 0; i < buffering; i++)
            {
                dataInfo += $"{quaternionData[i].W}\t{quaternionData[i].X}\t{quaternionData[i].Y}\t{quaternionData[i].Z}" +
                    $"\t{rawData[i].AccX}\t{rawData[i].AccY}\t{rawData[i].AccZ}" +
                    $"\t{rawData[i].GyrX}\t{rawData[i].GyrY}\t{rawData[i].GyrZ}\r\n";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }

        private static void ParseQuatPacket(byte[] buffer, int buffering)
        {
            Quaternion[] quaternionData = new Quaternion[buffering];

            int index = HEADER_LENGTH;
            for (int j = 0; j < quaternionData.Length; j++)
            {
                quaternionData[j] = new Quaternion()
                {
                    W = BitConverter.ToSingle(buffer, index),
                    X = BitConverter.ToSingle(buffer, index + 4),
                    Y = BitConverter.ToSingle(buffer, index + 8),
                    Z = BitConverter.ToSingle(buffer, index + 12)
                };
                index += 16;
            }

            string dataInfo = "q.w\tq.x\tq.y\tq.z\r\n";
            for (int i = 0; i < buffering; i++)
            {
                dataInfo += $"{quaternionData[i].W}\t{quaternionData[i].X}\t{quaternionData[i].Y}\t{quaternionData[i].Z}\r\n";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }

        private static void ParseRawPacket(byte[] buffer, int buffering, float accScale, float gyrScale)
        {
            Raw9Dof[] rawData = new Raw9Dof[buffering];

            int index = HEADER_LENGTH;
            for (int j = 0; j < rawData.Length; j++)
            {
                rawData[j] = new Raw9Dof(buffer, index, accScale, gyrScale, true);
                index += 18;
            }

            string dataInfo = "acc.x\tacc.y\tacc.z" +
                "\tgyro.x\tgyro.y\tgyro.z" +
                "\tmag.x\tmag.y\tmag.z\r\n";
            for (int i = 0; i < buffering; i++)
            {
                dataInfo += $"{rawData[i].AccX}\t{rawData[i].AccY}\t{rawData[i].AccZ}" +
                    $"\t{rawData[i].GyrX}\t{rawData[i].GyrY}\t{rawData[i].GyrZ}" +
                    $"\t{rawData[i].MagX}\t{rawData[i].MagY}\t{rawData[i].MagZ}\r\n";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }

        private static void ParseMixedPacket(byte[] buffer, int buffering, float accScale, float gyrScale)
        {
            float[] freeAcc = new float[3];
            Quaternion quaternionData = new Quaternion();
            Raw9Dof[] rawData = new Raw9Dof[buffering];

            int index = HEADER_LENGTH;
            quaternionData = new Quaternion()
            {
                W = BitConverter.ToSingle(buffer, index),
                X = BitConverter.ToSingle(buffer, index + 4),
                Y = BitConverter.ToSingle(buffer, index + 8),
                Z = BitConverter.ToSingle(buffer, index + 12)
            };
            index += 16;

            for (int j = 0; j < freeAcc.Length; j++)
            {
                freeAcc[j] = BitConverter.ToSingle(buffer, index);
                index += 4;
            }

            for (int j = 0; j < rawData.Length; j++)
            {
                rawData[j] = new Raw9Dof(buffer, index, accScale, gyrScale, true);
                index += 18;
            }

            string dataInfo = "q.w\tq.x\tq.y\tq.z" +
                "\tfreeAcc.x\tfreeAcc.y\tfreeAcc.z" +
                "\tacc.x\tacc.y\tacc.z" +
                "\tgyro.x\tgyro.y\tgyro.z" +
                "\tmag.x\tmag.y\tmag.z\r\n";
            for (int i = 0; i < buffering; i++)
            {
                dataInfo += $"{quaternionData.W}\t{quaternionData.X}\t{quaternionData.Y}\t{quaternionData.Z}" +
                    $"\t{freeAcc[0]}\t{freeAcc[1]}\t{freeAcc[2]}" +
                    $"\t{rawData[i].AccX}\t{rawData[i].AccY}\t{rawData[i].AccZ}" +
                    $"\t{rawData[i].GyrX}\t{rawData[i].GyrY}\t{rawData[i].GyrZ}" +
                    $"\t{rawData[i].MagX}\t{rawData[i].MagY}\t{rawData[i].MagZ}\r\n";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(dataInfo);
#else
            Console.WriteLine(dataInfo);
#endif
        }
    }

    public class Raw9Dof
    {
        private const float MAG_SCALE = 0.0015f;
        public float AccX { get; set; }
        public float AccY { get; set; }
        public float AccZ { get; set; }
        public float GyrX { get; set; }
        public float GyrY { get; set; }
        public float GyrZ { get; set; }
        public float MagX { get; set; }
        public float MagY { get; set; }
        public float MagZ { get; set; }

        public Raw9Dof(byte[] buffer, int position, float accScale, float gyrScale, bool includeMag = false)
        {
            AccX = (float)BitConverter.ToInt16(buffer, position + 0) * accScale;
            AccY = (float)BitConverter.ToInt16(buffer, position + 2) * accScale;
            AccZ = (float)BitConverter.ToInt16(buffer, position + 4) * accScale;
            GyrX = (float)BitConverter.ToInt16(buffer, position + 6) * gyrScale;
            GyrY = (float)BitConverter.ToInt16(buffer, position + 8) * gyrScale;
            GyrZ = (float)BitConverter.ToInt16(buffer, position + 10) * gyrScale;
            if (includeMag)
            {
                MagX = (float)BitConverter.ToInt16(buffer, position + 12) * MAG_SCALE;
                MagY = (float)BitConverter.ToInt16(buffer, position + 14) * MAG_SCALE;
                MagZ = (float)BitConverter.ToInt16(buffer, position + 16) * MAG_SCALE;
            }
        }
        public Raw9Dof(byte[] buffer, int position)
        {
            MagX = (float)BitConverter.ToInt16(buffer, position + 0) * MAG_SCALE;
            MagY = (float)BitConverter.ToInt16(buffer, position + 2) * MAG_SCALE;
            MagZ = (float)BitConverter.ToInt16(buffer, position + 4) * MAG_SCALE;
        }
    }
}