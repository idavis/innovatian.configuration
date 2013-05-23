@echo off
%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& build.ps1 %*"
