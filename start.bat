@echo off
title JDKTrap Project Builder
color 0B
echo ===================================================
echo             JDKTrap Project Auto-Builder           
echo ===================================================
echo.
echo [*] Dang kiem tra .NET SDK...
dotnet --list-sdks >nul 2>&1
if %errorlevel% neq 0 (
    color 0C
    echo [LOI] Khong tim thay .NET SDK! Vui long cai dat .NET SDK tu: https://dotnet.microsoft.com/download
    goto end
)

echo [*] Dang tien hanh bien dich JDKTrap.sln...
echo.
dotnet build JDKTrap.sln

if %errorlevel% equ 0 (
    color 0A
    echo.
    echo ===================================================
    echo [THANH CONG] Bien dich du an hoan tat!
    echo ===================================================
) else (
    color 0C
    echo.
    echo ===================================================
    echo [THAT BAI] Bien dich gap loi! Vui long cuon len xem chi tiet.
    echo ===================================================
)

:end
echo.
echo Nhan phim bat ky de thoat...
pause >nul
