echo -----------------------------
echo $(Targetname) Post Build start
echo ...
echo set local build path vars...
set /p KSP_DIR=<"$(ProjectDir)LocalDev\ksp_dir.txt"
set /p PDB2MDB_EXE_DIR=<"$(ProjectDir)LocalDev\pdb2mdb_exe.txt"
set /p ZA_DIR=<"$(ProjectDir)LocalDev\7za_dir.txt"
set /p DIST_DIR=<"$(ProjectDir)LocalDev\dist_dir.txt"

echo distributing $(Targetname) files...
copy /Y "$(TargetPath)" "$(ProjectDir)Distribution\GameData\$(Targetname)\Plugins\"
copy /Y "$(TargetDir)CLSInterfaces.dll" "$(ProjectDir)Distribution\GameData\$(Targetname)\Plugins\"

echo Copying $(Targetname) files to test env:  %KSP_DIR%\GameData\$(Targetname)\...
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\Localization\*.*" "%KSP_DIR%\GameData\$(Targetname)\Localization\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\assets\*.*" "%KSP_DIR%\GameData\$(Targetname)\assets\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\configs\*.*" "%KSP_DIR%\GameData\$(Targetname)\Configs\"
copy /Y "$(TargetPath)" "%KSP_DIR%\GameData\$(Targetname)\Plugins\"
copy /Y "$(TargetDir)$(Targetname).pdb" "%KSP_DIR%\GameData\$(Targetname)\Plugins\"
copy /Y "$(TargetDir)CLSInterfaces.dll" "%KSP_DIR%\GameData\$(Targetname)\Plugins\"

echo distributing $(Targetname) files...
del /Q "%DIST_DIR%\Distribution\GameData\$(Targetname)\Configs\*.*"
del /Q "%DIST_DIR%\Distribution\GameData\$(Targetname)\assets\*.*"
del /Q "%DIST_DIR%\Distribution\GameData\$(Targetname)\Localization\*.*"

copy /Y "$(TargetPath)" "%DIST_DIR%\Distribution\GameData\$(Targetname)\Plugins\"
copy /Y "$(TargetDir)\CLSInterfaces.dll" "%DIST_DIR%\Distribution\GameData\$(Targetname)\Plugins\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\Localization\*.*" "%DIST_DIR%\Distribution\GameData\$(Targetname)\Localization\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\assets\*.*" "%DIST_DIR%\Distribution\GameData\$(Targetname)\assets\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\configs\*.*" "%DIST_DIR%\Distribution\GameData\$(Targetname)\Configs\"
copy /Y "$(ProjectDir)\Distribution\README.txt" "%DIST_DIR%\Distribution\"
copy /Y "$(ProjectDir)\Distribution\GameData\$(Targetname)\$(Targetname).version" "%DIST_DIR%\Distribution\GameData\$(Targetname)\"

echo building $(Targetname).dll.mdb file...
cd "$(TargetDir)"
call "%PDB2MDB_EXE_DIR%\pdb2mdb.exe" $(Targetname).dll

copy /Y "$(TargetDir)$(Targetname).dll.mdb" "%KSP_DIR%\GameData\$(Targetname)\Plugins\"

echo packaging files...
if exist "%DIST_DIR%\Distribution\dev\CLSDevPack*.zip" del "%DIST_DIR%\Distribution\dev\CLSDevPack*.zip"
call "%ZA_DIR%\7za.exe" a -tzip -r  "%DIST_DIR%\Distribution\dev\CLSDevPack.@(VersionNumber).zip" "%DIST_DIR%\CLSDevPack\*.*"


if exist "%KSP_DIR%\$(Targetname)*.zip" del "%KSP_DIR%\$(Targetname)*.zip"
call "%ZA_DIR%\7za.exe" a -tzip -r  "%DIST_DIR%\$(Targetname).@(VersionNumber)_%DATE:~4,2%%DATE:~7,2%%DATE:~10,4%.zip" "%DIST_DIR%\Distribution\*.*"

echo ...
echo Post Build complete!

