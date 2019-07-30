::You must keep this file in the solution folder for it to work. 
::Make sure to pass the solution configuration when calling it (either Debug or Release)

::Set the directories in the setdirectories.bat file if you want a different folder than Kerbal Space Program
::EXAMPLE:
:: SET KSPPATH=C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program
:: SET KSPPATH2=C:\Users\Malte\Desktop\Kerbal Space Program
call ".\SetDirectories.bat"
set PLUGIN_NAME="Haystack"

IF DEFINED KSPPATH (ECHO KSPPATH is defined) ELSE (SET KSPPATH=C:\Kerbal Space Program)
::%1
SET SOLUTIONCONFIGURATION=Debug

mkdir "%KSPPATH%\GameData\%PLUGIN_NAME%\"

mkdir "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins"
IF DEFINED KSPPATH2 (mkdir "%KSPPATH2%\GameData\%PLUGIN_NAME%\Plugins")

del "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins\*.*" /Q /F

"%~dp0..\External\pdb2mdb\pdb2mdb.exe" "%~dp0..\HaystackContinued\bin\%SOLUTIONCONFIGURATION%\HaystackContinued.dll"

xcopy /Y "%~dp0..\HaystackContinued\bin\%SOLUTIONCONFIGURATION%\*.*" "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins"