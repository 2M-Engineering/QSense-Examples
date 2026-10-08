using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace QSenseDotNet.Dongle
{
    public class Dongle
    {
        private const Int32 MAX_NODES = 13;
        private const string DEVICE_NAME = "QSense\0";
        private const char PREFIX = '$';
        private const char POSTFIX = '\n';
        private const char OPCODE_RECEIVE = 'R';
        private const char OPCODE_CONNECT = 'C';
        private const char OPCODE_DISCONNECT = 'D';
        private const char OPCODE_STOPSCAN = 'I';
        private const char OPCODE_STATUS = 'S';
        private const char OPCODE_CONNECT_WHITELIST = 'W';


        #region Fields
        private ICommunication? parser;
        private DongleCommunication[] channels = new DongleCommunication[MAX_NODES];
        private Int32 packetIndex = 0;
        private char opcode;
        private byte hi_nible;
        private List<byte> dataBuffer = new List<byte>();
        private DongleStatusReceivedEventArgs statusConnections = new DongleStatusReceivedEventArgs();
        private System.Timers.Timer readTimer;
        private bool[] notifyConnection = new bool[MAX_NODES];
        private bool scanStarted = false;
        #endregion
        /// <summary>
        /// Array of QSense Sensors
        /// </summary>
        public Device[] Devices { get; private set; } = new Device[MAX_NODES];
        public string Comport { get { return parser is null ? "" : ((SerialCommunication)parser).Comport; } }
        public bool IsScanning { get { return scanStarted; } }
        /// <summary>
        /// Occurs when a sensor was connected
        /// </summary>
        public event EventHandler<int>? SensorConnected;
        /// <summary>
        /// Occurs when a sensor was disconnected
        /// </summary>
        public event EventHandler<int>? SensorDisconnected;
        /// <summary>
        /// Occurs when the dongle stops scanning
        /// </summary>
        public event EventHandler DongleScanStopped;
        /// <summary>
        /// Occurs when an exception is thrown during communication with the QSense wireless BLE USB Dongle, either while sending or receiving a package
        /// </summary>
        public event EventHandler CommunicationError;
        /// <summary>
        /// Occurs when an exception is thrown during communication with the QSense Sensor, either while sending or receiving a package
        /// </summary>
        public event EventHandler<int> DeviceCommunicationError;

        internal delegate void DongleStatusReceivedEventHandler(object sender, DongleStatusReceivedEventArgs e);
        /// <summary>
        /// Creates an instance of the Dongle class
        /// </summary>
        /// <param name="port">Serial Port of the QSense wireless BLE USB Dongle</param>
        public Dongle(string port)
        {
            this.parser = new SerialCommunication(port);
            this.parser.DataReceived += port_DataReceived;
            statusConnections.MaxDataSize = 0;

            readTimer = new System.Timers.Timer();
            readTimer.Interval = 2000;
            readTimer.Elapsed += ReadTimer_Elapsed;
            readTimer.AutoReset = true;
            readTimer.Stop();

            for (Int32 i = 0; i < MAX_NODES; i++)
            {
                channels[i] = new DongleCommunication(parser, i);
                Devices[i] = new Sensor(); // Initialize devices as sensors by default;
            }
            scanStarted = false;
            GetStatus();
            readTimer.Start();
        }

        /// <summary>
        /// Disconnects from all conected QSense Sensors
        /// </summary>
        public void Disconnect()
        {
            readTimer.Stop();
            scanStarted = false;
            for (int i = 0; i < 13; i++) Devices[i].Disconnect();
            string s = "" + PREFIX + OPCODE_DISCONNECT + POSTFIX;
            try
            {
                parser?.Write(s);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
            readTimer.Start();
        }
        /// <summary>
        /// Triggers the QSense wireless BLE USB Dongle to start scanning for QSense Sensors
        /// </summary>
        public void StartScanning()
        {
            readTimer.Stop();
            string s = "" + PREFIX + OPCODE_CONNECT;
            s += MAX_NODES.ToString("X2");
            for (int i = 0; i < MAX_NODES; i++) Devices[i] = new Sensor();
            byte[] packet = ASCIIEncoding.ASCII.GetBytes(DEVICE_NAME);
            foreach (byte b in packet) s += b.ToString("X2");
            s += POSTFIX;
            try
            {
                parser?.Write(s);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
            readTimer.Start();
        }
        /// <summary>
        /// Triggers the QSense wireless BLE USB Dongle to start scanning for the specified QSense Sensors.
        /// </summary>
        /// <param name="serialNumbers">An array of serial numbers identifying the QSense Sensors to connect to.</param>
        public void ConnectWhitelist(string[] serialNumbers, bool scanningForHubs = false)
        {
            readTimer.Stop();
            if (scanningForHubs)
            {
                for (int i = 0; i < MAX_NODES; i++) Devices[i] = new Hub();
            }
            else
            {
                for (int i = 0; i < MAX_NODES; i++) Devices[i] = new Sensor();
            }
            string s = "" + PREFIX + OPCODE_CONNECT_WHITELIST;
            s += (serialNumbers.Length).ToString("X2");
            UInt64[] addresses = serialNumbers.Select(sn => Convert.ToUInt64(sn.Split("-")[0], 16)).ToArray();
            foreach (UInt64 address in addresses)
            {
                byte[] addressBytes = new byte[6];
                Array.Copy(BitConverter.GetBytes(address), 0, addressBytes, 0, 6);
                foreach (byte b in addressBytes) s += b.ToString("X2");
            }
            s += POSTFIX;
            try
            {
                if (s.Length <= 64) parser?.Write(s);
                else
                {
                    for (int i = 0; i < s.Length; i += 63)
                    {
                        parser?.Write(s.Substring(i, Math.Min(63, s.Length - i)));
                    }
                }
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
            readTimer.Start();
        }
        /// <summary>
        /// Triggers the QSense wireless BLE USB Dongle to stop scanning
        /// </summary>
        public void StopScanning()
        {
            readTimer.Stop();
            scanStarted = false;
            string s = "" + PREFIX + OPCODE_STOPSCAN + POSTFIX;
            try
            {
                parser?.Write(s);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
            readTimer.Start();
        }

        public void Close()
        {
            if (parser != null)
                ((SerialCommunication)parser).Dispose();
        }

        #region Private methods

        private void ReadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            readTimer.Stop();
            for (int i = 0; i < MAX_NODES; i++) if (Devices[i].IsConnected && !Devices[i].IsStreaming) Devices[i].ReadMemory();
            GetStatus();
            readTimer.Start();
        }
        private void GetStatus()
        {
            string s = "" + PREFIX + OPCODE_STATUS + POSTFIX;
            try
            {
                parser?.Write(s);
            }
            catch
            {
                CommunicationError?.Invoke(this, new EventArgs());
            }
        }
        private void port_DataReceived(object sender, DataReceivedEventArgs e)
        {
            ParseData(e.Data);
        }

        private void ParseData(string segment)
        {
            foreach (char c in segment)
            {
                if (PREFIX == c) packetIndex = 0;
                else packetIndex++;

                if (packetIndex == 1)
                {
                    opcode = c;
                    dataBuffer.Clear();
                }

                if (packetIndex > 1)
                {
                    if (POSTFIX == c)
                    {
                        switch (opcode)
                        {
                            case OPCODE_RECEIVE:
                                string s = "";
                                try
                                {
                                    var index = dataBuffer[0];
                                    for (int i = 1; i < dataBuffer.Count; i++) s += dataBuffer[i].ToString("X2");
                                    channels[index].InvokeDataEvent(s);
                                }
                                catch (Exception)
                                {
                                    CommunicationError?.Invoke(this, new EventArgs());
                                    continue;
                                }
                                break;
                            case OPCODE_STATUS:
                                statusConnections.MaxDataSize = dataBuffer[0];
                                statusConnections.StatusChannels = new List<Status>();
                                for (Int32 q = 1; q < dataBuffer.Count; q++)
                                {
                                    statusConnections.StatusChannels.Add((Status)dataBuffer[q]);
                                }
                                BleParse_StatusEvent(this, statusConnections);
                                break;
                            default:
                                break;
                        }

                        packetIndex = 0;
                    }
                    else if (ValidHex(c))
                    {
                        if (packetIndex % 2 == 0)
                        {
                            hi_nible = (byte)c;
                        }
                        else
                        {
                            dataBuffer.Add(GetValFromHexChars(hi_nible, (byte)c));
                        }
                    }
                    else
                    {
                        packetIndex--;
                    }
                }
            }
        }

        private void BleParse_StatusEvent(object sender, DongleStatusReceivedEventArgs e)
        {
            readTimer.Stop();
            List<int> sensorConnectedEvents = new List<int>();
            List<int> sensorDisconnectedEvents = new List<int>();
            for (Int32 i = 0; i < MAX_NODES; i++)
            {
                if (e.StatusChannels[i] == Status.Connected)
                {
                    if (!Devices[i].IsInitializing && !Devices[i].IsConnected)
                    {
                        Devices[i].Init(channels[i]);
                        Devices[i].MaxPacketSize = e.MaxDataSize;
                        Devices[i].InitializationDone += Device_InitializationDone;
                        Devices[i].CommunicationError -= Device_CommunicationError;
                        Devices[i].CommunicationError += Device_CommunicationError;
                        Devices[i].MemoryAccesWasDisabled -= Device_MemoryAccesWasDisabled;
                        Devices[i].MemoryAccesWasDisabled += Device_MemoryAccesWasDisabled;
                    }
                    else if (notifyConnection[i])
                    {
                        notifyConnection[i] = false;
                        sensorConnectedEvents.Add(i);
                    }
                }
                else if (Devices[i].IsConnected)
                {
                    sensorDisconnectedEvents.Add(i);
                    Devices[i].Disconnect();
                }
            }
            if (!scanStarted && e.StatusChannels.Any(x => x == Status.Scanning)) scanStarted = true;
            else if (scanStarted && !e.StatusChannels.Any(x => x == Status.Scanning))
            {
                scanStarted = false;
                DongleScanStopped?.Invoke(this, new EventArgs());
            }

            foreach (int handle in sensorConnectedEvents)
                SensorConnected?.Invoke(this, handle);
            foreach (int handle in sensorDisconnectedEvents)
                SensorDisconnected?.Invoke(this, handle);

            readTimer.Start();
        }

        private void Device_MemoryAccesWasDisabled(object sender, string serialNumber)
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                if (Devices[i].SerialNumber == serialNumber)
                {
                    Devices[i].Disconnect();
                    SensorDisconnected?.Invoke(this, i);
                    return;
                }
            }
        }

        private void Device_InitializationDone(object sender, string serialNumber)
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                if (Devices[i].SerialNumber == serialNumber)
                {
                    Devices[i].InitializationDone -= Device_InitializationDone;
                    notifyConnection[i] = true;
                    return;
                }
            }
        }

        private void Device_CommunicationError(object sender, EventArgs e)
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                if (Devices[i] == sender)
                {
                    DeviceCommunicationError?.Invoke(this, i);
                    return;
                }
            }
        }

        private static bool ValidHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        private byte GetValFromHexChars(byte hi_nible, byte lo_nible)
        {
            Int32 result = 0;
            if (hi_nible >= '0' && hi_nible <= '9') result = hi_nible - '0';
            else if (hi_nible >= 'A' && hi_nible <= 'F') result = 10 + hi_nible - 'A';
            else if (hi_nible >= 'a' && hi_nible <= 'f') result = 10 + hi_nible - 'a';
            result = result << 4;
            if (lo_nible >= '0' && lo_nible <= '9') result += lo_nible - '0';
            else if (lo_nible >= 'A' && lo_nible <= 'F') result += 10 + lo_nible - 'A';
            else if (lo_nible >= 'a' && lo_nible <= 'f') result += 10 + lo_nible - 'a';
            return (byte)result;
        }

        ~Dongle()
        {
            if (parser != null)
            {
                parser.DataReceived -= port_DataReceived;
                Disconnect();
                readTimer.Stop();
                ((SerialCommunication)parser)?.Dispose();
            }
        }
        #endregion
    }

    internal enum Status
    {
        Idle = 0,
        Scanning,
        Connected
    }

    internal class DongleStatusReceivedEventArgs : EventArgs
    {
        public List<Status> StatusChannels { get; set; } = new List<Status>();
        public Int32 MaxDataSize { get; set; }
    }

    internal class DongleCommunication : ICommunication
    {
        private byte index;
        private const char PREFIX = '$';
        private const char POSTFIX = '\n';
        private const char OPCODE_TRANSMIT = 'T';

        private ICommunication parser;

        public DongleCommunication(ICommunication parser, Int32 index)
        {
            this.parser = parser;
            this.index = (byte)index;
        }

        public event DataReceivedEventHandler? DataReceived;

        public void Write(string data)
        {
            string s = "" + PREFIX + OPCODE_TRANSMIT + index.ToString("X2") + data + POSTFIX;
            parser.Write(s);
        }

        public void InvokeDataEvent(string data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
        }
    }
}
