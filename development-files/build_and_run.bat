@echo off
set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo Compilador C# nao encontrado em %CSC%
    exit /b 1
)

echo Compilando export_csv.cs...
"%CSC%" /nologo /out:export_csv.exe export_csv.cs
if %errorlevel% neq 0 (
    echo Erro na compilacao.
    exit /b %errorlevel%
)

echo.
echo Compilado com sucesso: export_csv.exe
echo Uso: export_csv.exe "caminho/do/datalog.ftml"
echo   O CSV sera salvo automaticamente em: output/nomedoarquivo_decoded.csv
echo.
echo Testando com resources\puxada wot.ftml...
export_csv.exe "resources\puxada wot.ftml"
