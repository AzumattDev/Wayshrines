### Call upon Heimdall to take you to other wayshrines or back to your set spawn point. The Wayshrine is surrounded by a small 'NoMonsterArea' to aid in your escape. You will not be targeted by monsters in this area.



## Things you need to know


* I highly recommend using SpawnThat to allow the Wayshrine to appear around the map (if someone makes a decent one, I will begin to include them in the download)
* When using the Wayshrine you will shout out to Heimdall to take you to other Wayshrines around the map OR taking you to your set spawn point (Original functionality required for spawn point teleport. If you do not have a bed, it takes you to world spawn point).
* `Only admins can build/destroy the Wayshrines. Syncs with server admin list.`
* Config can be changed on while on the server by an admin using Configuration Managers (Default BepInEx or Aedenthorn's), no need for reboot. Changing the config file manually, will require a reboot of the server.
* **COMMAND**: delway in console or /delway in chat to delete all Wayshrines found in the world.

## Installation Instructions

>***You must have BepInEx installed correctly! I can not stress this enough.***

#### Windows (Steam)
1. Locate your game folder manually or start Steam client and :
   a. Right click the Valheim game in your steam library
   b. "Go to Manage" -> "Browse local files"
   c. Steam should open your game folder
2. Extract the contents of the archive into the BepInEx/plugins folder. Config file will generate on first boot.
3. Your config file will be located in BepInEx/config. Though, I do recommend getting a configuration manager to change the config from within the game.

#### Server

Must be installed on both the client and the server for syncing admins/configs to work properly.
1. Locate your main folder manually and :
   a. Extract the contents of the archive into the BepInEx/plugins folder.
   b. Locate Azumatt.Wayshrine.cfg under BepInEx\config on your machine and configure the mod to your needs
2. Reboot your server.

## Author Information


Azumatt

DISCORD: Azumatt#2625

For Questions or Comments, find me﻿ in the Odin Plus Team Discord:
[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/5gXNxNkUBt)

STEAM: https://steamcommunity.com/id/azumatt/


***
> ### Version 1.0.6
> * Complete re-write of code. Should be fixed for Hearth and Home, other mod compatibility, and known error from 1.0.5 fixed.
> * Admin command moved to H&H methods of doing them. delway in console or /delway in chat to delete all Wayshrines found in the world.
> * I lost some of the original Unity project and had to rebuild the assets, so there might be some texture/particle differences.
> * I removed the spawn that config file in this version as I don't know how good that file is anymore. Feel free to make and provide one if you wish. I'll include it in future downloads.
> ### Version 1.0.5
> * More singleplayer fixes
> * Customize shout message
> * Map pin fixes for Multiplayer and Singleplayer
> * Additional Wayshrine added
> * Map pins now specific to Wayshrine
> * NOTE: Known error output on entering the skull Wayshrine's collider for teleport
> ### Version 1.0.4
> * Single player FPS issue fixed. Map pins are a little broken, they do not update automatically like they do in Multiplayer. Not sure why, working on fixes.
> * Admin command to remove ALL wayshrines from the world regardless of distance or owner. Use !delway or !deletewayshrines in the console. Will work to add this to chat, testing with VChat when using the command caused game crashes after the latest game update.
> * Config options added
> *  General refactoring of code with some optimizations.
> ### Version 1.0.3
> * Fixed bug that causes issues with Jotunn mod's recipes
> ### Version 1.0.2
> * NOTE: Feel free to upload your own pictures and videos of this mod to the mod page!	
> * Wayshrine_Skull added, has double functionality for original and world wayshrine teleports. Interact for original..walk onto skull for world teleport	
> * Config options added. Server admins can change configs while on the server using configuration managers, no need for reboots	
> * Wayshrines are now linked together. Interact with them to choose one on the map. Map pins will periodically update if they don't immediately show when a wayshrine spawns/is placed.	
> * Wayshrines have visual changes to begin reflecting biomes in a better manner	
> * Assets reduced in size to help with file size overall.	
> * Custom bifrost effect with thunderclap sound on teleport