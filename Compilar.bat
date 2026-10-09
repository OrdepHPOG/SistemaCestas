@echo off
setlocal
cd /d "%~dp0"
set "COMPILADOR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%COMPILADOR%" set "COMPILADOR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%COMPILADOR%" (
 echo Compilador nao encontrado. Abra SistemaCestas.sln no Visual Studio 2022 com .NET Framework 4.8.
 pause
 exit /b 1
)
if not exist Executavel mkdir Executavel
"%COMPILADOR%" /nologo /target:exe /optimize+ /codepage:65001 /out:Executavel\SistemaCestas.exe /r:System.Core.dll SistemaCestas\Program.cs
if errorlevel 1 (
 echo Falha na compilacao. Requer .NET Framework 4.8. Consulte LEIA-ME.md.
 pause
 exit /b 1
)
echo Executavel criado: Executavel\SistemaCestas.exe
pause
