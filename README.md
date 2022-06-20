### CreamInstaller: SmokeAPI/ScreamAPI Installer & Configuration Generator

![Program Preview Image](https://i.imgur.com/c8bAUwL.png)

###### Refer to [this post](https://cs.rin.ru/forum/viewtopic.php?f=29&t=122487) and/or [this repository](https://github.com/acidicoala/SmokeAPI) if you don't know what SmokeAPI is! ;;)
###### Refer to [this post](https://cs.rin.ru/forum/viewtopic.php?f=29&t=106474) and/or [this repository](https://github.com/acidicoala/ScreamAPI) if you don't know what ScreamAPI is! ;)

###### The program utilizes SmokeAPI v1.0.3 and ScreamAPI v3.0.1 downloaded from those posts and embedded into the program itself; no download necessary on your part!
---
#### Description:
Automatically finds all installed Steam and Epic games with their respective Steamworks and Epic Online Services DLL locations on the user's computer,
parses SteamCMD, Steam Store, and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface utilizing the gathered information.

The primary function of the program is to **automatically generate and install SmokeAPI and/or ScreamAPI** for whichever
games and DLCs the user selects, however through the use of the **right-click context menu** the user can also:
* automatically repair the Paradox Launcher
* open parsed Steam and/or Epic Games appinfo in Notepad(++)
* refresh parsed Steam and/or Epic Games appinfo
* open root game directories and Steamworks/Epic Online Services DLL directories in Explorer
* open SteamDB, ScreamDB, Steam Store, Epic Games Store and Steam Community links in the default browser

---
#### Features:
* Automatic downloading and installing of SteamCMD (if a Steam game is chosen). *for gathering appinfo such as name, buildid, listofdlc, depots*
* Automatic gathering and caching of appinfo for **ALL** installed Steam and Epic games and **ALL** of their DLCs.
* Automatic generation of SmokeAPI.json and/or ScreamAPI.json configuration and installation of SmokeAPI and/or ScreamAPI DLLs.
* Automatic uninstallation of CreamAPI, SmokeAPI, and/or ScreamAPI DLLs and cream_api.ini, SmokeAPI.json, and/or ScreamAPI.json configuration.
* Automatic repairing of the Paradox Launcher via the right-click context menu "Repair" option. *for when the launcher updates whilst you have CreamAPI, SmokeAPI or ScreamAPI installed to it*

---
#### Installation:
1. Click [here](https://github.com/pointfeev/CreamInstaller/releases/latest/download/CreamInstaller.zip) to download the latest release from [GitHub](https://github.com/pointfeev/CreamInstaller).
2. Extract the executable to anywhere on your computer you want. *it's completely self-contained*

---
### **NOTE:** This program does not automatically download nor install actual DLC files for you. As the title of the program says, it's only a SmokeAPI/ScreamAPI installer. Should the game you wish to unlock DLC for not already come with the DLCs installed (very many do not), you have to find, download, and install those yourself. Preferably, you should be referring to the proper cs.rin.ru post for the game(s) you're tinkering with; you'll usually find any answer to your problems there.

---
#### Usage:
1. Start the program executable.
2. Choose which programs/games the program should scan for DLC. *the program automatically gathers all installed games from Steam and Epic directories*
3. Wait for the program to download and install SteamCMD (if you chose a Steam game). *very fast, depends on internet speed*
4. Wait for the program to gather and cache the chosen Steam/Epic games' appinfo & DLCs. *may take a good amount of time on the first run, depends on how many games you chose and how many DLCs they have*
5. **CAREFULLY** select which games' DLCs you wish to unlock. *SmokeAPI and ScreamAPI are not tested for every game!*
6. Click the **Generate and Install** button.
7. Click the **OK** button to close the program.
8. If SmokeAPI or ScreamAPI don't work for any of the games you installed them on, simply go back to step 5 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

---
##### Bugs/Crashes/Issues:
All bugs, crashes, and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!

---
##### More Information:
* You can right click on any game or DLC in the selection tree view to open a context menu with multiple shortcuts.
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) using [Onova](https://github.com/Tyrrrz/Onova). *updates can be ignored*
* The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).
