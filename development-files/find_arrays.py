import struct

def search_primitive_arrays(data):
    # RecordType 0x0F (15) = SingleDimensionalPrimitiveArray
    print("Searching for NRBF SingleDimensionalPrimitiveArrays...")
    for i in range(len(data) - 10):
        if data[i] == 0x0F:
            length = struct.unpack('<I', data[i+5:i+9])[0]
            if 100 < length < 2000000: # Reasonable lengths
                prim_type = data[i+9]
                if prim_type == 0x08: # Double
                    print(f"Found Double array of length {length} at offset {i}. Bytes: {length * 8}")
                    # Print first 5 values
                    if i + 10 + length*8 <= len(data):
                        vals = struct.unpack(f'<{min(5, length)}d', data[i+10:i+10+min(5, length)*8])
                        print(f"  First values: {vals}")
                elif prim_type == 0x07: # Single
                    print(f"Found Single array of length {length} at offset {i}. Bytes: {length * 4}")
                    if i + 10 + length*4 <= len(data):
                        vals = struct.unpack(f'<{min(5, length)}f', data[i+10:i+10+min(5, length)*4])
                        print(f"  First values: {vals}")
                elif prim_type == 0x06: # Int32
                    pass
                elif prim_type == 0x02: # Byte
                    print(f"Found Byte array of length {length} at offset {i}")
                    
def main():
    with open("decompressed.bin", "rb") as f:
        data = f.read()
    search_primitive_arrays(data)

if __name__ == "__main__":
    main()
