using System;

namespace QSenseDotNet.Uart
{
    public delegate void ConnectionEventHandler(object sender, EventArgs e);

    public class Uart
    {
        private const string QSENSE_DESCRIPTION = "QSense";
        private const string HUB_DESCRIPTION = "QSenseHub";
        #region Fields
        private QSenseDotNet.Device _device;
        private SerialCommunication? parser;
        #endregion

        public QSenseDotNet.Device Device { get { return _device; } }
        public string Comport { get { return parser.Comport; } }

        public event EventHandler<string>? SensorConnected;
        public event EventHandler<string>? SensorDisconnected;

        public Uart(string name)
        {
            if (name.Equals(QSENSE_DESCRIPTION)) _device = new QSenseDotNet.Sensor();
            else if (name.Equals(HUB_DESCRIPTION)) _device = new QSenseDotNet.Hub();
            _device.InitializationDone += (s, e) => SensorConnected?.Invoke(this, e);
        }

        public void Connect(string port)
        {
            if (port == "") return;
            this.parser = new SerialCommunication(port);
            parser.Disconnected += Parser_Disconnected;
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
        public void Reboot()
        {
            parser?.Reboot();
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
