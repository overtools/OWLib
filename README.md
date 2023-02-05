# overtools [![Build Status](https://github.com/overtools/OWLib/actions/workflows/dotnet.yml/badge.svg)](https://github.com/overtools/OWLib/actions/workflows/dotnet.yml) [![Discord](https://img.shields.io/discord/346445737367699456.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/XM93ZdB)

Series of programs (tools) to interact with the Overwatch files.

## Downloads & Help
Downloads for the tools and updates are posted on our Discord where you can also find support and disccusion around using them.  
Join the Discord here:  https://discord.gg/XM93ZdB  

## How to use
DataTool is a command line application which means you need to know how to use a command line.

0. Make sure Overwatch is installed.
1. Download the latest relase from our Discord
2. Extract the all the files to a folder, do not put it in your Overwatch Directory.
3. Open a command line in the folder where you extracted the files.
4. Run DataTool.exe via command line for a list of help and supported commands.

Most commands follow the structure `DataTool.exe <overwatch_directory> <mode> [mode args]`

### Example List Commands
```
DataTool.exe "C:\Games\Overwatch" list-heroes
DataTool.exe "C:\Games\Overwatch" list-unlocks
DataTool.exe "C:\Games\Overwatch" list-maps
DataTool.exe "C:\Games\Overwatch" list-achievements
```

### Example Extract Commands

Some of the more common extract commands include:
 * extract-unlocks - ex all hero unlocks such as skins, highlight intros, emotes, sprays, icons
 * extract-general - handles extracting all all class unlocks such as all class sprays and icons and portraits
 * extract-maps - extract maps
 * extract-hero-voice-better - extracts all heroes voicelines and groups them by type (kinda)
 
 In most cases when using extract commands, you must provide the name of what you want to extract or use `*` for everything.  
 See below for some examples.


#### Example Commands
```
Tracers Overwatch 1 Skin (You can enter the name of any skin):
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "Tracer|skin=Overwatch 1"

All Heroes Overwatch 1 Skins:
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "*|skin=Overwatch 1"

All Heroes Skins (will take long time):
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "*|skin=*"

Everything - includes skins, emotes, highlight intros, etc. (will take very long time)
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" *

Extract Dorado map
DataTool.exe "C:\Games\Overwatch" extract-maps "C:\Games\Extracts" "Dorado"

Extract All Maps (will take a long time)
DataTool.exe "C:\Games\Overwatch" extract-maps "C:\Games\Extracts" *

Extract Tracers Voicelines
DataTool.exe "C:\Games\Overwatch" extract-hero-voice-better "C:\Games\Extracts" Tracer
```

#### Extract Unlocks filters
The extract unlocks command supports extracting a lot of data and you can filter to specifically what you want.  
The command structure looks like: `DataTool.exe <overwatch_directory> extract-unlocks <output_directory> [filters]`  
Filters follow the format `{hero name}|{type}={item name}`. You can specify `*` for the hero name or the type for everything.  
Valid types include: skin, icon, spray, victorypose, emote, voiceline

#### Example Filters
```
"*"                                   // Everything (all heroes skins, sprays, etc) (very slow)
"*|skin=*"                            // All Heroes Skins
"Tracer|skin=Overwatch 1"             // Tracers Overwatch 1 skin
"Reaper|spray=*"                      // Reaper's sprays
"Reaper|voiceline=*"                  // Reapers unlockable voicelines
```

## Disclaimer
This project is not affiliated with Blizzard Entertainment, Inc.  
All trademarks referenced herein are the properties of their respective owners.  
2023 Blizzard Entertainment, Inc. All rights reserved.
