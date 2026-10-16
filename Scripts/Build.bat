@ECHO OFF
REM Builds the Release version for Windows (net472) and packages it as EuropeanMortality.zip.
REM Usage: Build.bat [OutputFolder]   (default: <repository>\publish)
SETLOCAL

SET RootFolder=%~dp0..
SET OutputFolder=%~1
IF "%OutputFolder%"=="" SET OutputFolder=%RootFolder%\publish
SET PackageName=EuropeanMortality
SET PackageFolder=%OutputFolder%\%PackageName%
SET ZipFile=%OutputFolder%\%PackageName%.zip
SET BinFolder=%RootFolder%\EuropeanMortalityAnalyzer\bin\Release\net472

dotnet build "%RootFolder%\EuropeanMortalityAnalyzer.sln" -c Release -f net472 --no-incremental
IF ERRORLEVEL 1 GOTO Error

IF EXIST "%PackageFolder%" RMDIR /S /Q "%PackageFolder%"
MKDIR "%PackageFolder%\Bin"
REM The exe and its config stay at the root; the config's probing path makes the exe load the dlls from Bin
COPY /Y "%BinFolder%\EuropeanMortalityAnalyzer.exe" "%PackageFolder%" >NUL || GOTO Error
COPY /Y "%BinFolder%\EuropeanMortalityAnalyzer.exe.config" "%PackageFolder%" >NUL || GOTO Error
COPY /Y "%BinFolder%\*.dll" "%PackageFolder%\Bin" >NUL || GOTO Error
REM SQLite looks for its native library in x86/x64 next to System.Data.SQLite.dll
XCOPY "%BinFolder%\x86" "%PackageFolder%\Bin\x86" /I /Y /Q >NUL || GOTO Error
XCOPY "%BinFolder%\x64" "%PackageFolder%\Bin\x64" /I /Y /Q >NUL || GOTO Error

IF EXIST "%ZipFile%" MOVE /Y "%ZipFile%" "%OutputFolder%\%PackageName%.bak.zip" >NUL
REM Use the tar.exe shipped with Windows 10+ (not Git's GNU tar); -a picks the zip format from the extension
"%SystemRoot%\System32\tar.exe" -a -c -f "%ZipFile%" -C "%OutputFolder%" %PackageName% || GOTO Error

ECHO Package created: %ZipFile%
GOTO End

:Error
ECHO Build failed!
ENDLOCAL
PAUSE
EXIT /B 1

:End
ENDLOCAL
PAUSE
