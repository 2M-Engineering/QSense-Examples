using System;
using System.Diagnostics;
using System.Linq;

namespace QSenseDotNet
{
    public class Sensor : Device
    {
        /// <summary>
        /// Number of packets stored in the internal memory
        /// </summary>
        public UInt32 PacketCount { get { return ctrl is null ? 0 : ctrl.PacketCount; } }
        public int HubIndex { get; private set; } = -1;

        public event EventHandler<StreamPacket>? DownloadPacketReceived;
        public event EventHandler<Exception>? DownloadFailed;
        public event EventHandler? DownloadDone;

        /// <summary>
        /// Creates an object of the Device class
        /// </summary>
        public Sensor()
        {
            parser = null;
            bleApi = new Ble2M();
            Name = "";
            ctrl = null;
            synced = false;
            HubIndex = -1;
        }
        public Sensor(UInt64 address)
        {
            parser = null;
            bleApi = new Ble2M();
            Name = "";
            ctrl = null;
            synced = false;
            Address = address;
            state = State.CONNECTED;
            IsPeripheral = true;
        }

        /// <summary>
        /// Initializes the communication system by subscribing to BLE API events and 
        /// setting up the data parser for handling incoming data.
        /// </summary>
        /// <param name="parser">
        /// An implementation of <see cref="ICommunication"/> used to handle incoming data from the communication channel.
        /// </param>
        public override void Init(ICommunication parser)
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
        public override void ReadMemory()
        {
            AssertIsConnected();

            if (state > (State)1 && state < (State)5) //Connected and not streaming
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (ushort)MemMap.MEM_MAP_CTRL_SIZE);
        }

        /// <summary>
        /// Resets the Device object to default values.
        /// </summary>
        public override void Reset()
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
            ID = 0;
            Address = 0;
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
            HubIndex = -1;
        }

        /// <summary>
        /// Changes the connection state of the object to disconnected. It does not reset the QSense Sensor information.
        /// </summary>
        public override void Disconnect()
        {
            state = State.DISCONNECTED;
            if (parser != null)
            {
                parser.DataReceived -= BleParse_DataEvent;
                parser = null;
            }
            bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
            bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
            bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;

            bleApi = new Ble2M();
            streamingData = false;
            Battery = 0.0f;
            MagFieldMapped = false;
            MagneticFieldMappingOn = false;
            OffsetCompensated = false;
            OffsetCompensationOn = false;
            MagFieldMappingProgress = 0;
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
            if (IsPeripheral) return;
            try
            {
                if (bleApi != null && bleApi.Connected)
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_ui_state, new byte[] { (byte)animation, blue, green, red });
            }
            catch (System.IO.IOException) { }
        }

        /// <summary>
        /// Starts QSense Motion magnetic field mapping.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion magnetic field mapping. Throws an exception if an error occurs.
        /// </remarks>
        public override void StartMagFieldMapping()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            state = State.FIELD_MAPPING;
            StateReceived -= GetMagFieldMappingState;
            StateReceived += GetMagFieldMappingState;
            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x02;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
        public override void StopMagFieldMapping()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            state = State.CONNECTED;
            StateReceived -= GetMagFieldMappingState;
            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] &= 0xFD;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
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
        public override void StartOffsetCompensation()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            state = State.OFFSET_COMPENSATION;
            try
            {
                if (bleApi.Connected)
                {
                    byte[] data = new byte[2] { ctrl.State[4], ctrl.State[5] };
                    data[0] |= 0x01;
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_state, data);
                }
                streamingData = false;
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
            if (IsPeripheral) return;
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { (byte)(0x7F & networkKey) };
                if (isMaster) data[0] |= 0x80;
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_esb_radio, data);
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_hub_mode, new byte[] { 0x2 });
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
            if (IsPeripheral) return;
            if (bleApi.Connected)
            {
                byte[] data = new byte[1] { 0x00 };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_esb_radio, data);
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_hub_mode, new byte[] { 0x0 });
            }
        }

        /// <summary>
        /// Starts QSense Motion Device data logging.
        /// </summary>
        /// <remarks>
        /// This method verifies Bluetooth connection and starts QSense Motion Device data logging. Throws an exception if an error occurs.
        /// </remarks>
        public override void StartLogging()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            try
            {
                if (bleApi.Connected)
                {
                    if (ctrl.Version < ((2 << 16) | 3 << 8 | 2)) return; // Logging feature was introduced in v2.3.2
                    byte[] data = new byte[1] { 0x01 };
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_logging, data);
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
        public override void StopLogging()
        {
            AssertIsConnected();
            if (IsPeripheral) return;
            try
            {
                if (ctrl.Version < ((2 << 16) | 3 << 8 | 2)) return; // Logging feature was introduced in v2.3.2
                byte[] data = new byte[1] { 0x00 };
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_logging, data);
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
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_erase_file, new byte[] { 0x01 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public void StartDownload(string drive = "")
        {
            AssertIsConnected();
            try
            {
                if (ctrl.Version < ((2 << 16) | 3 << 8 | 2)) return; // Logging feature was introduced in v2.3.2
                fileAddress = 0;
                bleApi.ReadMemory(MemMap.MEM_MAP_FILE_ADDR, downloadChunkSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        internal override void BleApi_Ble2MWriteCompletEvent(object sender, Ble2MDataEventArgs e)
        {
            if (e.Address == MemMap.MEM_MAP_ADDR_pin)
            {
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (UInt16)MemMap.MEM_MAP_CTRL_SIZE);
            }
            else if (e.Address == MemMap.MEM_MAP_ADDR_time && e.Data.Length == 4)
            {
                if (state == State.INITIALIZING)
                {
                    RaiseBaseInitializationDone(this, SerialNumber);
                    state = State.CONNECTED;
                }
            }
        }
        internal void BleApi_Ble2MDataEvent(object sender, Ble2MDataEventArgs e)
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
                        RaiseBaseDeviceNameChanged(this, Name);
                    if (oldBatt != Battery)
                        RaiseBaseBatteryReceived(this, ctrl.Battery);
                    if (oldMotionLevel != MotionLevel)
                        RaiseBaseMotionLevelReceived(this, ctrl.MotionLevel);
                    if (state == State.INITIALIZING)
                    {
                        uint unixtime = (uint)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        bleApi?.WriteMemory(MemMap.MEM_MAP_ADDR_time, BitConverter.GetBytes(unixtime));
                    }

                    if (ctrl.Pin != MemMap.MEM_MAP_PIN && ctrl.Pin != MemMap.MEM_MAP_PIN_TEST) RaiseBaseMemoryAccesWasDisabled(this, SerialNumber);
                }
            }
            else if ((e.Address == MemMap.MEM_MAP_CONF_ADDR && streamingData))
            {
                if (ctrl == null) return;
                var data = new MemMapData();
                data.AddData(e.Data);
                if (data.Packet == null) return;
                RaiseBaseStreamPacketReceived(this, data.Packet);
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
                    ctrl.PacketCount = 0;
                    DownloadDone?.Invoke(this, new EventArgs());
                }
                else
                {
                    StreamPacket packet = new StreamPacket(e.Data);
                    DownloadPacketReceived?.Invoke(this, packet);
                    fileAddress += downloadChunkSize;
                    bleApi.ReadMemory(MemMap.MEM_MAP_FILE_ADDR + fileAddress, downloadChunkSize);
                }
            }
        }
        internal void InvokeStreamPacketReceived(StreamPacket e) => this.RaiseBaseStreamPacketReceived(this, e);
        ~Sensor()
        {
            try
            {
                bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
                bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
                bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;
            }
            catch (NullReferenceException) { }
        }
    }

}