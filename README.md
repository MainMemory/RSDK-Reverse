# RSDK-Reverse
A collection of libraries for the RSDK (Retro Engine Software Development Kit) + A few basic tools 

This repository contains libaries for all versions of the RSDK (and a few tools for editing certain file formats):
- RSDK 1: A libary for the Retro-Sonic Development Kit (Retro Sonic). 

- RSDK 2: A libary for RSDKv1 (Sonic Nexus).

- RSDK 3: A libary for RSDKv2 (Sonic CD (2011)).

- RSDK 4: A libary for RSDKvB (Sonic 1 and Sonic 2 Mobile Remakes).

- RSDK 5: A libary for RSDKv5 (Sonic Mania). This was made by koolkdev/EyeKey, with a few minor tweaks by me. original download here: https://github.com/koolkdev/ManiacEditor

- RetroED: A set of tools designed to modify the filetypes used by Retro Engine versions below version 5

- Test: Just A program i used to test various functions, nothin' to see here!

# Special Thanks:
koolkdev/EyeKey: For developing the RSDKv5 libaries

NextVolume/tails92: For making TaxED, Which I used as a guide to certain filetypes.

# NOTE:
This is a WIP (Work In Progress), So some things may not work as intended or not be finished at al right now...


# Retro-Sonic Engine Differences
Dreamcast Retro-Sonic filetypes that dont work with the RSDK 1 Lib: 
- .tcf Files? (different values, but may still work, Unknown since Retro-Sonic 2007's .tcf files have not been Reverse Engineered yet)

Dreamcast Retro-Sonic filetypes that DO work with the RSDK 1 Lib: 
 - Animations (Just had to change a check a value and set another to 2 if its the dreamcast version)
 - .zcf (Zone Config)
 - .til (Tiles/128x128Tiles)
 - .map (Tile Map)
 - .itm (Object Locations)

Misc.
- Music doesnt play with any audio player that i've tried (VLC, windows movie player and groove music)
- SFX files are stored in standard .wav for both versions
- There are some .mdf files located in Data\TitleScr in the both versions, i don't know what they are for...
