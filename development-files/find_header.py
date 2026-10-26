def analyze_chunk(data, start, size=100):
    chunk = data[start:start+size]
    hex_str = " ".join(f"{b:02x}" for b in chunk)
    ascii_str = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
    print(f"[{start:08x}]: {hex_str} | {ascii_str}")

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()

    # Find the first occurrence of `00 00 00 00 00 00 f0 3f 00 00 00 00 00 00 f0 3f`
    pattern = b'\x00\x00\x00\x00\x00\x00\xf0\x3f' * 2
    idx = data.find(pattern)
    print(f"Found 1.0 sequence at {idx:08x}")
    if idx != -1:
        analyze_chunk(data, idx - 64, 128)
        
    # Also search for the float 96.992 or double 96.992 again, maybe it is there!
    import struct
    time_double = struct.pack('<d', 96.992)
    idx_time = data.find(time_double)
    print(f"Found 96.992 at {idx_time}")
    
    rpm_double = struct.pack('<d', 1977.0)
    idx_rpm = data.find(rpm_double)
    print(f"Found 1977.0 at {idx_rpm}")

if __name__ == "__main__":
    main()
