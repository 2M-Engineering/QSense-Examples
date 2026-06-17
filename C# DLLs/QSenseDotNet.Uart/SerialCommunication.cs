using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace QSenseDotNet.Uart
{
    public class SerialCommunication : ICommunication, IDisposable
    {
        private const char PREFIX = '$';
        private const char POSTFIX = '\n';
        private const char OPCODE_RECEIVE = 'R';
        private const char OPCODE_UPGRADE = 'U';
        private const char OPCODE_TRANSMIT = 'T';

        private SerialPort port;
        private Int32 packetIndex = 0;
        private char opcode;
        private byte hi_nible;
        private List<byte> dataBuffer = new List<byte>();
        public event QSenseDotNet.DataReceivedEventHandler? DataReceived;
        public event EventHandler? Disconnected;

        public bool IsOpen { get { return port.IsOpen; } }

        public SerialCommunication(string portName)
        {
            port = new SerialPort(portName, 460800, Parity.None, 8, StopBits.One);
            port.WriteTimeout = 500;
            port.DtrEnable = true;
            port.DataReceived += Port_DataReceived;
            port.Open();
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.IsOpen) 
            {
                string s = port.ReadExisting();
                ParseData(s);
            }
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
                                foreach (byte b in dataBuffer) s += b.ToString("X2");
                                DataReceived?.Invoke(this, new DataReceivedEventArgs(s));
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

        public void EnableBootloader()
        {
            try
            {
                string s = "" + PREFIX + OPCODE_UPGRADE + POSTFIX;
                if (port.IsOpen)
                    port.Write(s);
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        public void Write(string data)
        {
            try
            {
                string s = "" + PREFIX + OPCODE_TRANSMIT + data + POSTFIX;
                if (port.IsOpen)
                    port.Write(s);
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        private static bool ValidHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        private static byte GetValFromHexChars(byte hi_nible, byte lo_nible)
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

        public void Dispose()
        {
            port.Close();
        }

        ~SerialCommunication()
        {
            port.Close();
        }
    }
}
