using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using QSenseDotNet;
using System.Diagnostics;

namespace QSenseExamples
{
    public class BleParserService : ICommunication
    {
        private List<byte> dataBuffer = new List<byte>();

        internal ICharacteristic RxCharacteristic { get; private set; }
        internal ICharacteristic TxCharacteristic { get; private set; }
        internal byte[] RxCharacteristicValue => RxCharacteristic.Value;
        internal byte[] TxCharacteristicValue => TxCharacteristic.Value;

        public event QSenseDotNet.DataReceivedEventHandler? DataReceived;

        public void Write(string data)
        {
            try
            {
                byte[] buffer = QSenseDotNet.Utilities.HexToByteArray(data);
                MainThread.BeginInvokeOnMainThread(async () => { try { await RxCharacteristic.WriteAsync(buffer); } catch { } });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        internal BleParserService(ICharacteristic rxCharacteristic, ICharacteristic txCharacteristic)
        {
            RxCharacteristic = rxCharacteristic;
            TxCharacteristic = txCharacteristic;
            TxCharacteristic.ValueUpdated -= OnDataReceived;
            TxCharacteristic.ValueUpdated += OnDataReceived;
        }

        internal async Task StartUpdatesAsync()
        {
            await TxCharacteristic.StartUpdatesAsync();
        }

        internal void StopUpdatesAsync()
        {
            TxCharacteristic.ValueUpdated -= OnDataReceived;
            TxCharacteristic.StopUpdatesAsync();
        }

        private void OnDataReceived(object sender, CharacteristicUpdatedEventArgs e)
        {
            byte[] segment = TxCharacteristicValue;
            ParseData(segment);
        }

        private void ParseData(byte[] segment)
        {
            string packet = "";
            foreach (byte b in segment) packet += b.ToString("X2");
            QSenseDotNet.DataReceivedEventArgs dataArgs = new QSenseDotNet.DataReceivedEventArgs(packet);
            MainThread.BeginInvokeOnMainThread(() => DataReceived?.Invoke(this, dataArgs));
            dataBuffer.Clear();
        }

        ~BleParserService()
        {
            try
            {
                TxCharacteristic.ValueUpdated -= OnDataReceived;
                TxCharacteristic.StopUpdatesAsync();
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
