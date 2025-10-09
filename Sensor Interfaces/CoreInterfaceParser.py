import struct
from datetime import datetime, timedelta


class CoreInterfaceParser:
    class Opcode:
        Read = 1
        Data = 2
        Abort = 3
        Stream = 4

    class PacketFieldAddress:
        Opcode = 0
        Address = 1
        Length = 5
        Data = 7

    class MemoryAddress:
        WhoAmI = 0x00000000
        Id = 0x00000004
        MacAddress = 0x0000000C
        Version = 0x00000014
        Battery = 0x00000018
        MotionLevel = 0x00000019
        OffsetCompensated = 0x0000001A
        MagneticFieldMapped = 0x0000001B
        MagneticFieldProgress = 0x0000001C
        ConnectionInterval = 0x0000001D
        SyncStatus = 0x0000001E
        Ticks100Hz = 0x0000001F
        Pin = 0x00000020
        Time = 0x00000024
        Annotation = 0x00000028
        DeviceState = 0x0000002A
        UiAnimation = 0x0000002C
        DeviceName = 0x00000030
        DataMode = 0x0000003C
        Timesync = 0x0000003D
        AlgorithmSelection = 0x0000003F

    class DataMode:
        Mixed = 0
        Raw = 1
        Quat = 2
        Optimized = 3
        QuatMag = 4

    CONTROL_MEMORY_ADDRESS = 0x00000000
    CONTROL_MEMORY_SIZE = 0x00000040
    STREAM_MEMORY_ADDRESS = 0x00000100
    STREAM_MEMORY_SIZE = 237
    WHOAMI_VALUE = 0x324D5351
    PIN_VALUE = 0x65766F6C
    HEADER_LENGTH = 10

    @staticmethod
    def create_read_packet(address: int, length: int) -> bytes:
        packet = bytearray(7)
        packet[CoreInterfaceParser.PacketFieldAddress.Opcode] = CoreInterfaceParser.Opcode.Read
        packet[1:5] = struct.pack('<I', address)
        packet[5:7] = struct.pack('<H', length)
        return bytes(packet)

    @staticmethod
    def create_data_packet(address: int, data: bytes) -> bytes:
        length = len(data)
        packet = bytearray(7 + length)
        packet[CoreInterfaceParser.PacketFieldAddress.Opcode] = CoreInterfaceParser.Opcode.Read
        packet[1:5] = struct.pack('<I', address)
        packet[5:7] = struct.pack('<H', length)
        packet[7:7+length] = data
        return bytes(packet)

    @staticmethod
    def create_abort_packet() -> bytes:
        return bytes([CoreInterfaceParser.Opcode.Abort])

    @staticmethod
    def create_stream_packet() -> bytes:
        packet = bytearray(7)
        packet[CoreInterfaceParser.PacketFieldAddress.Opcode] = CoreInterfaceParser.Opcode.Stream
        packet[1:5] = struct.pack('<I', CoreInterfaceParser.STREAM_MEMORY_ADDRESS)
        packet[5:7] = struct.pack('<H', CoreInterfaceParser.STREAM_MEMORY_SIZE)
        return bytes(packet)

    @staticmethod
    def parse_packet(packet: bytes):
        opcode = packet[CoreInterfaceParser.PacketFieldAddress.Opcode]
        address = struct.unpack_from('<I', packet, CoreInterfaceParser.PacketFieldAddress.Address)[0]
        length = struct.unpack_from('<H', packet, CoreInterfaceParser.PacketFieldAddress.Length)[0]

        packet_info = f"Opcode: {opcode}\tAddress: {address}\tLength: {length}\r\n"
        print(packet_info)

        if address + length <= CoreInterfaceParser.CONTROL_MEMORY_SIZE:
            data = packet[CoreInterfaceParser.PacketFieldAddress.Data:CoreInterfaceParser.PacketFieldAddress.Data+length]
            CoreInterfaceParser._parse_control_memory(address, length, data)
        elif address == CoreInterfaceParser.STREAM_MEMORY_ADDRESS and length == CoreInterfaceParser.STREAM_MEMORY_SIZE:
            data = packet[CoreInterfaceParser.PacketFieldAddress.Data:CoreInterfaceParser.PacketFieldAddress.Data+length]
            CoreInterfaceParser._parse_stream_data(data)

    @staticmethod
    def _parse_control_memory(address: int, length: int, data: bytes):
        data_info = ""

        def get_u32(offset): return struct.unpack_from('<I', data, offset)[0]
        def get_u64(offset): return struct.unpack_from('<Q', data, offset)[0]

        if address <= CoreInterfaceParser.MemoryAddress.WhoAmI and address + length >= CoreInterfaceParser.MemoryAddress.WhoAmI + 4:
            data_info += f"WhoAmI: {get_u32(CoreInterfaceParser.MemoryAddress.WhoAmI - address)}\r\n"
        if address <= CoreInterfaceParser.MemoryAddress.Id and address + length >= CoreInterfaceParser.MemoryAddress.Id + 8:
            data_info += f"Id: {get_u64(CoreInterfaceParser.MemoryAddress.Id - address)}\r\n"
        if address <= CoreInterfaceParser.MemoryAddress.MacAddress and address + length >= CoreInterfaceParser.MemoryAddress.MacAddress + 8:
            data_info += f"Address: {get_u64(CoreInterfaceParser.MemoryAddress.MacAddress - address)}\r\n"
        if address <= CoreInterfaceParser.MemoryAddress.Version and address + length >= CoreInterfaceParser.MemoryAddress.Version + 4:
            v_major = data[CoreInterfaceParser.MemoryAddress.Version - address + 2]
            v_minor = data[CoreInterfaceParser.MemoryAddress.Version - address + 1]
            v_patch = data[CoreInterfaceParser.MemoryAddress.Version - address]
            data_info += f"Version: v{v_major}.{v_minor}.{v_patch}\r\n"

        # Battery: 1 byte at MemoryAddress.Battery (0x18)
        if address <= CoreInterfaceParser.MemoryAddress.Battery and address + length >= CoreInterfaceParser.MemoryAddress.Battery + 1:
            batt_val = data[CoreInterfaceParser.MemoryAddress.Battery - address]
            data_info += f"Battery: {batt_val}%\r\n"

        print(data_info)


    @staticmethod
    def _parse_stream_data(data: bytes):
            interference_levels = ["", "None", "Soft-iron interference", "Hard-iron interference", "Change of environment detected"]
            data_modes = ["Mixed", "Raw", "Quaternion", "Optimized", "Quat+Mag"]
            acc_ranges = ["2g", "16g", "4g", "8g"]
            gyro_ranges = ["250dps", "125dps", "500dps", "", "1000dps", "", "2000dps"]
            acc_scale_factors = [0.000061, 0.000488, 0.000122, 0.000244]
            gyr_scale_factors = [0.008750, 0.004375, 0.0175, 0.0, 0.035, 0.0, 0.07]

            mode = data[0] & 0x0F
            header = f"Stream Data Header:\r\nData Mode: {data_modes[int(mode)]}\r\n"
            buffering = data[0] >> 4
            header += f"Buffering: {buffering}\r\n"
            seconds = struct.unpack_from("<I", data, 1)[0]
            milliseconds = struct.unpack_from("<H", data, 5)[0] * 1.25
            timestamp = datetime(1970, 1, 1) + timedelta(seconds=seconds, milliseconds=milliseconds)
            header += f"Timestamp: {timestamp.strftime('%m/%d/%Y,%H:%M:%S.%f')}\r\n"
            header += f"Interference: {interference_levels[data[7] & 0x07]}\r\n"
            header += f"Battery: {(data[7] >> 3) * 6.25}\r\n"
            header += f"Annotation: {data[8]}\r\n"
            header += f"Sync. Status: {(data[9] & 0x01) == 1}\r\n"
            header += f"Accelerometer range: {acc_ranges[(data[9] & 0x30) >> 4]}\r\n"
            header += f"Gyroscope range: {gyro_ranges[(data[9] & 0x0E) >> 1]}\r\n"

            acc_scale = acc_scale_factors[(data[9] & 0x30) >> 4]
            gyr_scale = gyr_scale_factors[(data[9] & 0x0E) >> 1]

            payload = "Stream Data Payload\r\n"
            print(header)
            print(payload)

            if mode == CoreInterfaceParser.DataMode.Mixed:
                CoreInterfaceParser._parse_mixed_packet(data, buffering, acc_scale, gyr_scale)
            elif mode == CoreInterfaceParser.DataMode.Raw:
                CoreInterfaceParser._parse_raw_packet(data, buffering, acc_scale, gyr_scale)
            elif mode == CoreInterfaceParser.DataMode.Quat:
                CoreInterfaceParser._parse_quat_packet(data, buffering)
            elif mode == CoreInterfaceParser.DataMode.Optimized:
                CoreInterfaceParser._parse_optimized_packet(data, buffering, acc_scale, gyr_scale)
            elif mode == CoreInterfaceParser.DataMode.QuatMag:
                CoreInterfaceParser._parse_quat_mag_packet(data, buffering)

    @staticmethod
    def _parse_quat_mag_packet(data: bytes, buffering: int):
        payload = "Stream Data Payload\r\n"
        q0 = struct.unpack_from('<h', data, 10)[0] / 32768.0
        q1 = struct.unpack_from('<h', data, 12)[0] / 32768.0
        q2 = struct.unpack_from('<h', data, 14)[0] / 32768.0
        q3 = struct.unpack_from('<h', data, 16)[0] / 32768.0
        payload += f"Quaternion: {q0}, {q1}, {q2}, {q3}\r\n"

        magX = struct.unpack_from('<h', data, 18)[0] * Raw9Dof.MAG_SCALE
        magY = struct.unpack_from('<h', data, 20)[0] * Raw9Dof.MAG_SCALE
        magZ = struct.unpack_from('<h', data, 22)[0] * Raw9Dof.MAG_SCALE
        payload += f"Magnetometer: {magX}, {magY}, {magZ}\r\n"

        print(payload)


    @staticmethod
    def _parse_optimized_packet(data: bytes, buffering: int, acc_scale: float, gyr_scale: float):
        payload = "Stream Data Payload\r\n"
        q0 = struct.unpack_from('<h', data, 10)[0] / 32768.0
        q1 = struct.unpack_from('<h', data, 12)[0] / 32768.0
        q2 = struct.unpack_from('<h', data, 14)[0] / 32768.0
        q3 = struct.unpack_from('<h', data, 16)[0] / 32768.0
        payload += f"Quaternion: {q0}, {q1}, {q2}, {q3}\r\n"

        sample = Raw9Dof(data, 18, acc_scale, gyr_scale, False)
        payload += f"Accelerometer: {sample.AccX}, {sample.AccY}, {sample.AccZ}\r\n"
        payload += f"Gyroscope: {sample.GyrX}, {sample.GyrY}, {sample.GyrZ}\r\n"

        print(payload)

    @staticmethod
    def _parse_quat_packet(data: bytes, buffering: int):
        payload = "Stream Data Payload\r\n"
        # Use the same int16-to-float conversion your translation used earlier
        for i in range(buffering):
            idx = 10 + i*16
            q0 = struct.unpack_from('<h', data, idx)[0] / 32768.0
            q1 = struct.unpack_from('<h', data, idx+2)[0] / 32768.0
            q2 = struct.unpack_from('<h', data, idx+4)[0] / 32768.0
            q3 = struct.unpack_from('<h', data, idx+6)[0] / 32768.0
            payload += f"Quaternion[{i}]: {q0}, {q1}, {q2}, {q3}\r\n"
        print(payload)


    @staticmethod
    def _parse_raw_packet(data: bytes, buffering: int, acc_scale: float, gyr_scale: float):
        payload = "Stream Data Payload\r\n"
        # iterate over buffering samples using 18-bytes per sample (as in your translation)
        index = 10
        for j in range(buffering):
            sample = Raw9Dof(data, index, acc_scale, gyr_scale, True)
            payload += f"Sample[{j}] Acc: {sample.AccX}, {sample.AccY}, {sample.AccZ}\t"
            payload += f"Gyr: {sample.GyrX}, {sample.GyrY}, {sample.GyrZ}\t"
            payload += f"Mag: {sample.MagX}, {sample.MagY}, {sample.MagZ}\r\n"
            index += 18
        print(payload)


    @staticmethod
    def _parse_mixed_packet(data: bytes, buffering: int, acc_scale: float, gyr_scale: float):
        payload = "Stream Data Payload\r\n"
        # Keep the previous indexing used in your translation (first mixed sample at offset 10)
        sample0 = Raw9Dof(data, 10, acc_scale, gyr_scale, True)
        payload += f"Accelerometer0: {sample0.AccX}, {sample0.AccY}, {sample0.AccZ}\r\n"
        payload += f"Gyroscope0: {sample0.GyrX}, {sample0.GyrY}, {sample0.GyrZ}\r\n"
        payload += f"Magnetometer0: {sample0.MagX}, {sample0.MagY}, {sample0.MagZ}\r\n"
        q0 = struct.unpack_from('<h', data, 28)[0] / 32768.0
        q1 = struct.unpack_from('<h', data, 30)[0] / 32768.0
        q2 = struct.unpack_from('<h', data, 32)[0] / 32768.0
        q3 = struct.unpack_from('<h', data, 34)[0] / 32768.0
        payload += f"Quaternion: {q0}, {q1}, {q2}, {q3}\r\n"
        sample1 = Raw9Dof(data, 36, acc_scale, gyr_scale, False)
        payload += f"Accelerometer1: {sample1.AccX}, {sample1.AccY}, {sample1.AccZ}\r\n"
        payload += f"Gyroscope1: {sample1.GyrX}, {sample1.GyrY}, {sample1.GyrZ}\r\n"
        print(payload)



class Raw9Dof:
    MAG_SCALE = 0.0015

    def __init__(self, buffer: bytes, position: int, acc_scale=None, gyr_scale=None, include_mag=False):
        if acc_scale is not None and gyr_scale is not None:
            self.AccX = struct.unpack_from('<h', buffer, position + 0)[0] * acc_scale
            self.AccY = struct.unpack_from('<h', buffer, position + 2)[0] * acc_scale
            self.AccZ = struct.unpack_from('<h', buffer, position + 4)[0] * acc_scale
            self.GyrX = struct.unpack_from('<h', buffer, position + 6)[0] * gyr_scale
            self.GyrY = struct.unpack_from('<h', buffer, position + 8)[0] * gyr_scale
            self.GyrZ = struct.unpack_from('<h', buffer, position + 10)[0] * gyr_scale
            if include_mag:
                self.MagX = struct.unpack_from('<h', buffer, position + 12)[0] * Raw9Dof.MAG_SCALE
                self.MagY = struct.unpack_from('<h', buffer, position + 14)[0] * Raw9Dof.MAG_SCALE
                self.MagZ = struct.unpack_from('<h', buffer, position + 16)[0] * Raw9Dof.MAG_SCALE
        else:
            self.MagX = struct.unpack_from('<h', buffer, position + 0)[0] * Raw9Dof.MAG_SCALE
            self.MagY = struct.unpack_from('<h', buffer, position + 2)[0] * Raw9Dof.MAG_SCALE
            self.MagZ = struct.unpack_from('<h', buffer, position + 4)[0] * Raw9Dof.MAG_SCALE

parse_packet = CoreInterfaceParser.parse_packet
ParsePacket = CoreInterfaceParser.parse_packet