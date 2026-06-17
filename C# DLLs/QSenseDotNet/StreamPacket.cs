using System;
using System.Numerics;

namespace QSenseDotNet
{
    public class StreamPacket
    {
        private const int PacketSize_V1 = 213;
        private const int PacketSize_V2 = 237;
        private float[] accScaleFactors = new float[4] { (float)0.000061, (float)0.000488, (float)0.000122, (float)0.000244 };
        private float[] gyrScaleFactors = new float[7] { (float)0.008750, (float)0.004375, (float)0.0175, 0.0f, (float)0.035, 0.0f, (float)0.07 };
        public DataMode DataMode { get; private set; }
        public byte Interference { get; private set; }
        public byte Buffering { get; private set; }
        public UInt32 Seconds { get; private set; }
        public float Milliseconds { get; private set; }
        public byte Battery { get; private set; }
        public byte Annotation { get; private set; }
        public Raw9Dof[] Raw { get; private set; } = new Raw9Dof[0];
        public Quaternion[] Quaternion { get; private set; } = new Quaternion[0];
        public float[] FreeAcceleration { get; private set; } = new float[0];
        public bool SyncOk { get; private set; }

        public StreamPacket(byte[] buffer, float accScale, float gyrScale)
        {
            switch (buffer.Length)
            {
                case PacketSize_V1:
                    throw new Exception("Unsupported");
                case PacketSize_V2:
                    ParseV2Packet(buffer); 
                    break;
                default:
                    throw new Exception("Wrong PacketSize");
            }
        }

        private void ParseV2Packet(byte[] buffer)
        {
            DataMode = (DataMode)(buffer[0] & 0x0F);
            Buffering = (byte)(buffer[0] >> 4);
            Seconds = BitConverter.ToUInt32(buffer, 1);
            Milliseconds = BitConverter.ToUInt16(buffer, 5) * 1.25f;
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
                default:
                    break;
            }
        }

        private void ParseQuatMagPacket(byte[] buffer)
        {
            Raw = new Raw9Dof[Buffering];
            FreeAcceleration = new float[0];
            Quaternion = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternion.Length; j++)
            {
                Quaternion[j] = new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                };
                i += 8;
            }
            i += 8 * (10 - Buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i);
                i += 6;
            }
        }

        private void ParseOptimizedPacket(byte[] buffer, float accScale, float gyrScale)
        {
            Raw = new Raw9Dof[Buffering];
            FreeAcceleration = new float[0];
            Quaternion = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternion.Length; j++)
            {
                Quaternion[j] = new Quaternion()
                {
                    W = ((float)BitConverter.ToInt16(buffer, i)) / 32767.0f,
                    X = ((float)BitConverter.ToInt16(buffer, i + 2)) / 32767.0f,
                    Y = ((float)BitConverter.ToInt16(buffer, i + 4)) / 32767.0f,
                    Z = ((float)BitConverter.ToInt16(buffer, i + 6)) / 32767.0f
                };
                i += 8;
            }
            i += 8 * (10 - Buffering);
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale);
                i += 12;
            }
        }

        private void ParseQuatPacket(byte[] buffer)
        {
            Raw = new Raw9Dof[0];
            FreeAcceleration = new float[0];
            Quaternion = new Quaternion[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternion.Length; j++)
            {
                Quaternion[j] = new Quaternion()
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
            Quaternion = new Quaternion[0];

            int i = 10;
            for (int j = 0; j < Raw.Length; j++)
            {
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale, true);
                i += 18;
            }
        }

        private void ParseMixedPacket(byte[] buffer, float accScale, float gyrScale)
        {
            FreeAcceleration = new float[3];
            Quaternion = new Quaternion[1];
            Raw = new Raw9Dof[Buffering];

            int i = 10;
            for (int j = 0; j < Quaternion.Length; j++)
            {
                Quaternion[j] = new Quaternion()
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
                Raw[j] = new Raw9Dof(buffer, i, accScale, gyrScale, true);
                i += 18;
            }
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

        internal Raw9Dof(byte[] buffer, int position, float accScale, float gyrScale, bool includeMag = false)
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
        internal Raw9Dof(byte[] buffer, int position)
        {
            MagX = (float)BitConverter.ToInt16(buffer, position + 0) * MAG_SCALE;
            MagY = (float)BitConverter.ToInt16(buffer, position + 2) * MAG_SCALE;
            MagZ = (float)BitConverter.ToInt16(buffer, position + 4) * MAG_SCALE;
        }
    }
}