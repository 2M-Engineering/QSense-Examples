using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QSenseDotNet
{
    internal class Ble2M
    {
        private enum State
        {
            Idle = 0,
            Reading,
            Writing,
            Streaming,
            RelayReading,
        }

        private State state;
        private byte[] readBuffer = new byte[0];
        private UInt32 readAddress = 0;
        private UInt32 tmp_readAddress = 0;
        private Queue<BleQueueData> BleQueue = new Queue<BleQueueData>();
        private System.Timers.Timer timeoutTimer;

        internal bool Connected { get; set; } = false;
        internal int MaxPacketSize { get; set; } = (int)MemMap.MEM_MAP_CTRL_SIZE + 7;

        public event EventHandler<Ble2MTxEventArgs>? Ble2MTxEvent;
        internal event EventHandler<Ble2MDataEventArgs>? Ble2MDataEvent;
        internal event EventHandler<Ble2MRelayDataEventArgs>? Ble2MRelayDataEvent;
        internal event EventHandler<Ble2MDataEventArgs>? Ble2MWriteCompletEvent;

        internal Ble2M()
        {
            state = State.Idle;
            readBuffer = new byte[0];
            readAddress = 0;
            Connected = true;
            BleQueue = new Queue<BleQueueData>();
            timeoutTimer = new System.Timers.Timer { Interval = 5000 };
            timeoutTimer.AutoReset = false;
            timeoutTimer.Enabled = false;
            timeoutTimer.Elapsed += TimeoutTimer_Elapsed;
        }

        private void TimeoutTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
        {
            state = State.Idle;
            BleDequeue();
        }

        public bool IsBusy()
        {
            return state != State.Idle;
        }

        private void BleDequeue()
        {
            if (state == State.Idle && BleQueue.Count > 0)
            {
                BleQueueData qData = BleQueue.Dequeue();
                switch (qData.PacketType)
                {
                    case Packet.Opcode.Read:
                        ReadMemory(qData.Address, qData.Length);
                        break;
                    case Packet.Opcode.Data:
                        WriteMemory(qData.Address, qData.Data);
                        break;
                    case Packet.Opcode.Stream:
                        StreamMemory(qData.Address, qData.Length);
                        break;
                    case Packet.Opcode.Abort:
                        Abort();
                        break;
                    case Packet.Opcode.RelayRead:
                        RelayReadMemory(qData.Data[0], qData.Address, qData.Length);
                        break;
                    case Packet.Opcode.RelayWrite:
                        RelayWriteMemory(qData.Data[0], qData.Address, qData.Data.Skip(1).ToArray());
                        break;
                    default:
                        break;
                }
            }
        }

        internal void ReadMemory(UInt32 address, UInt16 length)
        {
            if (state == State.Streaming)
            {
                // ignore
            }
            else if (state != State.Idle)
            {
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.Read, address, Array.Empty<byte>(), length));
                timeoutTimer.Start();
            }
            else
            {
                state = State.Reading;
                Ble2MTxEventArgs args = new Ble2MTxEventArgs
                {
                    Packet = (new Packet(Packet.Opcode.Read, address, length, new byte[] { 0 })).ToArray()
                };
                readAddress = address;
                readBuffer = new byte[length];
                if (Ble2MTxEvent != null)
                    Ble2MTxEvent.Invoke(this, args);
            }
        }

        internal void StreamMemory(UInt32 address, UInt16 length)
        {
            if (state != State.Idle)
            {
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.Stream, address, Array.Empty<byte>(), length));
                timeoutTimer.Start();
            }
            else
            {
                state = State.Streaming;
                Ble2MTxEventArgs args = new Ble2MTxEventArgs
                {
                    Packet = (new Packet(Packet.Opcode.Stream, address, length, new byte[] { 0 })).ToArray()
                };
                BleQueue.Clear();
                readAddress = address;
                readBuffer = new byte[length];
                if (Ble2MTxEvent != null)
                    Ble2MTxEvent.Invoke(this, args);
            }
        }

        internal void Abort()
        {
            if (state != State.Streaming && state != State.Idle)
            {
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.Abort, 0, Array.Empty<byte>(), 0));
                timeoutTimer.Start();
                return;
            }
            state = State.Idle;
            Ble2MTxEventArgs args = new Ble2MTxEventArgs
            {
                Packet = (new Packet(Packet.Opcode.Abort, 0, 0, new byte[] { 0 })).ToArray()
            };
            readAddress = 0;
            readBuffer = new byte[0];
            if (Ble2MTxEvent != null)
                Ble2MTxEvent.Invoke(this, args);
        }

        internal void Hibernate()
        {
            Ble2MTxEventArgs args = new Ble2MTxEventArgs
            {
                Packet = (new Packet(Packet.Opcode.Hibernate, 0, 0, new byte[] { 0 })).ToArray()
            };
            readAddress = 0;
            readBuffer = new byte[0];
            if (Ble2MTxEvent != null)
                Ble2MTxEvent.Invoke(this, args);
        }

        internal void WriteMemory(UInt32 address, byte[] Data)
        {
            if (state == State.Streaming)
            {
                // ignore
            }
            else if (state != State.Idle)
            {
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.Data, address, Data, (ushort)Data.Length));
                timeoutTimer.Start();
            }
            else
            {
                state = State.Writing;
                readAddress = address;
                readBuffer = new byte[Data.Length];
                Array.Copy(Data, readBuffer, Data.Length);

                int length = Math.Min(MaxPacketSize - 7, readBuffer.Length);
                byte[] packetData = new byte[length];
                Array.Copy(readBuffer, 0, packetData, 0, packetData.Length);

                Ble2MTxEventArgs args = new Ble2MTxEventArgs
                {
                    Packet = (new Packet(Packet.Opcode.Data, readAddress, (UInt16)packetData.Length, packetData)).ToArray()
                };
                if (Ble2MTxEvent != null)
                    Ble2MTxEvent.Invoke(this, args);
            }
        }

        internal void WriteMemoryWithoutResponse(UInt32 address, byte[] Data)
        {
            int length = Math.Min(MaxPacketSize - 7, Data.Length);
            byte[] packetData = new byte[length];
            Array.Copy(Data, 0, packetData, 0, packetData.Length);
            Ble2MTxEventArgs args = new Ble2MTxEventArgs
            {
                Packet = (new Packet(Packet.Opcode.DataWithoutResponse, address, (ushort)packetData.Length, packetData)).ToArray()
            };
            if (Ble2MTxEvent != null)
                Ble2MTxEvent.Invoke(this, args);
        }

        internal void RelayWriteMemory(byte handle, UInt32 address, byte[] Data, bool queue = false)
        {
            if (state != State.Idle || queue)
            {
                List<byte> data = new List<byte> { handle };
                data.AddRange(Data);
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.RelayWrite, address, data.ToArray(), (ushort)Data.Length));
            }
            else
            {
                byte[] HandleData = new byte[Data.Length + 1];
                Array.Copy(Data, 0, HandleData, 1, Data.Length);
                HandleData[0] = handle;

                int length = Math.Min(MaxPacketSize - 7, HandleData.Length);
                byte[] packetData = new byte[HandleData.Length];
                Array.Copy(HandleData, 0, packetData, 0, packetData.Length);
                Ble2MTxEventArgs args = new Ble2MTxEventArgs
                {
                    Packet = (new Packet(Packet.Opcode.RelayWrite, address, (ushort)packetData.Length, packetData)).ToArray()
                };
                if (Ble2MTxEvent != null)
                    Ble2MTxEvent.Invoke(this, args);
            }
        }

        internal void RelayReadMemory(byte handle, UInt32 address, UInt16 length, bool queue = false)
        {
            if (state != State.Idle || queue)
            {
                BleQueue.Enqueue(new BleQueueData(Packet.Opcode.RelayRead, address, new byte[] { handle }, length));
            }
            else
            {
                if (handle == 255)
                {
                    tmp_readAddress = address;
                }
                else
                {
                    tmp_readAddress = 0;
                }
                timeoutTimer.Start();
                state = State.RelayReading;
                readAddress = address;
                readBuffer = new byte[length + 1];
                Ble2MTxEventArgs args = new Ble2MTxEventArgs
                {
                    Packet = (new Packet(Packet.Opcode.RelayRead, address, (ushort)(length + 1), new byte[] { handle })).ToArray()
                };
                if (Ble2MTxEvent != null)
                    Ble2MTxEvent.Invoke(this, args);
            }
        }

        internal void Ble2MRxEvent(byte[] Data)
        {
            Packet rxPacket = new Packet(Data);
            if (rxPacket.Data.Length < rxPacket.Length)
            {
                return;
            }
            int extraByte = rxPacket.Type == Packet.Opcode.RelayRead || rxPacket.Type == Packet.Opcode.RelayWrite ? 1 : 0;
            if ((rxPacket.Address == MemMap.MEM_MAP_CONF_ADDR || rxPacket.Address == MemMap.MEM_MAP_DATA_ADDR) && rxPacket.Address > readAddress)
            {
                // We have received a stream packet when we don't expect it
                return;
            }
            else if (rxPacket.Address < readAddress || rxPacket.Address + rxPacket.Length > readAddress + readBuffer.Length + extraByte)
            {
                throw new Exception("Corrupt packet received");
            }

            timeoutTimer.Stop();
            bool wasBroadcasted = false;
            State tmp_state = state;
            uint tmp_address = readAddress;
            if (rxPacket.Type == Packet.Opcode.RelayWrite && state != State.RelayReading)
            {
                wasBroadcasted = true;
                state = State.RelayReading;
                readAddress = tmp_readAddress;

            }

            switch (state)
            {
                case State.Reading:
                    Array.Copy(rxPacket.Data, 0, readBuffer, rxPacket.Address - readAddress, rxPacket.Length);

                    if (rxPacket.Address + rxPacket.Length == readAddress + readBuffer.Length)
                    {
                        Ble2MDataEventArgs args = new Ble2MDataEventArgs();
                        args.Address = readAddress;
                        args.Data = readBuffer;
                        Ble2MDataEvent?.Invoke(this, args);
                        state = State.Idle;
                    }
                    break;
                case State.RelayReading:
                    Array.Copy(rxPacket.Data, 0, readBuffer, rxPacket.Address - readAddress, rxPacket.Length);
                    if (rxPacket.Address + rxPacket.Length == readAddress + readBuffer.Length)
                    {
                        Ble2MRelayDataEventArgs args = new Ble2MRelayDataEventArgs();
                        args.Address = readAddress;
                        args.Handle = readBuffer[0];
                        args.Data = readBuffer.Skip(1).ToArray();
                        Ble2MRelayDataEvent?.Invoke(this, args);
                        state = State.Idle;
                    }
                    break;
                case State.Writing:
                    if (rxPacket.Address + rxPacket.Length == readAddress + readBuffer.Length)
                    {
                        Ble2MDataEventArgs args = new Ble2MDataEventArgs();
                        args.Address = readAddress;
                        args.Data = readBuffer;
                        Ble2MWriteCompletEvent?.Invoke(this, args);
                        state = State.Idle;
                    }
                    else
                    {
                        uint index = (uint)(rxPacket.Address + rxPacket.Length - readAddress);
                        int length = (int)Math.Min(MaxPacketSize - 7, readBuffer.Length - index);
                        byte[] packetData = new byte[length];
                        Array.Copy(readBuffer, index, packetData, 0, length);

                        Ble2MTxEventArgs args = new Ble2MTxEventArgs();
                        args.Packet = (new Packet(Packet.Opcode.Data, readAddress + index, (ushort)packetData.Length, packetData)).ToArray();
                        Ble2MTxEvent?.Invoke(this, args);
                    }
                    break;

                case State.Streaming:
                    if ((rxPacket.Address == MemMap.MEM_MAP_CONF_ADDR || rxPacket.Address == MemMap.MEM_MAP_DATA_ADDR))
                    {
                        Array.Copy(rxPacket.Data, 0, readBuffer, (int)(rxPacket.Address - readAddress), rxPacket.Length);

                        if (rxPacket.Address + rxPacket.Length == readAddress + readBuffer.Length)
                        {
                            Ble2MDataEventArgs args = new Ble2MDataEventArgs
                            {
                                Address = readAddress,
                                Data = readBuffer
                            };
                            Ble2MDataEvent?.Invoke(this, args);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Corrupt BLE Packet");
                    }
                    break;
            }

            if (wasBroadcasted)
            {
                state = tmp_state;
                readAddress = tmp_address;
            }

            BleDequeue();

            if (state != State.Idle && state != State.Streaming) timeoutTimer.Enabled = true;
        }
    }

    internal class Ble2MTxEventArgs : EventArgs
    {
        public byte[] Packet { get; set; } = new byte[0];
    }

    internal class Ble2MDataEventArgs : EventArgs
    {
        public UInt32 Address { get; set; }
        public byte[] Data { get; set; } = new byte[0];
    }

    internal class Ble2MRelayDataEventArgs : EventArgs
    {
        public int Handle { get; set; }
        public UInt32 Address { get; set; }
        public byte[] Data { get; set; } = new byte[0];
    }

    internal class BleQueueData
    {
        public Packet.Opcode PacketType { get; set; }
        public uint Address { get; set; }
        public byte[] Data { get; set; }
        public ushort Length { get; set; }

        public BleQueueData(Packet.Opcode packetType, uint address, byte[] data, ushort length)
        {
            PacketType = packetType;
            Address = address;
            Data = data;
            Length = length;
        }
    }
}