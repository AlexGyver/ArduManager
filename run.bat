@echo off
dotnet restore
if errorlevel 1 exit /b %errorlevel%

dotnet run --project src/ArdulibsManager.App
if errorlevel 1 exit /b %errorlevel%