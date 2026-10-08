using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace QSenseDotNet
{
    public class StreamPacket
    {
        public const int PacketSize = 237;
        private float[] accScaleFactors = new float[4] { (float)0.000061, (float)0.000488, (float)0.000122, (float)0.000244 };
        private float[] gyrScaleFactors = new float[7] { (float)0.008750, (float)0.004375, (float)0.0175, 0.0f, (float)0.035, 0.0f, (float)0.07 };
        private Dictionary<Buffering, int> enumToInt = new Dictionary<Buffering, int>() { { QSenseDotNet.Buffering._1, 1}, { QSenseDotNet.Buffering._2, 2}, { QSenseDotNet.Buffering._4, 4}, { QSenseDotNet.Buffering._5, 5}, { QSenseDotNet.Buffering._8, 8}, { QSenseDotNet.Buffering._10, 10}, { QSenseDotNet.Buffering._20, 20}, { QSenseDotNet.Buffering._40, 40 }
        };
        public DataMode DataMode { get; set; }
        public byte Interference { get; set; }
        public byte Buffering { get; set; }
        public DateTime Timestamp { get { return new DateTime(1970, 1, 1).AddSeconds(Seconds).AddMilliseconds(Milliseconds); } }
        internal UInt32 Seconds { get; set; }
        internal float Milliseconds { get; set; }
        public byte Battery { get; set; }
        public byte Annotation { get; set; }
        internal Raw9Dof[] Raw { get; set; }
        public float[][] Acc { get { return Raw.Length == 0 ? new float[0][] : Raw[0].Acc.Length == 0 ? new float[0][] : Raw.Select(x => new float[] { x.Acc[0], x.Acc[1], x.Acc[2] }).ToArray(); } }
        public float[][] Gyr { get { return Raw.Length == 0 ? new float[0][] : Raw[0].Gyr.Length == 0 ? new float[0][] : Raw.Select(x => new float[] { x.Gyr[0], x.Gyr[1], x.Gyr[2] }).ToArray(); } }
        public float[][] Mag { get { return Raw.Length == 0 ? new float[0][] : Raw[0].Mag.Length == 0 ? new float[0][] : Raw.Select(x => new float[] { x.Mag[0], x.Mag[1], x.Mag[2] }).ToArray(); } }
        public Quaternion[] Quaternions { get; set; }
        public float[] FreeAcceleration { get; set; }
        public bool SyncOk { get; set; }

        private StreamPacket() { }
        internal StreamPacket(byte[] buffer)
        {
            switch (buffer.Length)
            {
                case PacketSize:
                    ParseV2Packet(buffer);
                    break;
                default:
                    throw new Exception("Wrong PacketSize");
            }
        }

        internal StreamPacket[] SplitPacket(int whitelistCount)
        {
            var packets = new StreamPacket[whitelistCount];
            for (int i = 0; i < whitelistCount; i++)
            {
                packets[i] = new StreamPacket();
                packets[i].DataMode = this.DataMode;
                packets[i].Buffering = this.Buffering;
                packets[i].Seconds = this.Seconds;
                packets[i].Milliseconds = this.Milliseconds;
                packets[i].Interference = this.Interference;
                packets[i].Battery = this.Battery;
                packets[i].Annotation = this.Annotation;
                packets[i].SyncOk = this.SyncOk;
                switch (DataMode)
                {
                    case DataMode.Raw:
                        packets[i].Raw = new Raw9Dof[1] { Raw[i] };
                        packets[i].Quaternions = new Quaternion[0];
                        packets[i].FreeAcceleration = new float[0];
                        break;
                    case DataMode.Gyro100Acc100:
                        packets[i].Raw = new Raw9Dof[2] { Raw[i], Raw[i + whitelistCount] };
                        packets[i].Quaternions = new Quaternion[0];
                        packets[i].FreeAcceleration = new float[0];
                        break;
                    case DataMode.Quat100Acc50:
                    case DataMode.Quat100Mag50:
                        packets[i].Raw = new Raw9Dof[1] { Raw[i] };
                        packets[i].Quaternions = new Quaternion[2] { Quaternions[i], Quaternions[i + whitelistCount] };
                        packets[i].FreeAcceleration = new float[0];
                        break;
                    default: throw new Exception("Wrong data mode");
                }
            }
            return packets;
        }

        private void ParseV2Packet(byte[] buffer)
        {
            DataMode = (DataMode)(buffer[0] & 0x0F);
            Buffering = (byte)(buffer[0] >> 4);
            Seconds = BitConverter.ToUInt32(buffer, 1);
            Milliseconds = BitConverter.ToUInt16(buffer, 5) * 1.25f;
            if (DataMode == 0 && Buffering == 0 && Seconds == 0)
            {
                Raw = new Raw9Dof[0];
                Quaternions = new Quaternion[0];
                FreeAcceleration = new float[0];
                return;
            }
            Interference = (byte)(buffer[7] & 0x07);
            Battery = (byte)(buffer[7] >> 3);
            Annotation = buffer[8];
            SyncOk = (buffer[9] & 0x01) == 1;
            float accScale = accScaleFactors[(buffer[9] & 0x30) >> 4];
            float gyrScale = gyrScaleFactors[(buffer[9] & 0x0E) >> 1];

            switch (DataMode)
            {
                case DataMode.Mixed:
                    ParseMixedPacket(buffer, accScale, gyrScale);
                    break;
                case DataMode.Raw:
                    ParseRawPacket(buffer, accScale, gyrScale);
                    break;
                case DataMode.Quat:
                    ParseQuatPacket(buffer);
                    break;
                case DataMode.Optimized:
                    ParseOptimizedPacket(buffer, accScale, gyrScale);
                    break;
                case DataMode.QuatMag:
                    ParseQuatMagPacket(buffer);
                    break;
                case DataMode.Quat100Acc50:
                    ParseQuat100Acc50Packet(buffer, accScale, gyrScale);
                    break;
                case DataMode.Quat100Mag50:
                    ParseQuat100Mag50Packet(buffer, accScale, gyrScale);
                    break;
                case DataMode.Gyro100Acc100:
                    ParseGyro100Acc100Packet(buffer, accScale, gyrScale);
                    break;
                default:
                    break;
            }
        }

        private void ParseGyro100Acc100Packet(byte[] buffer, float accScale, float gyrScale)
        {
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[0];
            int buffering = enumToInt[(Buffering)Buffering];
            byte[] Compressed6DofBuffer = new byte[225 + 2];
            Array.Copy(buffer, 10, Compressed6DofBuffer, 0, 225);
            Raw = new Raw9Dof[buffering];
            for (int i = 0; i < 20; i++)
            {
                Raw[i] = new Raw9Dof();
                Raw[i].Acc = new float[3];
                Raw[i].Gyr = new float[3];
                Raw[i].Mag = new float[0];
                Raw[i].Acc[0] = ReadFromPackedBuffer(15, i * 6 + 0, ref Compressed6DofBuffer) * accScale * 2;
                Raw[i].Acc[1] = ReadFromPackedBuffer(15, i * 6 + 1, ref Compressed6DofBuffer) * accScale * 2;
                Raw[i].Acc[2] = ReadFromPackedBuffer(15, i * 6 + 2, ref Compressed6DofBuffer) * accScale * 2;
                Raw[i].Gyr[0] = ReadFromPackedBuffer(15, i * 6 + 3, ref Compressed6DofBuffer) * gyrScale * 2;
                Raw[i].Gyr[1] = ReadFromPackedBuffer(15, i * 6 + 4, ref Compressed6DofBuffer) * gyrScale * 2;
                Raw[i].Gyr[2] = ReadFromPackedBuffer(15, i * 6 + 5, ref Compressed6DofBuffer) * gyrScale * 2;
            }
        }
        public static Int16 ReadFromPackedBuffer(int dataBits, int index, ref byte[] buffer)
        {
            Int16 result = 0;

            int byteIndex = index * dataBits / 8;
            int bitIndex = (index * dataBits) % 8;
            int missingMsbBits = 16 - dataBits;
            uint negativePrefix = 0xffff - ((((uint)1) << dataBits) - 1);

            uint bitMask = (((uint)1 << dataBits) - 1) << bitIndex;
            uint value = BitConverter.ToUInt32(buffer, byteIndex) & bitMask;

            value = value >> bitIndex;
            value = value << missingMsbBits;
            if ((value & 0x8000) == 0)
            {
                value = value >> missingMsbBits;
            }
            else
            {
                value = (value >> missingMsbBits) + negativePrefix;
            }

            byte[] valueBuffer = BitConverter.GetBytes(value);
            result = BitConverter.ToInt16(valueBuffer, 0);

            return result;
        }

        private void ParseQuat100Mag50Packet(byte[] buffer, float accScale, float gyrScale)
        {
            int buffering = enumToInt[(Buffering)Buffering];
            Raw = new Raw9Dof[buffering / 2];
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = Quaternion.Normalize(new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                });
                i += 8;
            }
            i += 8 * (20 - buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale, false, false, true);
                i += 6;
            }
        }

        private void ParseQuat100Acc50Packet(byte[] buffer, float accScale, float gyrScale)
        {
            int buffering = enumToInt[(Buffering)Buffering];
            Raw = new Raw9Dof[buffering / 2];
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = Quaternion.Normalize(new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                });
                i += 8;
            }
            i += 8 * (20 - buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale, true, false, false);
                i += 6;
            }
        }

        private void ParseQuatMagPacket(byte[] buffer)
        {
            Raw = new Raw9Dof[Buffering];
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = Quaternion.Normalize(new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                });
                i += 8;
            }
            i += 8 * (10 - Buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, 0, 0, false, false, true);
                i += 6;
            }
        }

        private void ParseOptimizedPacket(byte[] buffer, float accScale, float gyrScale)
        {
            Raw = new Raw9Dof[Buffering];
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = Quaternion.Normalize(new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                });
                i += 8;
            }
            i += 8 * (10 - Buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale, true, true, false);
                i += 12;
            }
        }

        private void ParseQuatPacket(byte[] buffer)
        {
            Raw = new Raw9Dof[0];
            FreeAcceleration = new float[0];
            Quaternions = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = new Quaternion()
                {
                    W = BitConverter.ToSingle(buffer, i),
                    X = BitConverter.ToSingle(buffer, i + 4),
                    Y = BitConverter.ToSingle(buffer, i + 8),
                    Z = BitConverter.ToSingle(buffer, i + 12)
                };
                i += 16;
            }
        }

        private void ParseRawPacket(byte[] buffer, float accScale, float gyrScale)
        {
            FreeAcceleration = new float[0];
            Raw = new Raw9Dof[Buffering];
            Quaternions = new Quaternion[0];

            int i = 10;
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale);
                i += 18;
            }
        }

        private void ParseMixedPacket(byte[] buffer, float accScale, float gyrScale)
        {
            FreeAcceleration = new float[3];
            Quaternions = new Quaternion[1];
            Raw = new Raw9Dof[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternions.Length; j++)
            {
                Quaternions[j] = new Quaternion()
                {
                    W = BitConverter.ToSingle(buffer, i),
                    X = BitConverter.ToSingle(buffer, i + 4),
                    Y = BitConverter.ToSingle(buffer, i + 8),
                    Z = BitConverter.ToSingle(buffer, i + 12)
                };
                i += 16;
            }

            for (int j = 0; j < FreeAcceleration.Length; j++)
            {
                FreeAcceleration[j] = BitConverter.ToSingle(buffer, i);
                i += 4;
            }

            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale);
                i += 18;
            }
        }
    }

    public class Raw9Dof
    {
        private const float MAG_SCALE = 0.0015f;
        public float[] Acc { get; set; }
        public float[] Gyr { get; set; }
        public float[] Mag { get; set; }

        internal Raw9Dof()
        { }
        internal Raw9Dof(byte[] buffer, int position, float accScale, float gyrScale, bool includeAcc = true, bool includeGyr = true, bool includeMag = true)
        {
            Acc = new float[includeAcc ? 3 : 0];
            Gyr = new float[includeGyr ? 3 : 0];
            Mag = new float[includeMag ? 3 : 0];
            if (includeAcc)
            {
                Acc[0] = (float)BitConverter.ToInt16(buffer, position + 0) * accScale;
                Acc[1] = (float)BitConverter.ToInt16(buffer, position + 2) * accScale;
                Acc[2] = (float)BitConverter.ToInt16(buffer, position + 4) * accScale;
            }
            if (includeGyr)
            {
                Gyr[0] = (float)BitConverter.ToInt16(buffer, position + 0 + 2 * Acc.Length) * gyrScale;
                Gyr[1] = (float)BitConverter.ToInt16(buffer, position + 2 + 2 * Acc.Length) * gyrScale;
                Gyr[2] = (float)BitConverter.ToInt16(buffer, position + 4 + 2 * Acc.Length) * gyrScale;
            }
            if (includeMag)
            {
                Mag[0] = (float)BitConverter.ToInt16(buffer, position + 0 + 2 * (Acc.Length + Gyr.Length)) * MAG_SCALE;
                Mag[1] = (float)BitConverter.ToInt16(buffer, position + 2 + 2 * (Acc.Length + Gyr.Length)) * MAG_SCALE;
                Mag[2] = (float)BitConverter.ToInt16(buffer, position + 4 + 2 * (Acc.Length + Gyr.Length)) * MAG_SCALE;
            }
        }
    }
}