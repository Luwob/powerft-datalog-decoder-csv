# FT Datalog Decoder

Conversor de datalogs FuelTech `.ftml` para `.csv` — rápido, direto e sem depender da exportação lenta do FTManager.

---

## 🚀 Como Usar

### Pré-requisitos

- **Windows** com **.NET Framework 4.x** instalado (já vem por padrão no Windows 10/11)
- **FTManager** instalado no caminho padrão: `C:\Program Files (x86)\FuelTech\FTManager\`

### Compilar

Abra o terminal na pasta do projeto e execute:

```cmd
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:export_csv.exe export_csv.cs
```

Ou simplesmente rode o script `build_and_run.bat` com duplo clique.

### Converter um datalog

**Opção 1 — Drag-and-drop (mais fácil):**

Arraste o arquivo `.ftml` e solte sobre o `export_csv.exe`. Uma janela do console abrirá, fará a conversão e mostrará o resultado. Pressione qualquer tecla para fechar.

**Opção 2 — Linha de comando:**

```cmd
export_csv.exe "caminho\do\meu_datalog.ftml"
```

**Exemplo:**

```cmd
export_csv.exe "resources\Log 2_20260517-2102_parcial.ftml"
```

O CSV é salvo automaticamente na pasta `output/` ao lado do executável, com o nome `nomedoarquivo_decoded.csv`.

No exemplo acima, a saída seria:
```
output\Log 2_20260517-2102_parcial_decoded.csv
```

O arquivo `.csv` gerado contém todos os canais habilitados do datalog, com coluna `TIME` calculada automaticamente a partir do `StartTime` e `SampleRate` do arquivo original.

### Configurar nomes dos canais

Edite o arquivo `config.yaml` na raiz do projeto para renomear os canais no CSV de saída:

```yaml
# config.yaml
RPMIndex: RPM
TPSIndex: TPS
MAPIndex: MAP
IgnAdvanceIndex: Ignition_timing
LambdaTargetIndex: O2_Target
Exhaust_O2Index: O2_General
```

- Formato: `NomeOriginal: NomeDesejado`
- Linhas com `#` são comentários
- Canais **não listados** no config mantêm o nome original do `.ftml`
- Se o `config.yaml` não existir, todos os canais usam o nome original

### Saída esperada

```
config.yaml carregado: 16 canais mapeados
Successfully exported to C:\...\output\meu_datalog_decoded.csv
```

O CSV gerado segue o formato (com nomes do `config.yaml` aplicados):

```csv
TIME,RPM,TPS,Ignition_timing,MAP,O2_Target,O2_Correction,O2_General,...
0.000,1223,3,4,-0.697,0,0,0,...
0.040,1223,3.1,6,-0.692,0,0,0,...
0.080,1232,4,8,-0.718,0,0,0,...
```

---

## 📋 O que faz

Este projeto **decodifica arquivos de datalog proprietários da FuelTech** (formato `.ftml`) e os converte para `.csv` padrão, permitindo análise em qualquer ferramenta externa (Excel, Python, nosso Datalog Viewer Pro, etc).

O software FTManager da FuelTech permite exportar datalogs como CSV, mas o processo é extremamente lento — especialmente para logs longos de sessões completas. Este decoder realiza a mesma conversão de forma **quase instantânea**.

### Canais extraídos

O decoder exporta **todos os canais habilitados** no datalog, incluindo (mas não limitado a):

| Canal original | Nome padrão (config.yaml) | Descrição |
|---|---|---|
| `RPMIndex` | `RPM` | Rotação do motor |
| `TPSIndex` | `TPS` | Posição da borboleta (%) |
| `MAPIndex` | `MAP` | Pressão do coletor (bar) |
| `IgnAdvanceIndex` | `Ignition_timing` | Avanço de ignição (°) |
| `LambdaTargetIndex` | `O2_Target` | Lambda alvo |
| `LambdaCorrectionIndex` | `O2_Correction` | Correção lambda (%) |
| `Exhaust_O2Index` | `O2_General` | Sonda O2 (lambda) |
| `InjBankAIndex` | `Primary_injection_time` | Tempo de injeção banco A (ms) |
| `EngineTempIndex` | `Engine_temp` | Temperatura do motor (°C) |
| `AirTempIndex` | `Air_temp` | Temperatura do ar (°C) |
| `FuelPressureIndex` | `Fuel_pressure` | Pressão de combustível (bar) |
| `VBatIndex` | `Battery_voltage` | Tensão da bateria (V) |
| `WheelSpeed_TractionHiResIndex` | `Wheel_speed_traction` | Velocidade da roda (km/h) |
| `ActualGearIndex` | `Gear` | Marcha atual |
| ... | ... | + todos os demais canais digitais e analógicos |

---

## ⚙️ Como funciona

### O problema: formato proprietário protegido

O arquivo `.ftml` é um objeto .NET serializado com `BinaryFormatter`, comprimido com **Deflate**. A estrutura interna é a classe `FTManager_Bll.FTDatalogger.DataloggerFile`, que contém:

- Metadados do arquivo (versão do SW, odômetro, horímetro)
- Informações da sessão (data, pista, piloto, clima)
- **Lista de canais** (`DataloggerChannel`) — cada um com tipo, sample rate e valores
- Configuração completa do módulo (mapas de injeção, ignição, etc.)

O problema adicional é que a FuelTech usa o **SmartAssembly** como ofuscador/protetor de código. Isso significa que a DLL `FTManager_Bll.dll` não existe como arquivo separado — ela está encriptada e embutida dentro do próprio `FTManager.exe`, sendo descriptografada em runtime por hooks internos.

### A solução: engenharia reversa via reflexão

O decoder utiliza uma abordagem engenhosa em 4 etapas:

```
┌─────────────────────────────────────────────────────┐
│  1. Carrega FTManager.exe via Assembly.LoadFrom()   │
│                                                     │
│  2. Força inicialização do SmartAssembly chamando   │
│     RuntimeHelpers.RunModuleConstructor() e          │
│     RunClassConstructor() no módulo <Module>        │
│     → Isso ativa os hooks de descriptografia        │
│                                                     │
│  3. Registra handler AssemblyResolve para redirecionar│
│     pedidos de "FTManager_Bll" para o assembly já    │
│     carregado (onde o SmartAssembly já decriptou)    │
│                                                     │
│  4. Deserializa o .ftml com BinaryFormatter +        │
│     DeflateStream + SerializationBinder customizado  │
│     → O .NET reconstrói o DataloggerFile completo!  │
└─────────────────────────────────────────────────────┘
```

**Etapa 1 — Carregar o assembly:**
O `FTManager.exe` é carregado em memória. Ao fazer isso, o .NET também carrega os recursos embutidos do SmartAssembly.

**Etapa 2 — Ativar o SmartAssembly:**
Chamamos `RunModuleConstructor` e `RunClassConstructor` no módulo `<Module>`. Isso força a execução dos construtores estáticos que o SmartAssembly injeta, registrando os hooks de descriptografia de assemblies na memória.

**Etapa 3 — Resolver dependências:**
Quando o `BinaryFormatter` tenta deserializar o arquivo, ele precisa da `FTManager_Bll`. Como ela não existe em disco (está encriptada dentro do EXE), registramos um `AssemblyResolve` handler que redireciona para o assembly principal — onde o SmartAssembly já descriptografou tudo.

**Etapa 4 — Deserializar e extrair:**
Com o `SerializationBinder` customizado, o `BinaryFormatter` consegue reconstruir o objeto `DataloggerFile` completo. A partir daí, usamos reflexão para acessar:
- `obj.Channels` → Lista de 73 canais
- Cada canal tem `.ChannelType` (nome), `.Enable` (ativo), `.SampleRate` e `.Values` (dados)
- Os valores já estão convertidos e na escala correta (Double)

### Estrutura do objeto deserializado

```
DataloggerFile
├── SW_Version: "5.50.xxxx"
├── StartTime: 0.0
├── EndTime: 248.36
├── OffSetTime: 0.0
├── ChannelsCount: 73
├── Channels: List<DataloggerChannel>
│   ├── [0] RPMIndex       → 6211 valores, SR=40ms
│   ├── [1] TPSIndex       → 6211 valores, SR=40ms
│   ├── [2] MAPIndex       → 6211 valores, SR=40ms
│   ├── [3] LambdaTarget   → 6210 valores, SR=40ms
│   └── ... (73 canais no total)
├── Times: DataloggerFileTimes
│   ├── DateSession, Driver, Track, CarInfo
│   ├── Temp, Humidity, BarPress (condições climáticas)
│   └── SessionType, RunType
├── Adjust: Adjust (configuração/mapas do módulo)
├── LogStream: byte[] (stream bruto comprimido)
└── AdjustCripto: List<AdjustCriptoItem> (segurança)
```

---

## 📁 Estrutura do Projeto

```
ft-datolog-decoder/
├── export_csv.cs          ← Código principal do decoder
├── export_csv.exe         ← Executável compilado
├── config.yaml            ← Mapeamento de nomes dos canais
├── build_and_run.bat      ← Script para compilar e testar
├── README.md              ← Este arquivo
├── output/                ← CSVs decodificados (gerado automaticamente)
└── resources/             ← Arquivos de exemplo/referência
    ├── *.ftml             ← Datalogs originais FuelTech
    ├── *.csv              ← CSVs exportados (referência)
    └── *.ftm              ← Arquivo de configuração/mapa
```

### Scripts auxiliares de engenharia reversa (desenvolvimento)

| Arquivo | Função |
|---|---|
| `decode.cs` | Inspeção do objeto deserializado (campos e propriedades) |
| `test_export.cs` | Teste de extração de valores com parâmetros de escala |
| `decode_times.cs` | Inspeção dos metadados de sessão (Times) |
| `analyze_bin.py` | Busca por padrões de dados de sensores no binário |
| `extract_strings.py` | Extração de strings ASCII do binário descomprimido |
| `find_arrays.py` | Busca por arrays primitivos no formato NRBF |
| `hexdump.py` | Inspeção hexadecimal de regiões do binário |

---

## ⚠️ Observações

- O decoder **depende do FTManager instalado** na máquina, pois utiliza o próprio executável como biblioteca para descriptografar e deserializar os dados.
- Atualizações do FTManager podem alterar a estrutura interna das classes. Se isso acontecer, o decoder pode precisar de ajustes.
- O caminho do FTManager está hardcoded em `export_csv.cs` na linha 20. Se sua instalação estiver em outro local, altere o caminho antes de compilar.
- Somente canais marcados como `Enable = true` no datalog são exportados para o CSV.

---

## 📄 Licença

Projeto de uso pessoal para análise de dados de motor. O formato `.ftml` é proprietário da FuelTech.
