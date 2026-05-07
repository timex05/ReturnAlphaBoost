# ReturnAlphaBoost

A small Windows tool that replaces the Bubble Boost with Alpha Boost.

## About

ReturnAlphaBoost automatically detects your Rocket League installation and replaces the standard "Bubbles" boost audio and visual effects with the nostalgic Alpha Boost. The tool modifies game files locally and is **not bannable by Easy Anti-Cheat (EAC)**.

## Features

- ✅ Automatic Rocket League installation detection
- ✅ Manual installation path selection support
- ✅ Loads its JSON config directly from GitHub
- ✅ Downloads replacement files from online URLs in JSON
- ✅ Replaces Bubbles boost with Alpha Boost
- ✅ Safe and non-detectable by anti-cheat
- ✅ Steam and Epic Games (Only Verified on Epic Games)

## Usage

1. **Download** the latest `ReturnAlphaBoost.exe` from [Releases](https://github.com/timex05/ReturnAlphaBoost/releases)
2. **Run** the executable
3. The tool will **automatically detect** your Rocket League installation
   - Or manually **browse** to select your Rocket League install folder (can be found through Epic Games Launcher --> Library --> Rocket League --> Manage --> Open Install Location)
4. Click **"Replace Bubbles with Alpha Boost"** to apply the changes
5. The tool reads `returnalphaboost.config.json` from GitHub Raw and copies the configured files over the target game files

## Configuration

The replacement behavior is driven by `returnalphaboost.config.json` from GitHub Raw.

Example:

```json
{
   "profiles": {
      "bubbles_online": {
         "mappings": [
            {
               "source": "https://raw.githubusercontent.com/timex05/ReturnAlphaBoost/main/alpha_files/Boost_Bubble_SF.upk",
               "target": "Boost_Bubble_SF.upk"
            },
            {
               "source": "https://raw.githubusercontent.com/timex05/ReturnAlphaBoost/main/alpha_files/SFX_Boost_Bubbles.bnk",
               "target": "SFX_Boost_Bubbles.bnk"
            }
         ]
      }
   }
}
```

The app always treats each `source` value as a full online URL and downloads it directly before writing to `target` in the game folder.

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