import re

def extract_strings(filename, min_length=4):
    with open(filename, "rb") as f:
        data = f.read()
    
    ascii_pattern = b"[%s]{%d,}" % (b" -~", min_length)
    ascii_strings = [m.decode('ascii') for m in re.findall(ascii_pattern, data)]
    
    with open("strings.txt", "w", encoding="utf-8") as f:
        for s in set(ascii_strings):
            f.write(s + "\n")

if __name__ == "__main__":
    extract_strings("decompressed.bin")
