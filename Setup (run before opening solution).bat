:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off
set /p path="Please enter the folder location of your SpaceEngineersDedicated.exe: "
cd %~dp0
mklink /J GameBinaries "%path%"
if errorlevel 1 goto Error
echo Done! You can now open the Torch solution without issue.
goto End
:Error
echo An error occured creating the symlink.
:End
pause
