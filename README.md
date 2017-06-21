Discord: [![Discord](https://discordapp.com/api/guilds/230191591640268800/widget.png)](https://discord.gg/8uHZykr)

# What is Torch?
Torch is the successor to SE Server Extender and gives server admins the tools they need to keep their Space Engineers servers running smoothly. It features a user interface with live management tools and a plugin system so you can run your server exactly how you'd like. Torch is still in early development so there may be bugs and incomplete features.

# Features
* WPF-based user interface
* Chat: interact with the game chat and run commands without having to join the game.
* Entity manager: realtime modification of ingame entities such as stopping grids and changing block settings without having to join the game
* Organized, easy to use configuration editor
* Extensible using the Torch plugin system

# Installation Guide

Note: Until Torch is in a stable, nearly feature complete state there will not be any binaries available. You'll have to compile the solution yourself.

### Automatic (recommended)
* Unzip Torch to its own folder, run Torch.Server.exe and enter 'y' in the prompt for automatic updates. Torch will automatically download the Space Engineers files and generate all of the configs/folders necessary.

### Manual (for hosting companies or the paranoid)
* Install the Space Engineers DS and then unzip the Torch files into the server's DedicatedServer64 directory. It will automatically detect the manual install and disable automatic updates.

In both cases you will need to set the InstancePath in TorchConfig.xml to an existing dedicated server instance as Torch can't fully generate it on its own yet.

# Official Plugins
Install plugins by unzipping them into the 'Plugins' folder which should be in the same location as the Torch files. If it doesn't exist you can simply create it.
* [Essentials](https://github.com/TorchAPI/Essentials): Adds a slew of chat commands and other tools to help manage your server.
* [Concealment](https://github.com/TorchAPI/Concealment): Adds game logic and physics optimizations that significantly improve sim speed.

If you have a more enjoyable server experience because of Torch, please consider supporting us on [Patreon](https://www.patreon.com/bePatron?u=847269)!
