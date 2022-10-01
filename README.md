[![Discord](https://discordapp.com/api/guilds/230191591640268800/widget.png)](https://discord.gg/8uHZykr) <!--- [![Build Status]  (https://build.torchapi.com/job/Torch/job/master/badge/icon)](https://build.torchapi.com/job/Torch/job/master/) -->

# What is Torch?
Torch is the successor to SE Server Extender and gives server admins the tools they need to keep their Space Engineers servers running smoothly. It features a user interface with live management tools and a plugin system so you can run your server exactly how you'd like. Torch is still in early development so there may be bugs and incomplete features.

## Torch.Server

### Features
* WPF-based user interface
* Chat: interact with the game chat and run commands without having to join the game.
* Entity manager: realtime modification of ingame entities such as stopping grids and changing block settings without having to join the game
* Organized, easy to use configuration editor
* Extensible using the Torch plugin system

### Installation

* Get the latest Torch release here: https://build.torchapi.com/job/Torch/job/master/lastSuccessfulBuild/artifact/bin/torch-server.zip
* Unzip the Torch release into its own directory and run the executable. It will automatically download the SE DS and generate the other necessary files.
  - If you already have a DS installed you can unzip the Torch files into the folder that contains the DedicatedServer64 folder.

# Building
To build Torch you must first have a complete SE Dedicated installation somewhere. Before you open the solution, run the Setup batch file and enter the path of that installation's DedicatedServer64 folder. The script will make a symlink to that folder so the Torch solution can find the DLL references it needs.

If you have a more enjoyable server experience because of Torch, please consider supporting us on Patreon. (https://www.patreon.com/TorchSE)

