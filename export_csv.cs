using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

class Program {

    // Detectar se o console foi criado pelo proprio processo (drag-and-drop)
    [DllImport("kernel32.dll")]
    static extern uint GetConsoleProcessList(uint[] list, uint count);

    static bool IsRunningFromExplorer() {
        uint[] list = new uint[2];
        uint count = GetConsoleProcessList(list, 2);
        return count <= 1; // Se so tem 1 processo no console, foi aberto por drag-and-drop
    }

    // Carrega mapeamento de nomes do config.yaml (se existir)
    static Dictionary<string, string> LoadChannelNames(string configPath) {
        Dictionary<string, string> map = new Dictionary<string, string>();
        if (!File.Exists(configPath)) return map;

        foreach (string rawLine in File.ReadAllLines(configPath)) {
            string line = rawLine.Trim();
            // Ignorar linhas vazias e comentarios
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            int sep = line.IndexOf(':');
            if (sep < 0) continue;

            string key = line.Substring(0, sep).Trim();
            string val = line.Substring(sep + 1).Trim();
            if (key.Length > 0 && val.Length > 0) {
                map[key] = val;
            }
        }
        return map;
    }

    static void Main(string[] args) {
        bool dragDrop = IsRunningFromExplorer();

        if (args.Length < 1) {
            Console.WriteLine("Uso: export_csv.exe \"caminho/do/datalog.ftml\"");
            Console.WriteLine("  O CSV sera salvo em: output/nomedoarquivo_decoded.csv");
            Console.WriteLine("  Configure nomes dos canais em config.yaml");
            Console.WriteLine();
            Console.WriteLine("  Dica: arraste um arquivo .ftml sobre o export_csv.exe!");
            if (dragDrop) { Console.WriteLine(); Console.Write("Pressione qualquer tecla para fechar..."); Console.ReadKey(); }
            return;
        }

        string ftmlPath = args[0];
        string exePath = @"C:\Program Files (x86)\FuelTech\FTManager\FTManager.exe";

        // Gerar caminho de saida automaticamente
        string inputName = Path.GetFileNameWithoutExtension(ftmlPath);
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string outDir = Path.Combine(exeDir, "output");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, inputName + "_decoded.csv");

        // Carregar mapeamento de nomes dos canais
        string configPath = Path.Combine(exeDir, "config.yaml");
        Dictionary<string, string> channelNames = LoadChannelNames(configPath);
        if (channelNames.Count > 0) {
            Console.WriteLine("config.yaml carregado: " + channelNames.Count + " canais mapeados");
        }

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
                
                double startTime = Convert.ToDouble(type.GetProperty("StartTime").GetValue(obj, null));
                
                PropertyInfo channelsProp = type.GetProperty("Channels");
                IEnumerable channels = (IEnumerable)channelsProp.GetValue(obj, null);
                
                List<string> headers = new List<string>();
                headers.Add("TIME");
                
                List<List<double>> columns = new List<List<double>>();
                int maxRows = 0;
                double step = 0.04; // default
                
                foreach (var channel in channels) {
                    bool enabled = Convert.ToBoolean(channel.GetType().GetProperty("Enable").GetValue(channel, null));
                    if (!enabled) continue;
                    
                    string chType = channel.GetType().GetProperty("ChannelType").GetValue(channel, null).ToString();

                    // Aplicar renomeacao do config.yaml (se existir)
                    string displayName;
                    if (channelNames.ContainsKey(chType)) {
                        displayName = channelNames[chType];
                    } else {
                        displayName = chType;
                    }
                    headers.Add(displayName);
                    
                    int sr = Convert.ToInt32(channel.GetType().GetProperty("SampleRate").GetValue(channel, null));
                    if (sr > 0) step = sr / 1000.0;
                    
                    IEnumerable values = channel.GetType().GetProperty("Values").GetValue(channel, null) as IEnumerable;
                    List<double> col = new List<double>();
                    if (values != null) {
                        foreach (var v in values) {
                            col.Add(Convert.ToDouble(v));
                        }
                    }
                    columns.Add(col);
                    if (col.Count > maxRows) maxRows = col.Count;
                }
                
                using (StreamWriter sw = new StreamWriter(outPath)) {
                    sw.WriteLine(string.Join(",", headers));
                    for (int i = 0; i < maxRows; i++) {
                        List<string> row = new List<string>();
                        double time = startTime + (i * step);
                        row.Add(Math.Round(time, 3).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        
                        foreach (var col in columns) {
                            if (i < col.Count) {
                                row.Add(col[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                            } else {
                                row.Add("");
                            }
                        }
                        sw.WriteLine(string.Join(",", row));
                    }
                }
                Console.WriteLine("Successfully exported to " + outPath);
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }

        // Pausar se executado via drag-and-drop para que o usuario veja o resultado
        if (dragDrop) {
            Console.WriteLine();
            Console.Write("Pressione qualquer tecla para fechar...");
            Console.ReadKey();
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
