using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

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
                
                Console.WriteLine("StartTime: " + type.GetProperty("StartTime").GetValue(obj, null));
                Console.WriteLine("OffSetTime: " + type.GetProperty("OffSetTime").GetValue(obj, null));

                PropertyInfo channelsProp = type.GetProperty("Channels");
                IEnumerable channels = (IEnumerable)channelsProp.GetValue(obj, null);
                
                foreach (var channel in channels) {
                    string chType = channel.GetType().GetProperty("ChannelType").GetValue(channel, null).ToString();
                    if (chType.Contains("RPM") || chType.Contains("TPS") || chType.Contains("MAP") || chType.Contains("Target")) {
                        double dividend = Convert.ToDouble(channel.GetType().GetProperty("Dividend").GetValue(channel, null));
                        double divider = Convert.ToDouble(channel.GetType().GetProperty("Divider").GetValue(channel, null));
                        double valOffset = Convert.ToDouble(channel.GetType().GetProperty("ValOffset").GetValue(channel, null));
                        double factor = Convert.ToDouble(channel.GetType().GetProperty("Factor").GetValue(channel, null));
                        int sampleRate = Convert.ToInt32(channel.GetType().GetProperty("SampleRate").GetValue(channel, null));
                        
                        Console.WriteLine(string.Format("{0} -> divd:{1} divr:{2} ofs:{3} fact:{4} SR:{5}", chType, dividend, divider, valOffset, factor, sampleRate));
                        
                        IEnumerable values = channel.GetType().GetProperty("Values").GetValue(channel, null) as IEnumerable;
                        int c = 0;
                        Console.Write("  Values: ");
                        foreach (var v in values) {
                            if (c > 2400 && c < 2410) {
                                Console.Write(v + " ");
                            }
                            c++;
                        }
                        Console.WriteLine();
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
