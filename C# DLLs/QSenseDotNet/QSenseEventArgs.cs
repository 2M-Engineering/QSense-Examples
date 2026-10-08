using System;

namespace QSenseDotNet
{
    /// <summary>
    /// Provides data for the StateReceivedEvent
    /// </summary>
    public class StateReceivedEventArgs : EventArgs
    {
        internal StateReceivedEventArgs(bool isOffsetCompensationOn, bool isOffsetCompensated, bool isMagFieldMappingOn,
            bool isMagFieldMapped, Int32 magFieldMappingProgress, SensitivityAcc accSensitivity,
            SensitivityGyr gyroSensitivity, bool autocalOn, SamplingRate samplingRate, Buffering dataBuffering, float connectionInterval)
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
        public Buffering DataBuffering { get; }
        public float ConnectionInterval { get; }
    }
}