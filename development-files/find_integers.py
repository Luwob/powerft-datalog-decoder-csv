import struct

def find_sequence(data, format_str, values):
    b = b''
    for v in values:
        b += struct.pack(format_str, v)
    idx = data.find(b)
    return idx

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()

    # Time values: 96992, 97032, 97072 (diff 40)
    time_vals = [96992, 97032, 97072]
    idx = find_sequence(data, '<i', time_vals)
    print(f"Time as int32 found at: {idx}")

    idx = find_sequence(data, '<I', time_vals)
    print(f"Time as uint32 found at: {idx}")

    # RPM: 1977, 1982, 2017
    rpm_vals = [1977, 1982, 2017]
    idx = find_sequence(data, '<h', rpm_vals)
    print(f"RPM as int16 found at: {idx}")

    idx = find_sequence(data, '<H', rpm_vals)
    print(f"RPM as uint16 found at: {idx}")

    idx = find_sequence(data, '<i', rpm_vals)
    print(f"RPM as int32 found at: {idx}")

    # Maybe values are stored as Float?
    # Let's check exactly the first value of time: 96.992
    idx = data.find(struct.pack('<f', 96.992))
    print(f"Exact float 96.992 found at: {idx}")
    
    idx = data.find(struct.pack('<d', 96.992))
    print(f"Exact double 96.992 found at: {idx}")

    # Let's search for RPM as floats
    idx = find_sequence(data, '<f', [1977.0, 1982.0, 2017.0])
    print(f"RPM as float found at: {idx}")
    idx = find_sequence(data, '<d', [1977.0, 1982.0, 2017.0])
    print(f"RPM as double found at: {idx}")

if __name__ == "__main__":
    main()
