# overtools [![Build status](https://ci.appveyor.com/api/projects/status/5quie68hde5e1hs2?svg=true)](https://ci.appveyor.com/project/yretenai/owlib) [![Discord](https://img.shields.io/discord/346445737367699456.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/XM93ZdB)

Series of programs (tools) to interact with the Overwatch files.

**.NET 5 is required. Download here: https://dotnet.microsoft.com/download/dotnet/5.0**

## Downloads
You can find mostly stable releases on the AppVeyor page here:  
https://ci.appveyor.com/project/yretenai/owlib/history

## Help & Discussion
For most discussion related to the tools and for support, join our Discord. https://discord.gg/XM93ZdB  


You can also find some help and tutorials on the wiki here:  
https://owdev.wiki/Main_Page

## How to use
Run DataTool.exe for a list of help and supported commands.  
Most commands follow the structure `DataTool.exe <overwatch_directory> <mode> [mode args]`

### Example List Commands
```
DataTool.exe "C:\Games\Overwatch" list-heroes
DataTool.exe "C:\Games\Overwatch" list-unlocks
DataTool.exe "C:\Games\Overwatch" list-maps
```

### Example Extract Commands
Extract commands follow the struture `DataTool.exe <overwatch_directory> <mode> <output_directory> [filters]`  
Filters follow the format `{hero name}|{type}=({tag name}={tag}),{item name}`. You can specify `*` for the hero name or the type for everything.  
Valid types include: skin, icon, spray, victorypose, emote, voiceline

#### Example Filters
```
"*"                                   // Everything
"*|skin=*"                            // All Heroes Skins
"Lúcio|skin=Classic"                  // Lucio's Classic Skin
"Torbjörn|skin=(rarity=legendary)"    // Torbjörn's Legendary Skins
"D.Va|skin=(event=summergames)"       // D.Va's Summer Games skins
"Reaper|spray=*"                      // Reaper's sprays
"Reaper|spray=(event=!halloween)"     // Reaper's sprays that are not from Halloween
"Reaper|spray=!Cute,*"                // Reaper's sprays except "Cute" spray
"Soldier: 76|skin=Daredevil: 76" "Roadhog|spray=Pixel" // Soldier 76's Daredevil skin and Roadhogs Pixel spray
```

#### Example Commands
```
Tracers Classic Skin (You can enter the name of any skin):
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "Tracer|skin=Classic"

All Heroes Classic Skins:
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "*|skin=Classic"

All Heroes Skins (will take long time):
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" "*|skin=*"

Everything - includes skins, emotes, highlight intros, etc. (will take very long time)
DataTool.exe "C:\Games\Overwatch" extract-unlocks "C:\Games\Extracts" *

Extract Dorado map
DataTool.exe "C:\Games\Overwatch" extract-maps "C:\Games\Extracts" "Dorado"

Extract All Maps (will take a long time)
DataTool.exe "C:\Games\Overwatch" extract-maps "C:\Games\Extracts" *
```

## Disclaimer
This project is not affiliated with Blizzard Entertainment, Inc.  
All trademarks referenced herein are the properties of their respective owners.  
2021 Blizzard Entertainment, Inc. All rights reserved.
