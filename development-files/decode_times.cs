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
                
                PropertyInfo timesProp = type.GetProperty("Times");
                object timesObj = timesProp.GetValue(obj, null);
                
                Console.WriteLine("Times object type: " + timesObj.GetType().FullName);
                foreach (var prop in timesObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    object val = null;
                    try { val = prop.GetValue(timesObj, null); } catch {}
                    Console.WriteLine("  Prop: " + prop.Name + " = " + (val == null ? "null" : val.GetType().Name));
                    IEnumerable vList = val as IEnumerable;
                    if (vList != null && !(val is string)) {
                        int count = 0;
                        foreach (var v in vList) count++;
                        Console.WriteLine("    Count: " + count);
                    }
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
