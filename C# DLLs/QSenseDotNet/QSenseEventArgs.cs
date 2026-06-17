using System;
using System.Collections.Generic;
using System.Numerics;

namespace QSenseDotNet
{

    /// <summary>
    /// Provides data for the BatteryReceivedEvent
    /// </summary>
    public class BatteryReceivedEventArgs : EventArgs
    {
        internal BatteryReceivedEventArgs(float battery)
        {
            Battery = battery;
        }

        public float Battery { get; set; }
    }

    
    /// <summary>
    /// Provides data for the DeviceNameChangedEvent
    /// </summary>
    public class DeviceNameChangedEventArgs : EventArgs
    {
        internal DeviceNameChangedEventArgs(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    /// <summary>
    /// Provides data for the EnergyReceivedEvent
    /// </summary>
    public class MotionLevelReceivedEventArgs : EventArgs
    {
        internal MotionLevelReceivedEventArgs(float motionLevel)
        {
            MotionLevel = motionLevel;
        }

        public float MotionLevel { get; set; }
    }

    /// <summary>
    /// Provides data for the StateReceivedEvent
    /// </summary>
    public class StateReceivedEventArgs : EventArgs
    {
        internal StateReceivedEventArgs(bool isOffsetCompensationOn, bool isOffsetCompensated, bool isMagFieldMappingOn, 
            bool isMagFieldMapped, Int32 magFieldMappingProgress, SensitivityAcc accSensitivity, 
            SensitivityGyr gyroSensitivity, bool autocalOn, SamplingRate samplingRate, Int32 dataBuffering, float connectionInterval)
        {
            IsOffsetCompensationOn = isOffsetCompensationOn;
            IsOffsetCompensated = isOffsetCompensated;
            IsMagFieldMappingOn = isMagFieldMappingOn;
            IsMagFieldMapped = isMagFieldMapped;
            MagFieldMappingProgress = magFieldMappingProgress;
            AccSensitivity = accSensitivity;
            GyroSensitivity = gyroSensitivity;
            IsAutoCalibrationOn = autocalOn;
            SamplingRate = samplingRate;
            DataBuffering = dataBuffering;
            ConnectionInterval = connectionInterval;
        }

        public bool IsOffsetCompensationOn { get; }
        public bool IsOffsetCompensated { get; }
        public bool IsMagFieldMappingOn { get; }
        public bool IsMagFieldMapped { get; }
        public Int32 MagFieldMappingProgress { get; }
        public MagFieldMapQuality MagFieldMapQuality { get; }
        public SensitivityAcc AccSensitivity { get; }
        public SensitivityGyr GyroSensitivity { get; }
        public bool IsAutoCalibrationOn { get; }
        public SamplingRate SamplingRate { get; }
        public Int32 DataBuffering { get; }
        public float ConnectionInterval { get; }
    }

    /// <summary>
    /// Provides data for the DataPacketReadyEvent
    /// </summary>
    public class StreamPacketReceivedEventArgs : EventArgs
    {
        internal StreamPacketReceivedEventArgs(DataMode datamode, int buffering, string serialNumber, List<float[]> acc, List<float[]> gyro, List<float[]> mag,
            List<Quaternion> quaternion, float[] freeAcc, MagInterference interference, UInt32 seconds, float milliseconds, byte battery, byte marker)
        {
            DataMode = datamode;
            Buffering = buffering;  
            SerialNumber = serialNumber;
            Acc = acc;
            Gyro = gyro;
            Mag = mag;
            Quaternion = quaternion;
            FreeAcc = freeAcc;
            Interference = interference;
            Battery = battery;
            Marker = marker;
            Timestamp =  new DateTime(1970, 1, 1).AddSeconds(seconds).AddMilliseconds(milliseconds);
        }

        public DataMode DataMode { get; }
        public int Buffering { get; }
        public string SerialNumber { get; }
        public List<float[]> Acc { get; } = new List<float[]>() { new float[0] };
        public List<float[]> Gyro { get; } = new List<float[]>() { new float[0] };
        public List<float[]> Mag { get; } = new List<float[]>() { new float[0] };
        public List<Quaternion> Quaternion { get; }
        public float[] FreeAcc { get; } = new float[3];
        public MagInterference Interference { get; }
        public DateTime Timestamp { get; }
        public byte Battery { get; }
        public byte Marker { get; }
    }

}