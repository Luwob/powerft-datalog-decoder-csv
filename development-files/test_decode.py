import zlib
import sys

def main():
    try:
        with open("mapa_tps_novo.ftm", "rb") as f:
            data = f.read()
        decompressed = zlib.decompress(data, -zlib.MAX_WBITS)
        with open("mapa.bin", "wb") as f:
            f.write(decompressed)
        print(f"Decompressed mapa size: {len(decompressed)}")
    except Exception as e:
        print("Error:", e)

if __name__ == "__main__":
    main()
