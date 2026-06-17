using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QSenseDotNet
{
    public class Device
    {
        private enum State
        {
            DISCONNECTED,
            INITIALIZING,
            CONNECTED,
            OFFSET_COMPENSATION,
            FIELD_MAPPING,
            STREAMING
        }

        #region Fields
        private State state = State.DISCONNECTED;
        private ICommunication? parser;
        private Ble2M bleApi;
        private MemMapCtrl? ctrl;
        private bool streamingData = false;
        private float accSensitivity;
        private float gyrSensitivity;
        private float[] accScaleFactors = new float[4] { (float)0.000061, (float)0.000488, (float)0.000122, (float)0.000244 };
        private float[] gyrScaleFactors = new float[7] { (float)0.008750, (float)0.004375, (float)0.0175, 0.0f, (float)0.035, 0.0f, (float)0.07 };
        private bool synced = false;
        private uint fileAddress = 0;
        private const int downloadChunkSize = 237;
        #endregion

        /// <summary>
        /// Sensitivity of the accelerometer
        /// </summary>
        /// <returns>
        /// A <see cref="SensitivityAcc"/> object that contains the calculated sensitivity of the accelerometer.
        /// </returns>
        public SensitivityAcc AccSensitivity { get; protected set; }
        /// <summary>
        /// Device address
        /// </summary>
        /// <returns>
        /// A string containing the Serial Number.
        /// </returns>
        public string SerialNumber { get; protected set; } = "";
        public UInt64 Address { get; private set; }
        public UInt64 ID { get; private set; }
        /// <summary>
        /// Battery level
        /// </summary>
        /// <returns>
        /// Battery level in type float.
        /// </returns>
        public float Battery { get; protected set; }
        /// <summary>
        /// Connection interval in milliseconds
        /// </summary>
        /// <returns>
        /// Connection Interval in type float.
        /// </returns>
        public float ConnectionInterval { get; private set; }
        /// <summary>
        /// Number of raw samples that are buffered in each stream packet
        /// </summary>
        /// <returns>
        /// An integer representing the number of raw samples that are buffered in each stream packet.
        /// </returns>
        public int DataBuffering { get; private set; }
        /// <summary>
        /// Sensitivity of the gyroscope
        /// </summary>
        /// <returns>
        /// A <see cref="SensitivityGyr"/> object that contains the calculated sensitivity for the gyroscope.
        /// </returns>
        public SensitivityGyr GyrSensitivity { get; protected set; }
        /// <summary>
        /// True if the Device is connected
        /// </summary>
        /// <returns> <c>true</c> if the device is connected, otherwise, <c>false</c>. </returns>
        public bool IsConnected { get { return state != State.DISCONNECTED && state != State.INITIALIZING; } }
        /// <summary>
        /// True if the device is initializing
        /// </summary>
        public bool IsInitializing { get { return state == State.INITIALIZING; } }
        /// <summary>
        /// True if the device is streaming data
        /// </summary>
        public bool IsStreaming { get { return state == State.STREAMING; } }
        /// <summary>
        /// True if the device is logging data
        /// </summary>
        public bool IsLogging { get { return ctrl is null ? false : ctrl.Logging == 0x01; } }
        /// <summary>
        /// True if the magnetic field mapping has been performed before.
        /// </summary>
        /// <returns> <c>true</c> if the magnetic field mapping has been performed before, otherwise, <c>false</c>. </returns>
        public bool MagFieldMapped { get; private set; }
        /// <summary>
        /// True if the magnetic field mapping is on.
        /// </summary>
        /// <returns> <c>true</c> if the magnetic field mapping is on, otherwise, <c>false</c>. </returns>
        public bool MagneticFieldMappingOn { get; private set; }
        /// <summary>
        /// True if the offset has been compensated before.
        /// </summary>
        /// <returns> <c>true</c> if the offset has been compensated before, otherwise, <c>false</c>. </returns>
        public bool OffsetCompensated { get; private set; }
        /// <summary>
        /// True if the offset compensation is on.
        /// </summary>
        /// <returns> <c>true</c> if the offset compensation is on, otherwise, <c>false</c>. </returns>
        public bool OffsetCompensationOn { get; private set; }
        /// <summary>
        /// True if the gyroscope autocalibration is on.
        /// </summary>
        /// <returns> <c>true</c> if the gyroscope autocalibration is on, otherwise, <c>false</c>. </returns>
        public bool AutoCalibrationOn { get; private set; }
        /// <summary>
        /// Magnetometer calibration progress (percentage)
        /// </summary>
        /// <returns>
        /// An integer representing the calibration progress percentage.
        /// </returns>
        public int MagFieldMappingProgress { get; protected set; }
        /// <summary>
        /// Maximum length of data (in bytes) that can be transmitted to the Device
        /// </summary>
        /// <returns>
        /// An integer representing the maximum length of data (in bytes) that can be transmitted to the device.
        /// </returns>
        public int MaxPacketSize { get { return bleApi.MaxPacketSize; } set { bleApi.MaxPacketSize = value; } }
        /// <summary>
        ///  Motion level of the device
        /// </summary>
        /// <returns>
        /// A float representing the motion level of the device.
        /// </returns>
        public float MotionLevel { get; private set; }
        /// <summary>
        /// Device Name
        /// </summary>
        /// <returns>
        /// A string containing the device name.
        /// </returns>
        public string Name { get; private set; }
        /// <summary>
        /// Current sampling rate
        /// </summary>
        /// <returns>
        /// A <see cref="SamplingRate"/> object that contains the current sampling rate.
        /// </returns>
        public SamplingRate SamplingRate { get; private set; }

        /// <summary>
        /// Selecting Algorithms
        /// </summary>
        /// <returns>
        /// A byte representing the selected algorithm.
        /// </returns>
        public Algorithms AlgorithmSelection { get { return (Algorithms)ctrl.AlgorithmSelection; } }
        /// <summary>
        /// True if the sensor is configured as master for the timesync mode
        /// </summary>
        public bool IsTimeSyncMaster { get { return ctrl is null ? false : IsTimeSyncEnabled && (ctrl.Timesync & 0x80) != 0; } }
        /// <summary>
        /// True if the timesync mode is enabled
        /// </summary>
        public bool IsTimeSyncEnabled { get { return ctrl is null ? false : (ctrl.Timesync & 0x7F) != 0; } } 

        /// <summary>
        /// Device Version
        /// </summary>
        /// <returns>
        /// A string containing device version.
        /// </returns>
        public string Version { get; protected set; } = "";
        /// <summary>
        /// Data Mode
        /// </summary>
        /// <returns>
        /// A <see cref="DataMode"/> object that contains the data mode.
        /// </returns>
        public DataMode Mode { get; protected set; }
        /// <summary>
        /// Number of packets stored in the internal memory
        /// </summary>
        public UInt32 PacketCount { get { return ctrl is null ? 0 : ctrl.PacketCount; } }
        /// <summary>
        /// Occurs when the battery level is received.
        /// </summary>
        public event BatteryReceivedEventHandler? BatteryReceived;
        /// <summary>
        /// Occurs when the name of the device changes
        /// </summary>
        public event DeviceNameChangedEventHandler? DeviceNameChanged;
        /// <summary>
        /// Occurs when the magnetometer calibration has finished.
        /// </summary>
        public event EventHandler? MagFieldMappingDone;
        /// <summary>
        /// Occurs when the energy level is received.
        /// </summary>
        public event MotionLevelReceivedEventHandler? MotionLevelReceived;
        /// <summary>
        /// Occurs when the device state is received.
        /// </summary>
        public event StateReceivedEventHandler? StateReceived;
        /// <summary>
        /// Occurs when a stream packet is received.
        /// </summary>
        public event StreamPacketReceivedEventHandler? StreamPacketReceived;
        /// <summary>
        /// Occurs when the QSense Sensor has finished initializing.
        /// </summary>
        public event EventHandler<string>? InitializationDone;
        /// <summary>
        /// Occurs when the pin value changes and access to the QSense Sensor memory is disabled
        /// </summary>
        public event EventHandler<string>? MemoryAccesWasDisabled;
        /// <summary>
        /// Occurs when an exception is thrown during communication with the QSense Sensor, either while sending or receiving a package
        /// </summary>
        public event EventHandler? CommunicationError;
        public event StreamPacketReceivedEventHandler? DownloadPacketReceived;
        public event EventHandler? DownloadDone;

        /// <summary>
        /// Creates an object of the Device class
        /// </summary>
        public Device()
        {
            parser = null;
            bleApi = new Ble2M();
            Name = "";
            ctrl = null;
            synced = false;
        }

        /// <summary>
        /// Initializes the communication system by subscribing to BLE API events and 
        /// setting up the data parser for handling incoming data.
        /// </summary>
        /// <param name="parser">
        /// An implementation of <see cref="ICommunication"/> used to handle incoming data from the communication channel.
        /// </param>
        public void Init(ICommunication parser)
        {
            bleApi.Ble2MDataEvent += BleApi_Ble2MDataEvent;
            bleApi.Ble2MTxEvent += BleApi_Ble2MTxEvent;
            bleApi.Ble2MWriteCompletEvent += BleApi_Ble2MWriteCompletEvent;

            if (parser != null)
            {
                this.parser = parser;
                this.parser.DataReceived += BleParse_DataEvent;
            }
            bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_pin, BitConverter.GetBytes(MemMap.MEM_MAP_PIN));    //enable memory write in the Device
            state = State.INITIALIZING;
        }

        /// <summary>
        /// Reads QSense Motion Device memory if BLE is connected and not streaming.
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, then checks the current device state.
        /// If the device is connected and not actively streaming, it triggers a read operation
        /// on the memory control area using <see cref="bleApi.ReadMemory"/>.
        /// </remarks>
        public void ReadMemory()
        {
            AssertIsConnected();

            if (state > (State)1 && state < (State)5) //Connected and not streaming
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (ushort)MemMap.MEM_MAP_CTRL_SIZE);
        }

        /// <summary>
        /// Resets QSense Motion Device Bluetooth Communication.
        /// </summary>

        /// <summary>
        /// Resets the Device object  to default values. It does not reset the QSense Sensor
        /// </summary>
        public void Reset()
        {
            if (parser != null)
            {
                parser.DataReceived -= BleParse_DataEvent;
                parser = null;
            }
            bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
            bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
            bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;


            bleApi = new Ble2M();
            Name = "";
            ctrl = null;
            state = State.DISCONNECTED;
            streamingData = false;
            AccSensitivity = 0;
            SerialNumber = "";
            Battery = 0.0f;
            ConnectionInterval = 0;
            DataBuffering = 0;
            GyrSensitivity = 0;
            MagFieldMapped = false;
            MagneticFieldMappingOn = false;
            OffsetCompensated = false;
            OffsetCompensationOn = false;
            MagFieldMappingProgress = 0;
            MotionLevel = 0;
            Name = "";
            SamplingRate = 0;
            Version = "";
            Mode = 0;
        }
        /// <summary>
        /// Sets accelerometer sensitivity. 
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then sets the accelerometer sensitivity accordingly.
        /// </remarks>
        /// <param name="value"> <see cref="SensitivityAcc"/> type sensitivity of the accelerometer </param>
        public void SetAccSensitivity(SensitivityAcc value)
        {
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] = (byte)((data[0] & 0xF3) | ((byte)value << 2));
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
            accSensitivity = accScaleFactors[(int)value];
        }
        /// <summary>
        /// Fills in the Data Buffer according to the Interface version.
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then fills in the data buffer with the entered value accordingly.
        /// </remarks>
        /// <param name="value"> int type input to be stored in the data buffer.</param>
        public void SetDataBuffering(int value)
        {
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[5] = (byte)((value << 4) | data[5] & 0x0F);
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
            DataBuffering = value;
        }
        /// <summary>
        /// Sets Data Mode according to the Interface version.
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then converts the input value to bits and writes to device memory as data mode.
        /// </remarks>
        /// <param name="value"> <see cref="DataMode"/> type input determining the data mode, getting converted to bits and stored in the memory .</param>
        public void SetDataMode(DataMode value)
        {
            byte[] data = { (byte)value };
            if (bleApi != null && bleApi.Connected)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_data_mode_v2, data);
                Mode = value;
            }
        }
        /// <summary>
        /// Sets Device Name
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then converts the string type input name to bits and writes to device memory as device name.
        /// </remarks>
        /// <param name="name"> String type input name, getting converted to bits and stored in the memory as device name.</param>
        public void SetDeviceName(string name)
        {
            var data = new byte[12];
            var bytesName = Encoding.ASCII.GetBytes(name);
            for (int i = 0; i < 12; i++)
            {
                if (bytesName.Length > i) data[i] = bytesName[i];
                else data[i] = 0;
            }
            if (bleApi != null && bleApi.Connected)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_device_name_v2, data);
                Name = name;
                DeviceNameChanged?.Invoke(this, new DeviceNameChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Sets Gyroscope Sensitivity 
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then converts the <see cref="SensitivityGyr"/> type input name to byte and writes to device memory as gyroscope sensitivity.
        /// </remarks>
        /// <param name="value"> <see cref="SensitivityGyr"/> type input in terms of gyroscope sensitivity, getting converted to bits and stored in the memory .</param>
        public void SetGyrSensitivity(SensitivityGyr value)
        {
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] = (byte)((data[0] & 0x0F) | ((byte)value << 4));
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
            gyrSensitivity = gyrScaleFactors[(int)value];
        }

        /// <summary>
        /// Sets color and animation of the QSense Motion Device LED.
        /// </summary>
        /// <param name="red">Intensity of red color</param>
        /// <param name="green">Intensity of green color</param>
        /// <param name="blue">Intensity of blue color</param>
        /// <param name="animation">LED animation to display. This parameter accepts two values: 0 (blinking LED) and 1 (fixed LED)</param>
        public void SetLEDAnimation(byte red, byte green, byte blue, LEDAnimation animation)
        {
            try
            {
                if (bleApi != null && bleApi.Connected)
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_ui_state_v2, new byte[] { (byte)animation, blue, green, red });
            }
            catch (System.IO.IOException) { }
        }

        /// <summary>
        /// Sets Sampling Rate 
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, checks if the interface version
        /// is old or not, then converts the <see cref="SamplingRate"/> type input name to byte and writes to device memory as sampling rate.
        /// </remarks>
        /// <param name="value"> <see cref="SamplingRate"/> type input getting converted to bits and stored in the device memory as sampling rate.</param>
        public void SetSamplingRate(SamplingRate value)
        {
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[1] = (byte)(((byte)value & 0x0F) | data[1] & 0xF0);
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
            SamplingRate = value;
        }

        /// <summary>
        /// Sets Sensor Config
        /// </summary>
        /// <remarks>
        /// This method sets accelerometer, gyroscope sensitivity as well as sampling rate and the number of raw samples that are buffered in each stream packet
        /// </remarks>
        /// <param name="accSens"> <see cref="SensitivityAcc"/> type input getting converted to bits and stored in the device memory as accelerometer sensitivity.</param>
        /// <param name="gyrSens"> <see cref="SensitivityGyr"/> type input getting converted to bits and stored in the device memory as gyroscope sensitivity.</param>
        /// <param name="sampRate"> <see cref="SamplingRate"/> type input getting converted to bits and stored in the device memory as sampling rate.</param>
        /// <param name="buffering">Integer type input getting converted to bits and stored in the device memory as the number of raw samples that are buffered in each stream packet.</param>
        public void SetSensorConfig(SensitivityAcc accSens, SensitivityGyr gyrSens, SamplingRate sampRate, int buffering, bool autocalibrateOn = false)
        {
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] &= 0x03;
            data[0] |= (byte)((byte)accSens << 2);
            data[0] |= (byte)((byte)gyrSens << 4);
            data[0] |= autocalibrateOn ? (byte)0x80 : (byte)0x00;
            data[1] = (byte)(((byte)sampRate & 0x0F) | (buffering << 4));
            if (BitConverter.ToUInt16(data) != BitConverter.ToUInt16(ctrl.State, 4) && bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
            accSensitivity = accScaleFactors[(int)accSens];
            gyrSensitivity = gyrScaleFactors[(int)gyrSens];
            SamplingRate = sampRate;
            DataBuffering = buffering;
        }

        /// <summary>
        /// Selects Algorithm 
        /// </summary>
        /// <remarks>
        /// This method writes selected algorithm to device memory.
        /// </remarks>
        /// <param name="value"> Byte type input representing the selected algorithm.</param>
        public void SetAlgorithm(Algorithms value)
        {
            if (bleApi != null && bleApi.Connected && ctrl != null)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_algorithm_selection_v2, new byte[] { (byte)value });
                ctrl.AlgorithmSelection = (byte)value;
            }
        }

        /// <summary>
        /// Starts QSense Motion magnetic field mapping.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion magnetic field mapping. Throws an exception if an error occurs.
        /// </remarks>
        public void StartMagFieldMapping()
        {
            AssertIsConnected();

            state = State.FIELD_MAPPING;
            StateReceived -= GetMagFieldMappingState;
            StateReceived += GetMagFieldMappingState;
            try
            {
                if (bleApi.Connected && ctrl != null)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x02;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
                    MagFieldMappingProgress = 0;
                }
                streamingData = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops QSense Motion magnetic field mapping.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion magnetic field mapping. Throws an exception if an error occurs.
        /// </remarks>
        public void StopMagFieldMapping()
        {
            AssertIsConnected();

            state = State.CONNECTED;
            StateReceived -= GetMagFieldMappingState;
            try
            {
                if (bleApi.Connected && ctrl != null)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] &= 0xFD;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
                    MagFieldMappingProgress = 0;
                }
                streamingData = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Starts QSense Motion offset compensation.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion offset compensation. Throws an exception if an error occurs.
        /// </remarks>
        public void StartOffsetCompensation()
        {
            AssertIsConnected();

            state = State.OFFSET_COMPENSATION;
            try
            {
                if (bleApi.Connected && ctrl != null)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x01;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
                }
                streamingData = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Enables the gyroscope autocalibration.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts the gyroscope autocalibration. Throws an exception if an error occurs.
        /// </remarks>
        public void EnableAutocalibration()
        {
            AssertIsConnected();

            try
            {
                if (bleApi.Connected && ctrl != null)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x80;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Disables the gyroscope autocalibration.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops the gyroscope autocalibration. Throws an exception if an error occurs.
        /// </remarks>
        public void DisableAutocalibration()
        {
            AssertIsConnected();

            try
            {
                if (bleApi.Connected && ctrl != null)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] &= 0x7F;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state_v2, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Starts Synchronization. 
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion synchronization.
        /// </remarks>
        public void StartSync(byte networkKey, bool isMaster = false)
        {
            AssertIsConnected();
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { (byte)(0x7F & networkKey) };
                if (isMaster) data[0] |= 0x80;
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_timesync_v2, data);
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_timesync_ui_v2, new byte[] { 0x2 });
            }
        }
        /// <summary>
        /// Stops Synchronization. 
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion synchronization.
        /// </remarks>
        public void StopSync()
        {
            AssertIsConnected();
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { 0x00 };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_timesync_v2, data);
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_timesync_ui_v2, new byte[] { 0x0 });
            }
        }

        /// <summary>
        /// Sets Annotation. 
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and sets annotation. 
        /// </remarks>
        public void SetAnnotation(byte val)
        {
            AssertIsConnected();
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { val };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_annotation_v2, data);
            }
        }

        /// <summary>
        /// Starts QSense Motion Device data streaming.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion Device data streaming. Throws an exception if an error occurs.
        /// </remarks>
        public void StartStreaming()
        {
            AssertIsConnected();

            state = State.STREAMING;
            try
            {
                if (bleApi.Connected)
                {
                    bleApi.StreamMemory(MemMap.MEM_MAP_CONF_ADDR, (UInt16)MemMap.MEM_MAP_CONF_SIZE_V2);
                }
                streamingData = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops QSense Motion Device data streaming.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion Device data streaming. Throws an exception if an error occurs.
        /// </remarks>
        public void StopStreaming()
        {
            AssertIsConnected();

            state = State.CONNECTED;
            try
            {
                if (bleApi.Connected)
                    bleApi.Abort();
                streamingData = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Starts QSense Motion Device data logging.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion Device data logging. Throws an exception if an error occurs.
        /// </remarks>
        public void StartLogging()
        {
            AssertIsConnected();

            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[1] { 0x01 };
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_logging_v2, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops QSense Motion Device data logging.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion Device data logging. Throws an exception if an error occurs.
        /// </remarks>
        public void StopLogging()
        {
            AssertIsConnected();

            try
            {
                byte[] data = new byte[1] { 0x00 };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_logging_v2, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void EraseFile()
        {             
            AssertIsConnected();

            try
            {
                if (bleApi.Connected)
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_erase_file_v2, new byte[] { 0x01 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void StartDownload()
        {
            AssertIsConnected();
            try
            {
                fileAddress = 0;
                bleApi.ReadMemory(MemMap.MEM_MAP_FILE_ADDR, downloadChunkSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void AssertIsConnected()
        {
            if (state == State.DISCONNECTED)
                throw new Exception("The QSense Motion Device is not connected. Make sure that you call Device.Init() before calling this method.");
        }

        private void BleParse_DataEvent(object sender, DataReceivedEventArgs e)
        {
            try
            {
                byte[] buffer = Utilities.HexToByteArray(e.Data);            
                bleApi.Ble2MRxEvent(buffer);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
        }

        #region Private methods
        private void BleApi_Ble2MDataEvent(object sender, Ble2MDataEventArgs e)
        {
            if (e.Address == MemMap.MEM_MAP_CTRL_ADDR && e.Data.Length == MemMap.MEM_MAP_CTRL_SIZE)
            {
                ctrl = new MemMapCtrl(e.Data);
                if (ctrl != null && bleApi.Connected)
                {
                    ID = ctrl.Id;
                    Address = ctrl.Address;
                    SerialNumber = (ctrl.Address.ToString("x") + "-" + ctrl.Id.ToString("x").Substring(0, 4)).ToUpper(); 
                    var version = BitConverter.GetBytes(ctrl.Version);
                    Version = $"v{version[2]}.{version[1]}.{version[0]}";
                    string oldnName = Name;
                    Name = ctrl.Name.Split('\0')[0];
                    float oldBatt = Battery;
                    Battery = ctrl.Battery;
                    float oldMotionLevel = MotionLevel;
                    MotionLevel = ctrl.MotionLevel;
                    Mode = ctrl.DataMode;
                    StateReceivedEventArgs newState = ExtractState(ctrl.State);
                    if (state == State.OFFSET_COMPENSATION && newState.IsOffsetCompensated)
                        state = State.CONNECTED;
                    StateUpdate();
                    if (oldnName != Name)
                        DeviceNameChanged?.Invoke(this, new DeviceNameChangedEventArgs(Name));
                    if (oldBatt != Battery) 
                        BatteryReceived?.Invoke(this, new BatteryReceivedEventArgs(ctrl.Battery));
                    if (oldMotionLevel != MotionLevel)
                        MotionLevelReceived?.Invoke(this, new MotionLevelReceivedEventArgs(ctrl.MotionLevel));
                    if (state == State.INITIALIZING)
                    {
                        uint unixtime = (uint)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        bleApi?.WriteMemory(MemMap.MEM_MAP_ADDR_time_v2, BitConverter.GetBytes(unixtime));
                    }

                    if (ctrl.Pin != MemMap.MEM_MAP_PIN) MemoryAccesWasDisabled?.Invoke(this, SerialNumber);
                }
            }
            else if (e.Address == MemMap.MEM_MAP_CONF_ADDR && streamingData)
            {
                if (ctrl == null)
                    return;
                var data = new MemMapData();
                data.AddData(e.Data, accSensitivity, gyrSensitivity);
                if (data.Packet == null)
                    return;
                var serialNumber = SerialNumber;
                var acc = new List<float[]>();
                var gyro = new List<float[]>();
                var mag = new List<float[]>();
                foreach (var sample in data.Packet.Raw)
                {
                    if (data.Packet.DataMode != DataMode.QuatMag)
                    {
                        acc.Add(new float[] { sample.AccX, sample.AccY, sample.AccZ });
                        gyro.Add(new float[] { sample.GyrX, sample.GyrY, sample.GyrZ });
                    }
                    if (data.Packet.DataMode != DataMode.Optimized)
                    {
                        mag.Add(new float[] { sample.MagX, sample.MagY, sample.MagZ });
                    }
                }
                var quat = data.Packet.Quaternion.ToList();
                var dataMode = data.Packet.DataMode;
                var freeAcc = data.Packet.FreeAcceleration;
                var interference = (MagInterference)data.Packet.Interference;
                StreamPacketReceived?.Invoke(this, new StreamPacketReceivedEventArgs(
                    dataMode,
                    data.Packet.Buffering,
                    serialNumber,
                    acc,
                    gyro,
                    mag,
                    quat,
                    freeAcc,
                    interference,
                    data.Packet.Seconds,
                    data.Packet.Milliseconds,
                    data.Packet.Battery,
                    data.Packet.Annotation
                ));
            }
            else if (e.Address >= MemMap.MEM_MAP_FILE_ADDR && e.Data.Length == downloadChunkSize)
            {
                if (ctrl == null)
                    return;
                if (e.Data.All(b => b == 0))
                {
                    fileAddress = 0;
                    EraseFile();
                    ReadMemory();
                    DownloadDone?.Invoke(this, new EventArgs());
                }
                else
                {
                    StreamPacket packet = new StreamPacket(e.Data, 0, 0);
                    var serialNumber = SerialNumber;
                    var acc = new List<float[]>();
                    var gyro = new List<float[]>();
                    var mag = new List<float[]>();
                    foreach (var sample in packet.Raw)
                    {
                        if (packet.DataMode != DataMode.QuatMag)
                        {
                            acc.Add(new float[] { sample.AccX, sample.AccY, sample.AccZ });
                            gyro.Add(new float[] { sample.GyrX, sample.GyrY, sample.GyrZ });
                        }
                        if (packet.DataMode != DataMode.Optimized)
                        {
                            mag.Add(new float[] { sample.MagX, sample.MagY, sample.MagZ });
                        }
                    }
                    var quat = packet.Quaternion.ToList();
                    var dataMode = packet.DataMode;
                    var freeAcc = packet.FreeAcceleration;
                    var interference = (MagInterference)packet.Interference;
                    DownloadPacketReceived?.Invoke(this, new StreamPacketReceivedEventArgs(
                        dataMode,
                        packet.Buffering,
                        serialNumber,
                        acc,
                        gyro,
                        mag,
                        quat,
                        freeAcc,
                        interference,
                        packet.Seconds,
                        packet.Milliseconds,
                        packet.Battery,
                        packet.Annotation
                    ));
                    fileAddress += downloadChunkSize;
                    bleApi.ReadMemory(MemMap.MEM_MAP_FILE_ADDR + fileAddress, downloadChunkSize);
                }
            }
        }

        private void BleApi_Ble2MTxEvent(object sender, Ble2MTxEventArgs e)
        {
            byte[] message = new byte[e.Packet.Length];
            Array.Copy(e.Packet, 0, message, 0, e.Packet.Length);
            string s = "";
            foreach (byte b in message) s += b.ToString("X2");
            try
            {
                parser?.Write(s);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
        }

        private void BleApi_Ble2MWriteCompletEvent(object sender, Ble2MDataEventArgs e)
        {
            if (e.Address == MemMap.MEM_MAP_ADDR_pin)
            {
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (UInt16)MemMap.MEM_MAP_CTRL_SIZE);
            }
            else if (e.Address == MemMap.MEM_MAP_ADDR_time_v2 && e.Data.Length == 4)
            {
                if (state == State.INITIALIZING)
                {
                    InitializationDone?.Invoke(this, SerialNumber);
                    state = State.CONNECTED;
                }
            }
        }

        private void GetMagFieldMappingState(object sender, StateReceivedEventArgs e)
        {
            MagFieldMappingProgress = e.MagFieldMappingProgress;
            if (MagFieldMappingProgress == 100)
            {
                state = State.CONNECTED;
                MagFieldMappingDone?.Invoke(this, new EventArgs());
            }
        }

        private StateReceivedEventArgs ExtractState(byte[] state)
        {
            OffsetCompensated = state[0] == 1;
            MagFieldMapped = state[1] == 1;
            var progress = state[2];
            MagFieldMappingProgress = progress * 10;
            ConnectionInterval = state[3] * 1.25f;
            OffsetCompensationOn = (state[4] & 0x01) == 1;
            MagneticFieldMappingOn = (state[4] & 0x02) == 1;
            AccSensitivity = (SensitivityAcc)((state[4] & 0x0C) >> 2);
            GyrSensitivity = (SensitivityGyr)((state[4] & 0x70) >> 4);
            AutoCalibrationOn = (state[4] & 0x80) == 0x80;
            SamplingRate = (SamplingRate)(state[5] & 0x0F);
            DataBuffering = state[5] >> 4;

            StateReceivedEventArgs stateArgs = new StateReceivedEventArgs(OffsetCompensationOn, OffsetCompensated, MagneticFieldMappingOn,
                MagFieldMapped, MagFieldMappingProgress, AccSensitivity, GyrSensitivity, AutoCalibrationOn, SamplingRate,
                DataBuffering, ConnectionInterval);
            return stateArgs;
        }

        private void StateUpdate()
        {
            if (ctrl == null) return;

            StateReceivedEventArgs state = ExtractState(ctrl.State);

            accSensitivity = accScaleFactors[(int)state.AccSensitivity];
            gyrSensitivity = gyrScaleFactors[(int)state.GyroSensitivity];
            StateReceived?.Invoke(this, state);
        }

        ~Device()
        {
            try
            {
                bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
                bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
                bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;

            }
            catch (NullReferenceException) { }
        }        
        #endregion
    }
}