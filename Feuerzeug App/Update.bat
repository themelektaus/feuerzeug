@echo off
:waitfortask
for /f %%i in ('tasklist') do (
  if "%%i" == "Feuerzeug App.exe" (
    timeout /t 2 /nobreak >nul
    goto waitfortask
  )
  if "%%i" == "Feuerzeug Service.exe" (
    timeout /t 2 /nobreak >nul
    goto waitfortask
  )
)
timeout /t 1 /nobreak >nul
robocopy temp . /e /r:1 /w:1 /dst
timeout /t 1 /nobreak >nul
rmdir /s /q temp
timeout /t 1 /nobreak >nul
start "" "Feuerzeug App.exe"