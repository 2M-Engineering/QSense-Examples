import CoreInterfaceParser
class DongleSerialInterfaceParser:
    MAX_NODES = 13
    DEVICE_NAME = "QSense\0"
    PREFIX = "$"
    POSTFIX = "\n"

    OPCODE_TRANSMIT = "T"
    OPCODE_RECEIVE = "R"
    OPCODE_CONNECT = "C"
    OPCODE_CONNECT_WHITELIST = "W"
    OPCODE_DISCONNECT = "D"
    OPCODE_STOPSCAN = "I"
    OPCODE_STATUS = "S"

    @staticmethod
    def byte_array_to_string(data: bytes) -> str:
        return data.hex().upper()

    @staticmethod
    def create_transmit_packet(core_interface_packet: bytes, handle: int) -> str:
        hex_string = DongleSerialInterfaceParser.byte_array_to_string(core_interface_packet)
        return f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_TRANSMIT}{handle:02X}{hex_string}{DongleSerialInterfaceParser.POSTFIX}"

    @staticmethod
    def create_connect_packet() -> str:
        packet = f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_CONNECT}"
        packet += f"{DongleSerialInterfaceParser.MAX_NODES:02X}"
        buffer = DongleSerialInterfaceParser.DEVICE_NAME.encode("ascii")
        packet += buffer.hex().upper()
        return packet + DongleSerialInterfaceParser.POSTFIX

    @staticmethod
    def create_connect_whitelist_packet(serial_numbers: list[str]) -> str:
        packet = f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_CONNECT_WHITELIST}"
        packet += f"{len(serial_numbers):02X}"
        for sn in serial_numbers:
            mac_address = int(sn.split("-")[0], 16)
            buffer = mac_address.to_bytes(8, "little")[:6] 
            packet += buffer.hex().upper()
        return packet + DongleSerialInterfaceParser.POSTFIX

    @staticmethod
    def create_disconnect_packet() -> str:
        return f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_DISCONNECT}{DongleSerialInterfaceParser.POSTFIX}"

    @staticmethod
    def create_stop_scan_packet() -> str:
        return f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_STOPSCAN}{DongleSerialInterfaceParser.POSTFIX}"

    @staticmethod
    def create_status_request() -> str:
        return f"{DongleSerialInterfaceParser.PREFIX}{DongleSerialInterfaceParser.OPCODE_STATUS}{DongleSerialInterfaceParser.POSTFIX}"

    @staticmethod
    def parse_packet(packet: str):
        packet_index = 0
        opcode = None
        hi_nibble = 0
        data_buffer: list[int] = []

        for c in packet:
            if c == DongleSerialInterfaceParser.PREFIX:
                packet_index = 0
            else:
                packet_index += 1

            if packet_index == 1:
                opcode = c
                data_buffer.clear()

            if packet_index > 1:
                if c == DongleSerialInterfaceParser.POSTFIX:
                    if opcode == DongleSerialInterfaceParser.OPCODE_RECEIVE:
                        packet_info = "Receive Packet\n"
                        packet_info += f"Handle: {data_buffer[0]}\nData: "
                        packet_info += "".join(f"{b:02X}" for b in data_buffer[1:])
                        print(packet_info)

                        core_interface_packet = bytes(data_buffer[1:])
                        CoreInterfaceParser.parse_packet(core_interface_packet)

                    elif opcode == DongleSerialInterfaceParser.OPCODE_STATUS:
                        status_values = ["Idle", "Scanning", "Connected"]
                        status_info = f"Status Packet\nMax. connections: {data_buffer[0]}"
                        for i in range(1, len(data_buffer)):
                            status_info += f"\nHandle {i}: {status_values[data_buffer[i]]}"
                        print(status_info)

                    packet_index = 0
                elif DongleSerialInterfaceParser.valid_hex(c):
                    if packet_index % 2 == 0:
                        hi_nibble = ord(c)
                    else:
                        data_buffer.append(
                            DongleSerialInterfaceParser.get_val_from_hex_chars(hi_nibble, ord(c))
                        )
                else:
                    packet_index -= 1

    @staticmethod
    def valid_hex(c: str) -> bool:
        return ('0' <= c <= '9') or ('A' <= c <= 'F') or ('a' <= c <= 'f')

    @staticmethod
    def get_val_from_hex_chars(hi_nibble: int, lo_nibble: int) -> int:
        def hex_val(ch: int) -> int:
            if ord("0") <= ch <= ord("9"):
                return ch - ord("0")
            elif ord("A") <= ch <= ord("F"):
                return 10 + ch - ord("A")
            elif ord("a") <= ch <= ord("f"):
                return 10 + ch - ord("a")
            return 0

        return (hex_val(hi_nibble) << 4) + hex_val(lo_nibble)
