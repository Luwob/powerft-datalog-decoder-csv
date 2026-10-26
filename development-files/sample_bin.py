def analyze_chunk(data, start, size=100):
    chunk = data[start:start+size]
    hex_str = " ".join(f"{b:02x}" for b in chunk)
    ascii_str = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
    print(f"[{start:08x}]: {hex_str} | {ascii_str}")

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()

    print(f"Total size: {len(data)}")
    analyze_chunk(data, 1000000, 128)
    analyze_chunk(data, 5000000, 128)
    analyze_chunk(data, 10000000, 128)

    # Let's count how many bytes are printable ASCII
    ascii_count = sum(1 for b in data if 32 <= b < 127)
    print(f"Total ASCII bytes: {ascii_count} ({(ascii_count/len(data))*100:.2f}%)")

if __name__ == "__main__":
    main()
