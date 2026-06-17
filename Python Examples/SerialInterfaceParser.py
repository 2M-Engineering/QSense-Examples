import CoreInterfaceParser
class SerialInterfaceParser:
    PREFIX = '$'
    POSTFIX = '\n'
    OPCODE_TRANSMIT = 'T'
    OPCODE_RECEIVE = 'R'
    OPCODE_UPGRADE = 'U'

    @staticmethod
    def byte_array_to_string(data: bytes) -> str:
        hex_string = ""
        for b in data:
            hex_string += f"{b:02X}"
        return hex_string

    @staticmethod
    def create_transmit_packet(core_interface_packet: bytes) -> str:
        hex_string = SerialInterfaceParser.byte_array_to_string(core_interface_packet)
        return f"{SerialInterfaceParser.PREFIX}{SerialInterfaceParser.OPCODE_TRANSMIT}{hex_string}{SerialInterfaceParser.POSTFIX}"

    @staticmethod
    def create_upgrade_packet() -> str:
        return f"{SerialInterfaceParser.PREFIX}{SerialInterfaceParser.OPCODE_UPGRADE}{SerialInterfaceParser.POSTFIX}"

    @staticmethod
    def parse_packet(packet: str):
        packet_index = 0
        opcode = '\0'
        hi_nible = 0
        data_buffer = []

        for c in packet:
            if SerialInterfaceParser.PREFIX == c:
                packet_index = 0
            else:
                packet_index += 1

            if packet_index == 1:
                opcode = c
                data_buffer.clear()

            if packet_index > 1:
                if SerialInterfaceParser.POSTFIX == c:
                    if opcode == SerialInterfaceParser.OPCODE_RECEIVE:
                        packet_info = "Receive Packet\r\n"
                        packet_info += f"Handle: {data_buffer[0]}\r\nData: "
                        for i in range(1, len(data_buffer)):
                            packet_info += f"{data_buffer[i]:02X}"
                        packet_info += "\r\n"
                        print(packet_info)

                        core_interface_packet = bytes(data_buffer[1:])
                        CoreInterfaceParser.parse_packet(core_interface_packet)
                    packet_index = 0
                elif SerialInterfaceParser.valid_hex(c):
                    if packet_index % 2 == 0:
                        hi_nible = ord(c)
                    else:
                        data_buffer.append(
                            SerialInterfaceParser.get_val_from_hex_chars(hi_nible, ord(c))
                        )
                else:
                    packet_index -= 1

    @staticmethod
    def valid_hex(c: str) -> bool:
        return ('0' <= c <= '9') or ('A' <= c <= 'F') or ('a' <= c <= 'f')

    @staticmethod
    def get_val_from_hex_chars(hi_nible: int, lo_nible: int) -> int:
        result = 0
        if ord('0') <= hi_nible <= ord('9'):
            result = hi_nible - ord('0')
        elif ord('A') <= hi_nible <= ord('F'):
            result = 10 + hi_nible - ord('A')
        elif ord('a') <= hi_nible <= ord('f'):
            result = 10 + hi_nible - ord('a')

        result <<= 4

        if ord('0') <= lo_nible <= ord('9'):
            result += lo_nible - ord('0')
        elif ord('A') <= lo_nible <= ord('F'):
            result += 10 + lo_nible - ord('A')
        elif ord('a') <= lo_nible <= ord('f'):
            result += 10 + lo_nible - ord('a')

        return result & 0xFF
