@echo off
setlocal

set PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin;%PATH%
set MSBUILD_ARGS=/property:Configuration=Release /nologo /verbosity:quiet

rd /q /s Console\bin
rd /q /s Plugin\bin
msbuild rainify.sln %MSBUILD_ARGS% /property:Platform=x64
msbuild rainify.sln %MSBUILD_ARGS% /property:Platform=x86

rd /q /s dist\Console
rd /q /s dist\plugin
xcopy Console\bin\x64\Release dist\Console\x64 /i
xcopy Console\bin\x86\Release dist\Console\x86 /i
xcopy Plugin\bin\x64\Release\rainify.dll dist\plugin\x64\ /i
xcopy Plugin\bin\x86\Release\rainify.dll dist\plugin\x86\ /i

endlocal
