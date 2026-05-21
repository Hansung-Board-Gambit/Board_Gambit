@echo off
setlocal
set EXE=%~dp001_TestBuild\BoardGambit.exe
if not exist "%EXE%" (
  echo Build executable not found: %EXE%
  pause
  exit /b 1
)
start "BoardGambit B" "%EXE%" -force-d3d11 -screen-width 1280 -screen-height 720 -logFile "%~dp0BoardGambit_B.log"
