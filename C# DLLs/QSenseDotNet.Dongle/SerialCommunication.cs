using System;
using System.IO.Ports;

namespace QSenseDotNet.Dongle
{
    internal class SerialCommunication : ICommunication, IDisposable
    {
        private SerialPort port;
        public event DataReceivedEventHandler? DataReceived;
        private const int blockLimit = 237;
        private byte[] buffer = new byte[blockLimit];
        private Action ReadAsync;

        public SerialCommunication(string portName)
        {
            port = new SerialPort(portName, 460800, Parity.None, 8, StopBits.One);
            port.DtrEnable = true;
            port.Open();

            ReadAsync = () =>
            {
                try
                {
                    port.BaseStream.BeginRead(buffer, 0, buffer.Length, Port_DataReceived(), null);
                }
                catch (Exception) { }
            };
            ReadAsync();
        }

        private AsyncCallback Port_DataReceived()
        {
            return delegate (IAsyncResult ar)
            {
                try
                {
                    int actualLength = port.BaseStream.EndRead(ar);
                    byte[] received = new byte[actualLength];
                    Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                    string s = System.Text.Encoding.ASCII.GetString(received);
                    DataReceived?.Invoke(this, new DataReceivedEventArgs(s));
                }
                catch (Exception) { }
                this.ReadAsync();
            };
        }

        public void Write(string data)
        {
            if (port.IsOpen)
                port.Write(data);
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
