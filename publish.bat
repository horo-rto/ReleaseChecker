@echo off
echo === Building lightweight version (requires .NET 8) ===
dotnet publish ReleaseChecker\ReleaseChecker.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o publish\tmp\lightweight
echo.

echo === Building portable version (self-contained) ===
dotnet publish ReleaseChecker\ReleaseChecker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\tmp\portable
echo.

echo === Done ===
copy /y publish\tmp\lightweight\ReleaseChecker.exe ReleaseChecker-light.exe
copy /y publish\tmp\lightweight\MediaInfo.dll MediaInfo.dll
copy /y publish\tmp\portable\ReleaseChecker.exe ReleaseChecker-portable.exe
rmdir /s /q publish\tmp
pause
