@echo off
setlocal
cd /d "%~dp0"
set "COMPILADOR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%COMPILADOR%" set "COMPILADOR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%COMPILADOR%" (
 echo Requer compilador C# do .NET Framework 4.8.
 pause
 exit /b 1
)
"%COMPILADOR%" /nologo /target:exe /codepage:65001 /main:Testes /out:Testes\Testes.exe /r:System.Core.dll SistemaCestas\Program.cs Testes\Testes.cs
if errorlevel 1 (
 pause
 exit /b 1
)
Testes\Testes.exe
set "RESULTADO=%ERRORLEVEL%"
pause
exit /b %RESULTADO%
