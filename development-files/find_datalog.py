import struct
import math

def search_approx(data, vals, tolerance=0.01):
    print(f"Searching for {vals}")
    fmt = f'<{len(vals)}d'
    size = len(vals) * 8
    for i in range(0, len(data) - size, 8):
        chunk = data[i:i+size]
        doubles = struct.unpack(fmt, chunk)
        match = True
        for a, b in zip(doubles, vals):
            if not math.isclose(a, b, abs_tol=tolerance):
                match = False
                break
        if match:
            print(f"Found match at offset {i:08x}: {doubles}")

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()

    # Search for MAP: -0.73, -0.71, -0.71, -0.69
    search_approx(data, [-0.73, -0.71, -0.71, -0.69])

    # Search for TPS: 8.2, 8.3, 9.3, 9.8
    search_approx(data, [8.2, 8.3, 9.3, 9.8])
    
    # Search for O2_Target: 0.773, 1.001, 1.038
    search_approx(data, [0.773, 1.001, 1.038], tolerance=0.001)

if __name__ == "__main__":
    main()
