@echo off
setlocal

rmdir /S /Q build\Plugin
rmdir /S /Q build\rainify\@Resources\Console

set MSBUILD_ARGS=/property:Configuration=Release /nologo /verbosity:minimal

msbuild rainify.sln %MSBUILD_ARGS% /property:Platform=x64
msbuild rainify.sln %MSBUILD_ARGS% /property:Platform=x86

endlocal
