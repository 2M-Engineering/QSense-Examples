using System.Collections.Generic;
using System.Diagnostics;

namespace QSenseDotNet
{
    public static class Utilities
    {
        /// <summary>
        /// Converts a string of hex values into a byte array.
        /// </summary>
        /// <param name="hexString">The string that will be converted into a byte array.</param>
        /// <returns>A byte array.</returns>
        public static byte[] HexToByteArray(string hexString)
        {
            byte hi_nible = 0;
            List<byte> buffer = new List<byte>();
            for (int i = 0; i < hexString.Length; i++)
            {
                if (ValidHex(hexString[i]))
                {
                    if (i % 2 == 0) hi_nible = (byte)hexString[i];
                    else buffer.Add(GetValFromHexChars(hi_nible, (byte)hexString[i]));
                }
                else
                {
                    Debug.WriteLine("Corrupt packet");
                    return new byte[0];
                }
            }
            return buffer.ToArray();
        }

        private static bool ValidHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        private static byte GetValFromHexChars(byte hi_nible, byte lo_nible)
        {
            int result = 0;
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
