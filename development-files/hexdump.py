import struct

def hexdump(data, addr, length=128):
    start = max(0, addr - length//2)
    end = min(len(data), addr + length//2)
    chunk = data[start:end]
    
    print(f"--- Hexdump around {addr} ---")
    for i in range(0, len(chunk), 16):
        row = chunk[i:i+16]
        hex_str = " ".join(f"{b:02x}" for b in row)
        ascii_str = "".join(chr(b) if 32 <= b < 127 else "." for b in row)
        print(f"{start+i:08x}: {hex_str:<48} |{ascii_str}|")

def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()
        
    s = b"DataloggerChannelItem"
    idx = data.find(s)
    if idx != -1:
        hexdump(data, idx, 256)
        
    # Also find 'RPM' and see what's near it
    s2 = b"DASH_ITEM_RPM"
    idx2 = data.find(s2)
    if idx2 != -1:
        hexdump(data, idx2, 256)

if __name__ == "__main__":
    main()
