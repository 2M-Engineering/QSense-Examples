using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QSenseDotNet
{
    public class Hub : Device
    {
        const string DRIVE_LABEL = "QSENSEHUB";
        const string DATA_FILE_NAME = "data.txt";

        public UInt32 DataSize { get { return ctrl is null ? 0 : ctrl.HubDataSize - MemMap.MEM_MAP_CTRL_SIZE_HUB; } }
        public UInt32 Status { get { return ctrl is null ? 0 : ctrl.HubStatus; } }

        public Sensor[] Peripherals { get; private set; }
        public UInt64[] Whitelist { get { return ctrl is null ? new UInt64[0] : ctrl.HubWhitelist; } }
        public event EventHandler<int>? PeripheralConnected;
        public event EventHandler<int>? PeripheralDisconnected;

        public event EventHandler<KeyValuePair<string, StreamPacket>>? DownloadHubPacketReceived;
        public event EventHandler<Exception>? DownloadFailed;
        public event EventHandler? DownloadDone;

        /// <summary>
        /// Creates an object of the Device class
        /// </summary>
        public Hub()
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
        public override void Init(ICommunication parser)
        {
            bleApi.Ble2MDataEvent += BleApi_Ble2MDataEvent;
            bleApi.Ble2MRelayDataEvent += BleApi_Ble2MRelayDataEvent;
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
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (ushort)MemMap.MEM_MAP_CTRL_SIZE_HUB);
        }

        /// <summary>
        /// Resets the Device object to default values.
        /// </summary>
        public override void Reset()
        {
            state = State.DISCONNECTED;
            if (parser != null)
            {
                parser.DataReceived -= BleParse_DataEvent;
                parser = null;
            }
            bleApi.Ble2MRelayDataEvent -= BleApi_Ble2MRelayDataEvent;
            bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
            bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
            bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;

            bleApi = new Ble2M();
            Name = "";
            ID = 0;
            Address = 0;
            ctrl = null;
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
            Peripherals = new Sensor[0];
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
            if (Peripherals != null)
            {
                foreach (var peripheral in Peripherals) peripheral?.Disconnect();
            }
        }


        /// <summary>
        /// Relays the mesage to set color and animation of a QSense Motion peripheral's LEDs.
        /// </summary>
        /// <param name="red">Intensity of red color</param>
        /// <param name="green">Intensity of green color</param>
        /// <param name="blue">Intensity of blue color</param>
        /// <param name="animation">LED animation to display. This parameter accepts two values: 0 (blinking LED) and 1 (fixed LED)</param>
        public void RelaySetLEDAnimation(int handle, byte red, byte green, byte blue, LEDAnimation animation)
        {
            try
            {
                if (bleApi != null && bleApi.Connected)
                    bleApi.RelayWriteMemory((byte)handle, MemMap.MEM_MAP_ADDR_ui_state, new byte[] { (byte)animation, blue, green, red });
            }
            catch (System.IO.IOException) { }
        }

        /// <summary>
        /// Starts QSense Motion magnetic field mapping in the Hub and all its peripherals.
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
                    if (Type == DeviceType.Hub)
                    {
                        bleApi.RelayWriteMemory(255, MemMap.MEM_MAP_ADDR_state, data, true);
                        foreach (var peripheral in Peripherals) peripheral.MagFieldMappingProgress = 0;
                    }
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
        /// Stops QSense Motion magnetic field mapping in the Hub and all its peripherals.
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
                    if (Type == DeviceType.Hub)
                    {
                        bleApi.RelayWriteMemory(255, MemMap.MEM_MAP_ADDR_state, data, true);
                    }
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
        /// Starts QSense Motion offset compensation in the Hub and all its peripherals.
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
                    if (Type == DeviceType.Hub)
                    {
                        bleApi.RelayWriteMemory(255, MemMap.MEM_MAP_ADDR_state, data, true);
                    }
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
                    UInt32 startTime = (UInt32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    UInt32 stopTime = startTime + 3600 * 24;
                    byte[] startAndStopTime = new byte[8];
                    Array.Copy(BitConverter.GetBytes(startTime), 0, startAndStopTime, 0, 4);
                    Array.Copy(BitConverter.GetBytes(stopTime), 0, startAndStopTime, 4, 4);
                    bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_start_time, BitConverter.GetBytes(startTime));
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
                UInt32 unixtime = (UInt32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                bleApi.WriteMemory(MemMap.MEM_MAP_ADDR_stop_time, BitConverter.GetBytes(unixtime));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void StartDownload(string drive = "")
        {
            AssertIsConnected();
            if (string.IsNullOrEmpty(drive) || !Directory.Exists(drive)) throw new Exception("Drive does not exist.");
            string path = Path.Combine(drive, DATA_FILE_NAME);
            DateTime startTime = Utilities.UnixTimeStampToDateTime(0);
            List<string> whitelist = new List<string> { Address.ToString("X") };

            try
            {
                using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    int pos = 0;
                    int length = (int)br.BaseStream.Length;
                    if (length < MemMap.MEM_MAP_CTRL_SIZE_HUB)
                    {
                        DownloadFailed?.Invoke(this, new Exception("Empty file"));
                        return;
                    }

                    MemMapCtrl header = new MemMapCtrl(br.ReadBytes((int)MemMap.MEM_MAP_CTRL_SIZE_HUB));
                    ctrl = header;
                    whitelist.AddRange(header.HubWhitelist.Select(x => x.ToString("X")));
                    pos += (int)MemMap.MEM_MAP_CTRL_SIZE_HUB;

                    while (length - pos >= StreamPacket.PacketSize * whitelist.Count)
                    {
                        StreamPacket[] packets = new StreamPacket[whitelist.Count];

                        for (int i = 0; i < whitelist.Count; i++) packets[i] = new StreamPacket(br.ReadBytes(StreamPacket.PacketSize));

                        pos += StreamPacket.PacketSize * whitelist.Count;
                        for (int i = 0; i < whitelist.Count; i++)
                        {
                            if (packets[i].Seconds > Utilities.UnixTimeStampFromDateTime(new DateTime(2026))) DownloadHubPacketReceived?.Invoke(this, new KeyValuePair<string, StreamPacket>(whitelist[i], packets[i]));
                        }
                    }
                }
                ctrl.HubDataSize = MemMap.MEM_MAP_CTRL_SIZE_HUB;
                File.WriteAllBytes(path, ctrl.ToByteArray());
                DownloadDone?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                DownloadFailed?.Invoke(this, ex);
            }
        }

        public void SetupHubConfig(string drive, UInt64[] whitelist, DataMode mode, SamplingRate samplingRate, Buffering buffering, SensitivityAcc accRange, SensitivityGyr gyroRange, Algorithms algo, bool autocalibrateOn)
        {
            if (Type != DeviceType.Hub) throw new Exception("The QSense Motion Device is not a hub. This method can only be called on hub devices.");
            if (!Directory.Exists(drive)) throw new Exception("Drive does not exist.");
            string path = Path.Combine(drive, DATA_FILE_NAME);

            ctrl.HubStartTime = 0;
            ctrl.HubStopTime = 0;
            ctrl.HubButtonStartEnable = 0;
            ctrl.HubWhitelist = whitelist;
            ctrl.DataMode = mode;
            ctrl.State[4] &= 0x03;
            ctrl.State[4] |= (byte)((byte)accRange << 2);
            ctrl.State[4] |= (byte)((byte)gyroRange << 4);
            ctrl.State[4] |= autocalibrateOn ? (byte)0x80 : (byte)0x00;
            ctrl.State[5] = (byte)(((byte)samplingRate & 0x0F) | ((int)buffering << 4));
            ctrl.AlgorithmSelection = (byte)algo;

            Mode = mode;
            SamplingRate = samplingRate;
            AccSensitivity = accRange;
            GyrSensitivity = gyroRange;
            AutoCalibrationOn = autocalibrateOn;

            Random rnd = new Random();
            ctrl.EsbRadio = (byte)rnd.Next(1, 127);
            ctrl.HubMode = 1;
            File.WriteAllBytes(path, ctrl.ToByteArray());
        }

        private void BleApi_Ble2MDataEvent(object sender, Ble2MDataEventArgs e)
        {
            if (e.Address == MemMap.MEM_MAP_CTRL_ADDR && e.Data.Length == MemMap.MEM_MAP_CTRL_SIZE_HUB)
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
                        Peripherals = new Sensor[Whitelist is null ? 0 : Whitelist.Length];
                        uint unixtime = (uint)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        bleApi?.WriteMemory(MemMap.MEM_MAP_ADDR_time, BitConverter.GetBytes(unixtime));
                    }
                    if (state >= State.CONNECTED && state <= State.FIELD_MAPPING && Type == DeviceType.Hub)
                    {
                        for (int i = 0; i < Peripherals.Length; i++)
                        {
                            bool connected = ((Status >> 16) & (1 << i)) != 0;
                            if ((Peripherals[i] is null || !Peripherals[i].IsConnected) && connected)
                            {
                                Peripherals[i] = new Sensor(Whitelist[i]);
                                PeripheralConnected?.Invoke(this, i);
                            }
                            else if (Peripherals[i] != null && Peripherals[i].IsConnected && !connected)
                            {
                                PeripheralDisconnected?.Invoke(this, i);
                                Peripherals[i].Disconnect();
                            }
                        }
                        for (int i = 0; i < Peripherals.Length; i++)
                        {
                            bool connected = ((Status >> 16) & (1 << i)) != 0;
                            if (connected) bleApi.RelayReadMemory((byte)i, MemMap.MEM_MAP_CTRL_ADDR, (ushort)MemMap.MEM_MAP_CTRL_SIZE, i != 0);
                        }
                    }

                    if (ctrl.Pin != MemMap.MEM_MAP_PIN && ctrl.Pin != MemMap.MEM_MAP_PIN_TEST) RaiseBaseMemoryAccesWasDisabled(this, SerialNumber);
                }
            }
            else if (e.Address == MemMap.MEM_MAP_DATA_ADDR && streamingData)
            {
                if (ctrl == null || BitConverter.ToUInt64(e.Data) == 0) return;
                var data = new MemMapData();
                data.AddData(e.Data);
                if (data.Packet == null) return;
                var packets = data.Packet.SplitPacket(Whitelist.Length + 1);
                RaiseBaseStreamPacketReceived(this, packets[0]);
                for (int i = 0; i < Whitelist.Length; i++)
                {
                    if (Peripherals[i] != null && Peripherals[i].IsConnected) Peripherals[i].InvokeStreamPacketReceived(packets[i + 1]);
                }
            }
        }
        private void BleApi_Ble2MRelayDataEvent(object sender, Ble2MRelayDataEventArgs e)
        {
            Ble2MDataEventArgs ble2MDataEventArgs = new Ble2MDataEventArgs
            {
                Address = e.Address,
                Data = e.Data,
            };
            if (Peripherals[e.Handle] != null && Peripherals[e.Handle].IsConnected) Peripherals[e.Handle].BleApi_Ble2MDataEvent(sender, ble2MDataEventArgs);
        }
        internal override void BleApi_Ble2MWriteCompletEvent(object sender, Ble2MDataEventArgs e)
        {
            if (e.Address == MemMap.MEM_MAP_ADDR_pin)
            {
                bleApi.ReadMemory(MemMap.MEM_MAP_CTRL_ADDR, (UInt16)MemMap.MEM_MAP_CTRL_SIZE_HUB);
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

        ~Hub()
        {
            try
            {
                bleApi.Ble2MRelayDataEvent -= BleApi_Ble2MRelayDataEvent;
                bleApi.Ble2MDataEvent -= BleApi_Ble2MDataEvent;
                bleApi.Ble2MTxEvent -= BleApi_Ble2MTxEvent;
                bleApi.Ble2MWriteCompletEvent -= BleApi_Ble2MWriteCompletEvent;
            }
            catch (NullReferenceException) { }
        }
    }

}