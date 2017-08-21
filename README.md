[![Discord](https://discordapp.com/api/guilds/230191591640268800/widget.png)](https://discord.gg/8uHZykr) ![Build Status](http://server.torchapi.net:8080/job/Torch/job/Torch/job/master/badge/icon)

# What is Torch?
Torch is the successor to SE Server Extender and gives server admins the tools they need to keep their Space Engineers servers running smoothly. It features a user interface with live management tools and a plugin system so you can run your server exactly how you'd like. Torch is still in early development so there may be bugs and incomplete features.

# Features
* WPF-based user interface
* Chat: interact with the game chat and run commands without having to join the game.
* Entity manager: realtime modification of ingame entities such as stopping grids and changing block settings without having to join the game
* Organized, easy to use configuration editor
* Extensible using the Torch plugin system

# Installation

* Get the latest Torch release here: https://github.com/TorchAPI/Torch/releases
* Unzip the Torch release into its own directory and run the executable. It will automatically download the SE DS and generate the other necessary files.
  - If you already have a DS installed you can unzip the Torch files into the folder that contains the DedicatedServer64 folder.

# Building
To build Torch you must first have a complete SE Dedicated installation somewhere. Before you open the solution, run the Setup batch file and enter the path of that installation's DedicatedServer64 folder. The script will make a symlink to that folder so the Torch solution can find the DLL references it needs.

In both cases you will need to set the InstancePath in TorchConfig.xml to an existing dedicated server instance as Torch can't fully generate it on its own yet.

# Official Plugins
Install plugins by unzipping them into the 'Plugins' folder which should be in the same location as the Torch files. If it doesn't exist you can simply create it.
* [Essentials](https://github.com/TorchAPI/Essentials): Adds a slew of chat commands and other tools to help manage your server.
* [Concealment](https://github.com/TorchAPI/Concealment): Adds game logic and physics optimizations that significantly improve sim speed.

If you have a more enjoyable server experience because of Torch, please consider supporting us on Patreon.
[![Patreon](http://i.imgur.com/VzzIMgn.png)](https://www.patreon.com/bePatron?u=847269)!
