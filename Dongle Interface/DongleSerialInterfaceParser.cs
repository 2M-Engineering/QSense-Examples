using System.Text;

namespace QSenseExamples
{
    public static class DongleSerialInterfaceParser
    {
        private const Int32 MAX_NODES = 13;
        private const string DEVICE_NAME = "QSense\0";
        private const char PREFIX = '$';
        private const char POSTFIX = '\n';
        private const char OPCODE_TRANSMIT = 'T';
        private const char OPCODE_RECEIVE = 'R';
        private const char OPCODE_CONNECT = 'C';
        private const char OPCODE_CONNECT_WHITELIST = 'W';
        private const char OPCODE_DISCONNECT = 'D';
        private const char OPCODE_STOPSCAN = 'I';
        private const char OPCODE_STATUS = 'S';

        public static string ByteArrayToString(byte[] data)
        {
            string hexString = "";
            foreach (byte b in data) hexString += b.ToString("X2");
            return hexString;
        }

        public static string CreateTransmitPacket(byte[] coreInterfacePacket, int handle)
        {
            string hexString = ByteArrayToString(coreInterfacePacket);
            return "" + PREFIX + OPCODE_TRANSMIT + handle.ToString("X2") + hexString + POSTFIX;
        }

        public static string CreateConnectPacket()
        {
            string packet = "" + PREFIX + OPCODE_CONNECT;
            packet += MAX_NODES.ToString("X2");
            byte[] buffer = ASCIIEncoding.ASCII.GetBytes(DEVICE_NAME);
            foreach (byte b in buffer) packet += b.ToString("X2");
            return packet;
        }

        public static string CreateConnectWhitelistPacket(string[] serialNumbers)
        {
            string packet = "" + PREFIX + OPCODE_CONNECT_WHITELIST;
            packet += (serialNumbers.Length).ToString("X2");
            UInt64[] macAddresses = serialNumbers.Select(sn => Convert.ToUInt64(sn.Split("-")[0], 16)).ToArray();
            foreach (UInt64 macAddress in macAddresses)
            {
                byte[] buffer = new byte[6];
                Array.Copy(BitConverter.GetBytes(macAddress), 0, buffer, 0, 6);
                foreach (byte b in buffer) packet += b.ToString("X2");
            }
            packet += POSTFIX;
            return packet;
        }

        public static string CreateDisconnectPacket()
        {
            return "" + PREFIX + OPCODE_DISCONNECT + POSTFIX;
        }

        public static string CreateStopScanPacket()
        {
            return "" + PREFIX + OPCODE_STOPSCAN + POSTFIX;
        }

        public static string CreateStatusRequest()
        {
            return "" + PREFIX + OPCODE_STATUS + POSTFIX;
        }

        private static void ParsePacket(string packet)
        {
            Int32 packetIndex = 0;
            char opcode = '\0';
            byte hi_nible = 0;
            List<byte> dataBuffer = new List<byte>();
            foreach (char c in packet)
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
                                string packetInfo = "Receive Packet\r\n";
                                packetInfo += $"Handle: {dataBuffer[0]}\r\nData: ";
                                for (int i = 1; i < dataBuffer.Count; i++) packetInfo += dataBuffer[i].ToString("X2");
                                packetInfo += "\r\n";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(packetInfo);
#else
                                Console.WriteLine(packetInfo);
#endif
                                byte[] coreInterfacePacket = new byte[dataBuffer.Count - 1];
                                Array.Copy(dataBuffer.ToArray(), 1, coreInterfacePacket, 0, dataBuffer.Count - 1);
                                QSenseExamples.CoreInterfaceParser.ParsePacket();
                                break;
                            case OPCODE_STATUS:
                                string[] statusValues = new string[] { "Idle", "Scanning", "Connected" };
                                string statusInfo = "Status Packet\r\n";
                                statusInfo += $"Max. connections: {dataBuffer[0]}";
                                for (int i = 1; i < dataBuffer.Count; i++)
                                {
                                    statusInfo += $"Handle {i}: {statusValues[dataBuffer[i]]}";
                                }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(statusInfo);
#else
                                Console.WriteLine(statusInfo);
#endif
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
    }
}