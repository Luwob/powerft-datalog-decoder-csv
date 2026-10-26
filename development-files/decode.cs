using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Collections;
using System.Threading;

class Program {
    static void Main(string[] args) {
        string exePath = @"C:\Program Files (x86)\FuelTech\FTManager\FTManager.exe";
        string ftmlPath = @"C:\Users\xnehp\Desktop\code\ft-datolog-decoder\puxada wot.ftml";
        
        Assembly asm = Assembly.LoadFrom(exePath);
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(asm.ManifestModule.ModuleHandle);
        foreach (Type t in asm.GetTypes()) {
            if (t.Name == "<Module>") {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
            }
        }
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            if (e.Name.StartsWith("FTManager_Bll")) return asm;
            return null;
        };

        using (FileStream fs = new FileStream(ftmlPath, FileMode.Open))
        using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress)) {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new MyBinder(asm);
            try {
                object obj = bf.Deserialize(ds);
                Type type = obj.GetType();
                
                PropertyInfo channelsProp = type.GetProperty("Channels");
                IEnumerable channels = (IEnumerable)channelsProp.GetValue(obj, null);
                
                int c = 0;
                foreach (var channel in channels) {
                    PropertyInfo typeProp = channel.GetType().GetProperty("ChannelType");
                    object chType = typeProp.GetValue(channel, null);
                    
                    PropertyInfo valuesProp = channel.GetType().GetProperty("Values");
                    object valuesObj = valuesProp.GetValue(channel, null);
                    
                    int count = 0;
                    IEnumerable vList = valuesObj as IEnumerable;
                    if (vList != null) {
                        foreach (var v in vList) count++;
                    }
                    
                    Console.WriteLine(string.Format("Channel {0}: Type={1}, ValuesCount={2}", c, chType, count));
                    c++;
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}

class MyBinder : SerializationBinder {
    Assembly _asm;
    public MyBinder(Assembly asm) { _asm = asm; }
    public override Type BindToType(string assemblyName, string typeName) {
        Type t = _asm.GetType(typeName);
        if (t != null) return t;
        return Type.GetType(typeName);
    }
}
