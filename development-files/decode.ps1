Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

public class FTMLDecoder {
    public static void Decode(string exePath, string ftmlPath) {
        Assembly asm = Assembly.LoadFrom(exePath);
        
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            Console.WriteLine("Requested: " + e.Name);
            return null;
        };

        using (FileStream fs = new FileStream(ftmlPath, FileMode.Open))
        using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress)) {
            BinaryFormatter bf = new BinaryFormatter();
            try {
                object obj = bf.Deserialize(ds);
                Console.WriteLine("Success: " + obj.GetType().FullName);
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
"@ -ReferencedAssemblies "System.IO.Compression"

[FTMLDecoder]::Decode("C:\Program Files (x86)\FuelTech\FTManager\FTManager.exe", "C:\Users\xnehp\Desktop\code\ft-datolog-decoder\puxada wot.ftml")
