REM SPDX-FileCopyrightText: 2018 DamianX <DamianX@users.norply.github.com>
REM SPDX-FileCopyrightText: 2019 Silver <Silvertorch5@gmail.com>
REM SPDX-FileCopyrightText: 2019 clusterfack <8516830+clusterfack@users.norply.github.com>
REM SPDX-FileCopyrightText: 2021 SweptWasTaken <sweptwastaken@protonmail.com>
REM SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
REM SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.norply.github.com>
REM
REM SPDX-License-Identifier: AGPL-3.0-or-later

@echo off
echo Building and running server in Release mode...
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Press any key to exit.
    pause
    exit /b 1
)
echo Build successful! Starting server...
dotnet run --project Content.Goobstation.Server --configuration Release
pause
