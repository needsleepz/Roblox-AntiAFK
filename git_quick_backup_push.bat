@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
title Git Quick Backup Push Auto Remote Fixed

set "GITHUB_OWNER=needsleepz"
for %%I in ("%CD%") do set "FOLDER_NAME=%%~nxI"
set "AUTO_REMOTE=https://github.com/%GITHUB_OWNER%/%FOLDER_NAME%.git"

echo ============================================================
echo  Git Quick Backup + Push Auto Remote - FIXED
echo  Runs:
echo    git status
echo    git add .
echo    git commit -m "Backup before changes"
echo    git push
echo.
echo  If no origin exists, it auto-suggests:
echo    %AUTO_REMOTE%
echo ============================================================
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Git was not found in PATH.
    pause
    exit /b 1
)

if not exist ".git" (
    echo [ERROR] This folder is not a Git repository.
    echo Current folder:
    echo %CD%
    echo Run git_bootstrap_auto_remote_fixed.bat first.
    pause
    exit /b 1
)

echo Current folder:
echo %CD%
echo.

echo [1/5] git status
git status
echo.

echo [2/5] git add .
git add .
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

echo.
echo [3/5] git commit -m "Backup before changes"
git diff --cached --quiet
if not errorlevel 1 (
    echo Nothing staged to commit. Skipping commit.
) else (
    git commit -m "Backup before changes"
    if errorlevel 1 (
        echo [ERROR] git commit failed.
        pause
        exit /b 1
    )
)

echo.
echo [4/5] Checking remote origin...
git remote get-url origin >nul 2>nul
if errorlevel 1 (
    echo No origin remote configured.
    echo Suggested remote:
    echo %AUTO_REMOTE%
    echo.
    set /p "REMOTE_URL=Remote URL [Enter = use suggested, N = skip push, or paste custom URL]: "

    if /I "!REMOTE_URL!"=="N" (
        echo Skipped remote setup and push.
        pause
        exit /b 0
    )

    if "!REMOTE_URL!"=="" set "REMOTE_URL=%AUTO_REMOTE%"

    git remote add origin "!REMOTE_URL!"
    if errorlevel 1 (
        echo [ERROR] Failed to add remote origin.
        pause
        exit /b 1
    )
)

echo Remote:
git remote -v
echo.

echo [5/5] git push
git push
if errorlevel 1 (
    echo.
    echo Normal git push failed. Trying first-time upstream push:
    git push -u origin main
    if errorlevel 1 (
        echo.
        echo [WARN] Push failed.
        echo If GitHub already has unrelated history, use:
        echo   git push -u origin main:recovered-local
        pause
        exit /b 1
    )
)

echo.
echo Done.
pause
endlocal
