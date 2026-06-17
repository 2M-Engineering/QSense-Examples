using System;
using System.Linq;

namespace QSenseDotNet.Uart
{
    public delegate void ConnectionEventHandler(object sender, EventArgs e);

    public class Uart
    {
        #region Fields
        private QSenseDotNet.Device _device;
        private SerialCommunication? parser;
        private Status Status = Status.Idle;
        private Int32 MaxDataSize;
        #endregion

        public string Name { get { return _device is null ? "" : _device.Name; } }
        public string Version { get { return _device is null ? "" : _device.Version; } }
        public UInt64 Id { get { return _device is null ? 0 : _device.ID; } }
        public UInt64 Address { get { return _device is null ? 0 : _device.Address; } }
        public int InterfaceVersion { get { return _device is null ? 0 : int.Parse(_device.Version.Split('.').Last()); } }
        public UInt32 PacketCount { get { return _device is null ? 0 : _device.PacketCount; } }

        public event EventHandler? SensorConnected;
        public event EventHandler? SensorDisconnected;
        public event QSenseDotNet.StreamPacketReceivedEventHandler? DownloadPacketReceived;
        public event EventHandler? DownloadDone;

        public Uart()
        {
            _device = new QSenseDotNet.Device();
            _device.DownloadPacketReceived += (s, e) => DownloadPacketReceived?.Invoke(this, e);
            _device.DownloadDone += (s, e) => DownloadDone?.Invoke(this, e);
            _device.InitializationDone += (s, e) => SensorConnected?.Invoke(this, new EventArgs());
        }

        public void Connect(string port)
        {
            if (port == "") return;
            this.parser = new SerialCommunication(port);
            parser.Disconnected += Parser_Disconnected;
            Status = Status.Usb;
            _device.Init(parser);
        }

        private void Parser_Disconnected(object sender, EventArgs e)
        {
            if (parser != null) parser.Disconnected -= Parser_Disconnected;
            parser = null;
        }

        public void Disconnect()
        {
            if (parser != null)
                parser.Dispose();
        }

        public void EnableBootloader()
        {
            parser?.EnableBootloader();
        }

        public void StartDownload()
        {
            _device.StartDownload();
        }
    }

    public enum Status
    {
        Idle = 0,
        Scanning,
        Connected,
        Usb
    }

    public class UartStatusReceivedEventArgs : EventArgs
    {
        public Status Status { get; set; }
        public Int32 MaxDataSize { get; set; }

        public UartStatusReceivedEventArgs(Status status, Int32 maxDataSize)
        {
            Status = status;
            MaxDataSize = maxDataSize;
        }
    }
}
