using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QSenseDotNet
{
    public abstract class Device
    {
        protected enum State
        {
            DISCONNECTED,
            INITIALIZING,
            CONNECTED,
            OFFSET_COMPENSATION,
            FIELD_MAPPING,
            STREAMING
        }

        #region Fields
        protected State state = State.DISCONNECTED;
        protected ICommunication? parser;
        internal Ble2M bleApi;
        internal MemMapCtrl? ctrl;
        protected bool streamingData = false;
        protected float accSensitivity;
        protected float gyrSensitivity;
        protected float[] accScaleFactors = new float[4] { (float)0.000061, (float)0.000488, (float)0.000122, (float)0.000244 };
        protected float[] gyrScaleFactors = new float[7] { (float)0.008750, (float)0.004375, (float)0.0175, 0.0f, (float)0.035, 0.0f, (float)0.07 };
        protected bool synced = false;
        protected uint fileAddress = 0;
        protected const int downloadChunkSize = 237;
        #endregion

        public DeviceType Type { get { return ctrl is null ? DeviceType.Sensor : ctrl.WhoAmI == MemMap.MEM_MAP_WHOAMI ? DeviceType.Sensor : DeviceType.Hub; } }
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
        public string SerialNumber { get; protected set; }
        public UInt64 Address { get; protected set; }
        public UInt64 ID { get; protected set; }
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
        public float ConnectionInterval { get; protected set; }
        /// <summary>
        /// Number of raw samples that are buffered in each stream packet
        /// </summary>
        /// <returns>
        /// An integer representing the number of raw samples that are buffered in each stream packet.
        /// </returns>
        public Buffering DataBuffering { get; protected set; }
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
        public bool IsPeripheral { get; protected set; }

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
        public bool IsLogging { get { return ctrl is null ? false : Type == DeviceType.Sensor ? ctrl.Logging : (ctrl.HubStatus & 0x01) == 0x01; } }
        /// <summary>
        /// True if the magnetic field mapping has been performed before.
        /// </summary>
        /// <returns> <c>true</c> if the magnetic field mapping has been performed before, otherwise, <c>false</c>. </returns>
        public bool MagFieldMapped { get; protected set; }
        /// <summary>
        /// True if the magnetic field mapping is on.
        /// </summary>
        /// <returns> <c>true</c> if the magnetic field mapping is on, otherwise, <c>false</c>. </returns>
        public bool MagneticFieldMappingOn { get; protected set; }
        /// <summary>
        /// True if the offset has been compensated before.
        /// </summary>
        /// <returns> <c>true</c> if the offset has been compensated before, otherwise, <c>false</c>. </returns>
        public bool OffsetCompensated { get; protected set; }
        /// <summary>
        /// True if the offset compensation is on.
        /// </summary>
        /// <returns> <c>true</c> if the offset compensation is on, otherwise, <c>false</c>. </returns>
        public bool OffsetCompensationOn { get; protected set; }
        /// <summary>
        /// True if the gyroscope autocalibration is on.
        /// </summary>
        /// <returns> <c>true</c> if the gyroscope autocalibration is on, otherwise, <c>false</c>. </returns>
        public bool AutoCalibrationOn { get; protected set; }
        /// <summary>
        /// Magnetometer calibration progress (percentage)
        /// </summary>
        /// <returns>
        /// An integer representing the calibration progress percentage.
        /// </returns>
        public int MagFieldMappingProgress { get; internal set; }
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
        public float MotionLevel { get; protected set; }
        /// <summary>
        /// Device Name
        /// </summary>
        /// <returns>
        /// A string containing the device name.
        /// </returns>
        public string Name { get; protected set; }
        /// <summary>
        /// Current sampling rate
        /// </summary>
        /// <returns>
        /// A <see cref="SamplingRate"/> object that contains the current sampling rate.
        /// </returns>
        public SamplingRate SamplingRate { get; protected set; }

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
        public bool IsTimeSyncMaster { get { return ctrl is null ? false : IsTimeSyncEnabled && (ctrl.EsbRadio & 0x80) != 0; } }
        /// <summary>
        /// True if the timesync mode is enabled
        /// </summary>
        public bool IsTimeSyncEnabled { get { return ctrl is null ? false : (ctrl.EsbRadio & 0x7F) != 0; } }

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


        /// <summary>
        /// Occurs when the battery level is received.
        /// </summary>
        public event EventHandler<float>? BatteryReceived;
        /// <summary>
        /// Occurs when the name of the device changes
        /// </summary>
        public event EventHandler<string>? DeviceNameChanged;
        /// <summary>
        /// Occurs when the magnetometer calibration has finished.
        /// </summary>
        public event EventHandler? MagFieldMappingDone;
        /// <summary>
        /// Occurs when the energy level is received.
        /// </summary>
        public event EventHandler<float>? MotionLevelReceived;
        /// <summary>
        /// Occurs when the device state is received.
        /// </summary>
        public event EventHandler<StateReceivedEventArgs>? StateReceived;
        /// <summary>
        /// Occurs when a stream packet is received.
        /// </summary>
        public event EventHandler<StreamPacket>? StreamPacketReceived;
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

        protected void RaiseBaseBatteryReceived(object? sender, float e) => BatteryReceived?.Invoke(sender, e);
        protected void RaiseBaseDeviceNameChanged(object? sender, string e) => DeviceNameChanged?.Invoke(sender, e);
        protected void RaiseBaseMagFieldMappingDone(object? sender, StateReceivedEventArgs e) => MagFieldMappingDone?.Invoke(sender, e);
        protected void RaiseBaseMotionLevelReceived(object? sender, float e) => MotionLevelReceived?.Invoke(sender, e);
        protected void RaiseBaseStateReceived(object? sender, StateReceivedEventArgs e) => StateReceived?.Invoke(sender, e);
        protected void RaiseBaseStreamPacketReceived(object? sender, StreamPacket e) => StreamPacketReceived?.Invoke(sender, e);
        protected void RaiseBaseInitializationDone(object? sender, string e) => InitializationDone?.Invoke(sender, e);
        protected void RaiseBaseMemoryAccesWasDisabled(object? sender, string e) => MemoryAccesWasDisabled?.Invoke(sender, e);
        protected void RaiseBaseCommunicationError(object? sender, EventArgs e) => CommunicationError?.Invoke(sender, e);



        /// <summary>
        /// Reads QSense Motion Device memory if BLE is connected and not streaming.
        /// </summary>
        /// <remarks>
        /// This method first verifies the BLE connection, then checks the current device state.
        /// If the device is connected and not actively streaming, it triggers a read operation
        /// on the memory control area using <see cref="bleApi.ReadMemory"/>.
        /// </remarks>
        public abstract void ReadMemory();

        /// <summary>
        /// Initializes the communication system by subscribing to BLE API events and 
        /// setting up the data parser for handling incoming data.
        /// </summary>
        /// <param name="parser">
        /// An implementation of <see cref="ICommunication"/> used to handle incoming data from the communication channel.
        /// </param>
        public abstract void Init(ICommunication parser);
        /// <summary>
        /// Resets the Device object to default values.
        /// </summary>
        public abstract void Reset();
        /// <summary>
        /// Changes the connection state of the object to disconnected. It does not reset the QSense Sensor information.
        /// </summary>
        public abstract void Disconnect();

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
            if (IsPeripheral) return;
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] = (byte)((data[0] & 0xF3) | ((byte)value << 2));
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
        public void SetDataBuffering(Buffering value)
        {
            if (IsPeripheral) return;
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[5] = (byte)(((byte)value << 4) | data[5] & 0x0F);
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
            if (IsPeripheral) return;
            byte[] data = { (byte)value };
            if (bleApi != null && bleApi.Connected)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_data_mode, data);
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
            if (IsPeripheral) return;
            var data = new byte[12];
            var bytesName = Encoding.ASCII.GetBytes(name);
            for (int i = 0; i < 12; i++)
            {
                if (bytesName.Length > i) data[i] = bytesName[i];
                else data[i] = 0;
            }
            if (bleApi != null && bleApi.Connected)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_device_name, data);
                Name = name;
                DeviceNameChanged?.Invoke(this, name);
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
            if (IsPeripheral) return;
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] = (byte)((data[0] & 0x0F) | ((byte)value << 4));
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
            gyrSensitivity = gyrScaleFactors[(int)value];
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
            if (IsPeripheral) return;
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[1] = (byte)(((byte)value & 0x0F) | data[1] & 0xF0);
            if (bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
        public void SetSensorConfig(SensitivityAcc accSens, SensitivityGyr gyrSens, SamplingRate sampRate, Buffering buffering, bool autocalibrateOn = false)
        {
            if (IsPeripheral) return;
            byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
            data[0] &= 0x03;
            data[0] |= (byte)((byte)accSens << 2);
            data[0] |= (byte)((byte)gyrSens << 4);
            data[0] |= autocalibrateOn ? (byte)0x80 : (byte)0x00;
            data[1] = (byte)(((byte)sampRate & 0x0F) | ((int)buffering << 4));
            if (BitConverter.ToUInt16(data) != BitConverter.ToUInt16(ctrl.State, 4) && bleApi != null && bleApi.Connected)
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
            if (IsPeripheral) return;
            if (bleApi != null && bleApi.Connected)
            {
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_algorithm_selection, new byte[] { (byte)value });
                ctrl.AlgorithmSelection = (byte)value;
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
            if (IsPeripheral) return;
            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x80;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
            if (IsPeripheral) return;
            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] &= 0x7F;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
            if (IsPeripheral) return;
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { val };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_annotation, data);
            }
        }
        /// <summary>
        /// Starts QSense Motion magnetic field mapping.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion magnetic field mapping. Throws an exception if an error occurs.
        /// </remarks>
        public abstract void StartMagFieldMapping();
        /// <summary>
        /// Stops QSense Motion magnetic field mapping.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion magnetic field mapping. Throws an exception if an error occurs.
        /// </remarks>
        public abstract void StopMagFieldMapping();
        /// <summary>
        /// Starts QSense Motion offset compensation.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion offset compensation. Throws an exception if an error occurs.
        /// </remarks>
        public abstract void StartOffsetCompensation();
        /// <summary>
        /// Starts QSense Motion Device data streaming.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion Device data streaming. Throws an exception if an error occurs.
        /// </remarks>
        public void StartStreaming()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            state = State.STREAMING;
            try
            {
                if (bleApi.Connected)
                {
                    bleApi.StreamMemory(Type == DeviceType.Hub ? MemMap.MEM_MAP_DATA_ADDR : MemMap.MEM_MAP_CONF_ADDR, (UInt16)MemMap.MEM_MAP_CONF_SIZE);
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
            if (IsPeripheral) return;
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
        public abstract void StartLogging();
        /// <summary>
        /// Stops QSense Motion Device data logging.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and stops QSense Motion Device data logging. Throws an exception if an error occurs.
        /// </remarks>
        public abstract void StopLogging();

        protected void AssertIsConnected()
        {
            if (state == State.DISCONNECTED)
                throw new Exception("The QSense Motion Device is not connected. Make sure that you call Device.Init() before calling this method.");
        }
        protected void BleParse_DataEvent(object sender, DataReceivedEventArgs e)
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
        internal void BleApi_Ble2MTxEvent(object sender, Ble2MTxEventArgs e)
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
        internal abstract void BleApi_Ble2MWriteCompletEvent(object sender, Ble2MDataEventArgs e);
        protected void GetMagFieldMappingState(object sender, StateReceivedEventArgs e)
        {
            MagFieldMappingProgress = e.MagFieldMappingProgress;
            if (MagFieldMappingProgress == 100)
            {
                StateReceived -= GetMagFieldMappingState;
                state = State.CONNECTED;
                MagFieldMappingDone?.Invoke(this, new EventArgs());
            }
        }
        protected StateReceivedEventArgs ExtractState(byte[] state)
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
            DataBuffering = (Buffering)(state[5] >> 4);

            StateReceivedEventArgs stateArgs = new StateReceivedEventArgs(OffsetCompensationOn, OffsetCompensated, MagneticFieldMappingOn,
                MagFieldMapped, MagFieldMappingProgress, AccSensitivity, GyrSensitivity, AutoCalibrationOn, SamplingRate,
                DataBuffering, ConnectionInterval);
            return stateArgs;
        }
        byte[]? prevState = null;
        protected void StateUpdate()
        {
            if (ctrl == null) return;

            StateReceivedEventArgs state = ExtractState(ctrl.State);

            accSensitivity = accScaleFactors[(int)state.AccSensitivity];
            gyrSensitivity = gyrScaleFactors[(int)state.GyroSensitivity];
            AutoCalibrationOn = state.IsAutoCalibrationOn;
            if (prevState is null || !prevState.SequenceEqual(ctrl.State))
            {
                prevState = ctrl.State;
                StateReceived?.Invoke(this, state);
            }
        }
    }

}