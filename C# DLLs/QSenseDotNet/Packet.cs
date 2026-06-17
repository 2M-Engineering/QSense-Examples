using System;

namespace QSenseDotNet
{
    internal class Packet
    {
        internal enum Opcode
        {
            NotUsed = 0,
            Read,
            Data,
            Abort,
            Stream,
            Hibernate = 0xFF
        }

        internal Opcode Type { get; set; }
        internal UInt32 Address { get; set; }
        internal UInt16 Length { get; set; }
        internal Byte[] Data { get; set; }

        internal Packet(Opcode type, UInt32 address, UInt16 length, Byte[] data)
        {
            Type = type;
            Address = address;
            Length = length;
            Data = new Byte[data.Length];
            Array.Copy(data, 0, Data, 0, Data.Length);
        }

        internal Packet(Byte[] array)
        {
            Type = (Opcode)array[0];
            if (Type == Opcode.Stream)
            {
                Address = MemMap.MEM_MAP_CONF_ADDR;
                Length = BitConverter.ToUInt16(array, 1);
                Data = new Byte[array.Length - 3];
                Array.Copy(array, 3, Data, 0, Data.Length);
            }
            else
            {
                Address = BitConverter.ToUInt32(array, 1);
                Length = BitConverter.ToUInt16(array, 5);
                Data = new Byte[array.Length - 7];
                Array.Copy(array, 7, Data, 0, Data.Length);
            }
        }

        internal Byte[] ToArray()
        {
            Byte[] result = new Byte[Data.Length + 7];
            result[0] = (Byte)Type;
            Array.Copy(BitConverter.GetBytes(Address), 0, result, 1, 4);
            Array.Copy(BitConverter.GetBytes(Length), 0, result, 5, 2);
            Array.Copy(Data, 0, result, 7, Data.Length);
            return result;
        }
    }
}