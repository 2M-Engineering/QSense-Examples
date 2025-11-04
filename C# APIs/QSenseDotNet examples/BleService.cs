using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using QSenseDotNet;
using Plugin.BLE.Abstractions;
using System.Diagnostics;

namespace QSenseExamples
{
    public class BleService
    {
        private const string ServiceID          = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        private const string RxCharacteristicID = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        private const string TxCharacteristicID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";
        private const string NAME_PERIPHERAL    = "qsense";

        private readonly IBluetoothLE _bluetoothManager;
        private IAdapter Adapter;
        private ICharacteristic rxCharacteristic = null;
        private ICharacteristic txCharacteristic = null;
        private CancellationTokenSource _scanCancellationTokenSource = null;
        private CancellationTokenSource _connectCancellationTokenSource = null;
        private IDevice discoveredDevice;
        private QSenseDotNet.Device device;

        public BleService()
        {
            _bluetoothManager = CrossBluetoothLE.Current;
            Adapter = _bluetoothManager?.Adapter;
            Adapter.ScanTimeout = 1000;
            Adapter.ScanMode = ScanMode.LowLatency;
            Adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
        }

        public async Task StartScanning()
        {
            _scanCancellationTokenSource = new CancellationTokenSource();

            if (_bluetoothManager.State != BluetoothState.On) return;

            Adapter.StartScanningForDevicesAsync(
                new Guid[0],
                (device) => device is not null && device.Name is not null && device.Name.ToLower().Equals(NAME_PERIPHERAL),
                false,
                _scanCancellationTokenSource.Token);
        }

        private async void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {
            discoveredDevice = Adapter.DiscoveredDevices[0];
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _connectCancellationTokenSource = new CancellationTokenSource();
                await Adapter.ConnectToDeviceAsync(discoveredDevice, new ConnectParameters(forceBleTransport: false), _connectCancellationTokenSource.Token);
                Adapter_DeviceConnected(Adapter, new DeviceEventArgs() { Device = discoveredDevice });
            });
        }

        public async void Disconnect()
        {
            try
            {
                await Adapter.DisconnectDeviceAsync(Adapter.ConnectedDevices[0]);
                device = null;
                discoveredDevice = null;
            }
            catch (Exception ex) { }
        }
        private async void Adapter_DeviceConnected(object sender, DeviceEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _connectCancellationTokenSource.Dispose();
                _connectCancellationTokenSource = null;
                BleParserService parser = null;

                try
                {
                    var service = await discoveredDevice.GetServiceAsync(Guid.Parse(ServiceID));
                    var characteristics = await service.GetCharacteristicsAsync();
                    rxCharacteristic = characteristics.FirstOrDefault(x => x.Id == Guid.Parse(RxCharacteristicID));
                    txCharacteristic = characteristics.FirstOrDefault(x => x.Id == Guid.Parse(TxCharacteristicID));
                    parser = new BleParserService(rxCharacteristic, txCharacteristic);
                    await parser.StartUpdatesAsync();
                }
                catch (Exception ex)
                {
                    return;
                }

                discoveredDevice.UpdateConnectionInterval(ConnectionInterval.High);
                try
                {
                    device = new QSenseDotNet.Device();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                device.Init(parser);
                device.InitializationDone += Device_InitializationDone;
            });
        }

        private void Device_InitializationDone(object sender, string e)
        {
            string logMessage = $"QSense sensor connected:\r\nSerial Number: {device.SerialNumber}\r\nVersion: {device.Version}\r\nDevice Name: {device.Name}\r\nBattery: {device.Battery}%";
            device.BatteryReceived += Device_BatteryReceived;
            device.MotionLevelReceived += Device_MotionLevelReceived;
            device.DeviceNameChanged += Device_DeviceNameChanged;
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Device_BatteryReceived(object sender, BatteryReceivedEventArgs e)
        {
            string logMessage = $"Battery: {e.Battery}%";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Device_DeviceNameChanged(object sender, DeviceNameChangedEventArgs e)
        {
            string logMessage = $"Device name changed: {e.Name}%";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Device_MotionLevelReceived(object sender, MotionLevelReceivedEventArgs e)
        {
            string logMessage = $"Motion level changed: {e.MotionLevel}%";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        public void ReadMemory()
        {
            device.ReadMemory();
        }

        public void StartStreaming()
        {
            device.StartStreaming(); 
            device.StreamPacketReceived += Device_StreamPacketReceived;
        }

        public void StopStreaming()
        {
            device.StreamPacketReceived -= Device_StreamPacketReceived;
            device.StopStreaming();
        }

        private void Device_StreamPacketReceived(object sender, StreamPacketReceivedEventArgs e)
        {
            string logMessage = "Stream packet received";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        public void StartOffsetCompensation()
        {
            device.StartOffsetCompensation();
        }

        public void StartMagFieldMapping()
        {
            device.StartMagFieldMapping();
            device.StateReceived += Device_StateReceived;
        }

        public void StopMagFieldMapping()
        {
            device.StateReceived -= Device_StateReceived;
            device.StopMagFieldMapping();
        }

        private void Device_StateReceived(object sender, StateReceivedEventArgs e)
        {
            if (device.MagFieldMappingProgress == 100)
            {
                string logMessage = "Magnetic Field Map successfully completed";
                device.StateReceived -= Device_StateReceived;
#if DEBUG
                System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
            }
            else if (device.MagFieldMappingProgress > 100)
            {
                string logMessage = "Magnetic Field Map timed out";
                device.StateReceived -= Device_StateReceived;
#if DEBUG
                System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
            }
            else
            {
                string logMessage = $"Magnetic Field Map progress: {device.MagFieldMappingProgress}%";
#if DEBUG
                System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
                Task.Delay(1000).Wait();
                ReadMemory();
            }
        }

        public void SetDeviceConfig(SensitivityAcc accSens, SensitivityGyr gyrSens, SamplingRate sampRate, int buffering)
        {
            device.SetSensorConfig(accSens, gyrSens, sampRate, buffering);
        }

        public void SetDataMode(DataMode mode)
        {
            device.SetDataMode(mode);
        }

        public void SelectAlgorithm(Algorithms value)
        {
            device.SetAlgorithm(value);
        }

        public void FlashGreenLight()
        {
            device.SetLEDAnimation(0, 0, 255, LEDAnimation.Blinking); 
        }

        public void DisableTimeSync()
        {
            device.StopSync();
        }

        public void EnableTimeSyncAsMaster(byte networkKey)
        {
            // The network key can take any value between and including 1 to 127
            device.StartSync(networkKey, true);
        }
        public void EnableTimeSyncAsSlave(byte networkKey)
        {
            // The network key can take any value between and including 1 to 127
            device.StartSync(networkKey, false);
        }
    }
}
