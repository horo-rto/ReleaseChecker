@echo off
setlocal

:: Read version from csproj
for /f "usebackq tokens=*" %%a in (`powershell -NoProfile -Command "(Select-Xml -Path 'ReleaseChecker\ReleaseChecker.csproj' -XPath '//Version').Node.InnerText"`) do set VER=%%a

echo Version: %VER%
echo.

echo === Building lightweight version (requires .NET 8) ===
dotnet publish ReleaseChecker\ReleaseChecker.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o publish\tmp\lightweight
echo.

echo === Building portable version (self-contained) ===
dotnet publish ReleaseChecker\ReleaseChecker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\tmp\portable
echo.

echo === Done ===
echo.
echo Copying ReleaseChecker-%VER%-light.exe
copy /y publish\tmp\lightweight\ReleaseChecker.exe ReleaseChecker-%VER%-light.exe
:: copy /y publish\tmp\lightweight\MediaInfo.dll MediaInfo.dll
echo Copying ReleaseChecker-%VER%-portable.exe
copy /y publish\tmp\portable\ReleaseChecker.exe ReleaseChecker-%VER%-portable.exe
rmdir /s /q publish
echo Copying ReleaseChecker-latest-light.exe
copy /y ReleaseChecker-%VER%-light.exe ReleaseChecker-latest-light.exe
pause
