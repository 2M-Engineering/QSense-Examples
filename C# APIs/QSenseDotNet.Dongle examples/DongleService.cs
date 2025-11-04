using QSenseDotNet;
using QSenseDotNet.Dongle;

namespace QSenseExamples
{
    public class DongleService
    {
        private int numberDevices = 0;
        private Dongle? Dongle;
        public DongleService()
        {

        }
        public void ConnectDongle(string port)
        {
            try
            {
                Dongle = new Dongle(port);
                Dongle.SensorConnected += Dongle_SensorConnected;
                Dongle.SensorDisconnected += Dongle_SensorDisconnected;
                Dongle.CommunicationError += Dongle_CommunicationError;
                Dongle.DeviceCommunicationError += Dongle_DeviceCommunicationError;
                Dongle.DongleScanStopped += Dongle_DongleScanStopped;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void StartScanning()
        {
            if (Dongle is not null) Dongle.StartScanning(); 
        }

        public void StartWhitelistScan(string[] serialNumberWhitelist)
        {
            if (Dongle is not null) Dongle.ConnectWhitelist(serialNumberWhitelist);
        }

        public void StopScanning()
        {
            if (Dongle is not null) Dongle.StopScanning();
        }

        public void Disconnect()
        {

            if (Dongle is not null)
            {
                Dongle.SensorConnected -= Dongle_SensorConnected;
                Dongle.SensorDisconnected -= Dongle_SensorDisconnected;
                Dongle.CommunicationError -= Dongle_CommunicationError;
                Dongle.DeviceCommunicationError -= Dongle_DeviceCommunicationError;
                Dongle.Disconnect();
            }
        }

        public void StartOffsetCompensation()
        {
            if (Dongle is not null) 
                for (int i = 0; i < Dongle.Devices.Length; i++) 
                    if (Dongle.Devices[i].IsConnected) Dongle.Devices[i].StartOffsetCompensation();
        }

        public void StartMagFieldMapping()
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                {
                    if (!Dongle.Devices[i].IsConnected) continue;
                    Dongle.Devices[i].StateReceived += Device_StateReceived;
                    Dongle.Devices[i].StartMagFieldMapping();
                }
            }
        }

        public void StopMagFieldMapping()
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                {
                    if (!Dongle.Devices[i].IsConnected) continue;
                    Dongle.Devices[i].StateReceived -= Device_StateReceived;
                    Dongle.Devices[i].StopMagFieldMapping();
                }
            }
        }

        public void StartStreaming()
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                {
                    if (!Dongle.Devices[i].IsConnected) continue;
                    Dongle.Devices[i].StreamPacketReceived += DongleService_StreamPacketReceived;
                    Dongle.Devices[i].StartStreaming();
                }
            }
        }

        public void StopStreaming()
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                {
                    if (!Dongle.Devices[i].IsConnected) continue;
                    Dongle.Devices[i].StreamPacketReceived -= DongleService_StreamPacketReceived;
                    Dongle.Devices[i].StopStreaming();
                }
            }
        }

        private void DongleService_StreamPacketReceived(object sender, StreamPacketReceivedEventArgs e)
        {
            string logMessage = $"Stream packet received";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Device_StateReceived(object sender, QSenseDotNet.StateReceivedEventArgs e)
        {
            string logMessage = "";
            float progress = 0.0f;
            for (int i = 0; i < Dongle.Devices.Length; ++i) progress += Dongle.Devices[i].MagFieldMappingProgress;
            progress /= numberDevices;
            if (progress == 100.0f)
            {
                logMessage = "Magnetic Field Map successfully completed";
            }
            if (progress == 110.0f)
            {
                logMessage = "Magnetic Field Map timed out";
            }
            else
            {
                logMessage = $"Magnetic Field Map progress: {progress}%";
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }



        private void Dongle_DongleScanStopped(object sender, EventArgs e)
        {
            string logMessage = "Dongle scan stopped";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Dongle_CommunicationError(object sender, EventArgs e)
        {
            string logMessage = "Dongle communication error detected";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Dongle_DeviceCommunicationError(object sender, int e)
        {
            string logMessage = $"Communication error with sensor at index {e} detected";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Dongle_SensorDisconnected(object sender, ConnectionEventArgs e)
        {
            numberDevices--;
            string logMessage = $"Sensor at index {e} disconnected";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Dongle_SensorConnected(object sender, ConnectionEventArgs e)
        {
            numberDevices++;
            string logMessage = $"Sensor at index {e} connected";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
        }

        private void Device_BatteryReceived(object sender, BatteryReceivedEventArgs e)
        {
            int i = 0;
            while (i < 13 && Dongle.Devices[i] != sender) { i++; };
            if (i != 13)
            {
                string logMessage = $"[Sensor {i}] Battery level: {e}";
#if DEBUG
                System.Diagnostics.Debug.WriteLine(logMessage);
#else
            Console.WriteLine(logMessage);
#endif
            }
        }

        public void SetDataMode(DataMode dataMode)
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                    if (Dongle.Devices[i].IsConnected) Dongle.Devices[i].SetDataMode(dataMode);
            }
        }

        public void SetDeviceConfig(SensitivityAcc accSens, SensitivityGyr gyrSens, SamplingRate samplRate, int buffering)
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                    if (Dongle.Devices[i].IsConnected) Dongle.Devices[i].SetSensorConfig(accSens, gyrSens, samplRate, buffering);
            }
        }

        public void SetLEDAnimation(Int32 index, byte r, byte g, byte b, LEDAnimation animation)
        {
            if (Dongle is not null) 
                if (Dongle.Devices[index].IsConnected) 
                    Dongle.Devices[index].SetLEDAnimation(r, g, b, animation);
        }

        public void SetAlgorithm(Algorithms val)
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                    if (Dongle.Devices[i].IsConnected) Dongle.Devices[i].SetAlgorithm(val);
            }
        }

        public void DisbaleTimeSync()
        {
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                    if (Dongle.Devices[i].IsConnected) Dongle.Devices[i].StopSync();
            }
        }

        public void EnableTimeSync(byte networkKey)
        {
            bool first = true;
            if (Dongle is not null)
            {
                for (int i = 0; i < Dongle.Devices.Length; i++)
                {
                    if (Dongle.Devices[i].IsConnected)
                    {
                        bool isMaster = first;
                        Dongle.Devices[i].StartSync(networkKey, isMaster);
                        if (first) first = false;
                    }
                }
            }
        }
    }
}
