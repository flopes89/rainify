@echo off
setlocal

rmdir /S /Q build\Plugin
rmdir /S /Q build\Console

msbuild rainify.sln /p:Configuration=Release /p:Platform=x64
msbuild rainify.sln /p:Configuration=Release /p:Platform=x86

endlocal
