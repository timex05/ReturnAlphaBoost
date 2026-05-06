# ReturnAlphaBoost

A small Windows tool that replaces the default Rocket League Bubble Boost with  classic Alpha Boost.

## About

ReturnAlphaBoost automatically detects your Rocket League installation and replaces the standard "Bubbles" boost audio and visual effects with the nostalgic Alpha Boost. The tool modifies game files locally and is **not banable by Easy Anti-Cheat (EAC)**.

## Features

- ✅ Automatic Rocket League installation detection
- ✅ Manual installation path selection support
- ✅ Downloads Alpha Boost files directly from GitHub
- ✅ Replaces Bubbles boost with Alpha Boost
- ✅ Safe and non-detectable by anti-cheat
- ✅ Steam and Epic Games (Only Verified on Epic Games)

## Usage

1. **Download** the latest `ReturnAlphaBoost.exe` from [Releases](https://github.com/timex05/ReturnAlphaBoost/releases)
2. **Run** the executable
3. The tool will **automatically detect** your Rocket League installation
   - Or manually **browse** to select your Rocket League install folder (can be found through Epic Games Launcher --> Library --> Rocket League --> Manage --> Open Install Location)
4. Click **"Replace Bubbles with Alpha Boost"** to apply the changes
5. The tool downloads the Alpha Boost files from GitHub and replaces the Bubbles files in your game installation

## Undo Changes

To revert to the default Bubbles boost effect:

1. Open **Epic Games Launcher**
2. Go to **Library** → **Rocket League**
3. Click **Manage** → **Verify** or **Reinstall** the game

This will restore all original game files to their default state.

## Run Locally (Development)

Clone Repository

```powershell
git clone https://github.com/timex05/ReturnAlphaBoost.git
```

Build local:

```powershell
dotnet build
```

Start local

```powershell
dotnet run
```