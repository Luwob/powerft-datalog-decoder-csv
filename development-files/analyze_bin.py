import struct

def find_bytes(data, sequence):
    res = []
    idx = data.find(sequence)
    while idx != -1:
        res.append(idx)
        idx = data.find(sequence, idx + 1)
    return res

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()

    print("Strings:")
    for s in [b"RPM", b"TIME", b"O2_Target", b"TPS"]:
        indices = find_bytes(data, s)
        print(f"'{s.decode()}' found at: {indices}")

    # Let's search for RPM values: 1977, 1982, 2017
    # They could be 2-byte short, 4-byte int, 4-byte float, 8-byte double
    rpm_vals = [1977, 1982, 2017, 2079, 2110]
    
    # Try float
    print("\nTrying Float (RPM):")
    b = b''
    for v in rpm_vals:
        b += struct.pack('<f', v)
    idx = data.find(b)
    print(f"Float consecutive match at: {idx}")

    # Try double
    print("\nTrying Double (RPM):")
    b = b''
    for v in rpm_vals:
        b += struct.pack('<d', v)
    idx = data.find(b)
    print(f"Double consecutive match at: {idx}")

    # Try 16-bit int
    print("\nTrying Int16 (RPM):")
    b = b''
    for v in rpm_vals:
        b += struct.pack('<h', v)
    idx = data.find(b)
    print(f"Int16 consecutive match at: {idx}")
    
    # Try TIME: 96.992, 97.032, 97.072
    time_vals = [96.992, 97.032, 97.072]
    # In float, exact representation might differ slightly, but let's try Double
    print("\nTrying Double (TIME):")
    b = b''
    for v in time_vals:
        b += struct.pack('<d', v)
    idx = data.find(b)
    print(f"Double consecutive match for TIME at: {idx}")

    # Try Float TIME
    print("\nTrying Float (TIME - approx):")
    # Float search might fail due to precision, let's search just the first value
    for v in time_vals:
        b = struct.pack('<f', v)
        indices = find_bytes(data, b)
        print(f"Float TIME {v} found at: {indices}")

if __name__ == "__main__":
    main()
