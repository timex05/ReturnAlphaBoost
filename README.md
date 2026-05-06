# ReturnAlphaBoost

A small Windows tool that replaces the default Rocket League Bubble Boost with  classic Alpha Boost.

## About

ReturnAlphaBoost automatically detects your Rocket League installation and replaces the standard "Bubbles" boost audio and visual effects with the nostalgic Alpha Boost. The tool modifies game files locally and is **not bannable by Easy Anti-Cheat (EAC)**.

## Features

- ✅ Automatic Rocket League installation detection
- ✅ Manual installation path selection support
- ✅ Loads its JSON config from GitHub Raw by default, with local fallback
- ✅ Supports `local` and `online` source modes in JSON
- ✅ Replaces Bubbles boost with Alpha Boost
- ✅ Safe and non-detectable by anti-cheat
- ✅ Steam and Epic Games (Only Verified on Epic Games)

## Usage

1. **Download** the latest `ReturnAlphaBoost.exe` from [Releases](https://github.com/timex05/ReturnAlphaBoost/releases)
2. **Run** the executable
3. The tool will **automatically detect** your Rocket League installation
   - Or manually **browse** to select your Rocket League install folder (can be found through Epic Games Launcher --> Library --> Rocket League --> Manage --> Open Install Location)
4. Click **"Replace Bubbles with Alpha Boost"** to apply the changes
5. The tool reads `returnalphaboost.config.json`, downloads it from GitHub Raw when available, and copies the configured files over the target game files

## Configuration

The replacement behavior is now driven by `returnalphaboost.config.json`. The app tries GitHub Raw first and falls back to the local file next to the executable.

Example:

```json
{
   "profiles": {
      "bubbles": {
         "type": "local",
         "sourceRoot": "TAGame",
         "mappings": [
            {
               "source": "Boost_AlphaReward_SF.upk",
               "target": "Boost_Bubble_SF.upk"
            },
            {
               "source": "SFX_Boost_Alpha.bnk",
               "target": "SFX_Boost_Bubbles.bnk"
            }
         ]
      },
      "bubbles_online": {
         "type": "online",
         "sourceRoot": "https://raw.githubusercontent.com/timex05/ReturnAlphaBoost/main/alpha_files/",
         "mappings": [
            {
               "source": "Boost_Bubble_SF.upk",
               "target": "Boost_Bubble_SF.upk"
            },
            {
               "source": "SFX_Boost_Bubbles.bnk",
               "target": "SFX_Boost_Bubbles.bnk"
            }
         ]
      }
   }
}
```

`type: "local"` means the app copies files from `sourceRoot` under your Rocket League install. `type: "online"` means the app downloads the source files from the `sourceRoot` GitHubusercontent URL and overwrites the `target` files in the game folder.

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