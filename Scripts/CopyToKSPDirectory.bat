::You must keep this file in the solution folder for it to work. 
::Make sure to pass the solution configuration when calling it (either Debug or Release)

::Set the directories in the setdirectories.bat file if you want a different folder than Kerbal Space Program
::EXAMPLE:
:: SET KSPPATH=C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program
:: SET KSPPATH2=C:\Users\Malte\Desktop\Kerbal Space Program
set KSPPATH=C:\Kerbal Space Program\KSP 1.7.3 Developer
set PLUGIN_NAME=HaystackContinued

set SOLUTIONCONFIGURATION=Debug

mkdir "%KSPPATH%\GameData\%PLUGIN_NAME%\"

mkdir "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins"

del "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins\*.*" /Q /F

"%~dp0..\External\pdb2mdb\pdb2mdb.exe" "%~dp0..\HaystackContinued\bin\%SOLUTIONCONFIGURATION%\HaystackContinued.dll"

xcopy /Y /O /X /E /H /K "%~dp0..\HaystackContinued\bin\%SOLUTIONCONFIGURATION%\*.*" "%KSPPATH%\GameData\%PLUGIN_NAME%\Plugins"

xcopy /Y /O /X /E /H /K "%~dp0..\GameData\HaystackContinued\*.*" "%KSPPATH%\GameData\%PLUGIN_NAME%"