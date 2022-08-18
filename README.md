### CreamInstaller: Automatic DLC Unlocker Installer & Configuration Generator

![Program Preview Image](https://raw.githubusercontent.com/pointfeev/CreamInstaller/main/preview.png)

###### The program utilizes the latest versions of [SmokeAPI](https://github.com/acidicoala/SmokeAPI), [ScreamAPI](https://github.com/acidicoala/ScreamAPI), [Uplay R1 Unlocker](https://github.com/acidicoala/UplayR1Unlocker) and [Uplay R2 Unlocker](https://github.com/acidicoala/UplayR2Unlocker), all by the wonderful [acidicoala](https://github.com/acidicoala), and all downloaded from the posts above and embedded into the program itself; no further downloads necessary on your part!
---
#### Description:
Automatically finds all installed Steam, Epic and Ubisoft games with their respective DLC-related DLL locations on the user's computer,
parses SteamCMD, Steam Store and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface utilizing the gathered information.

The primary function of the program is to **automatically generate and install DLC unlockers** for whichever
games and DLCs the user selects; however, through the use of **right-click context menus** the user can also:
* automatically repair the Paradox Launcher
* open parsed Steam and/or Epic Games appinfo in Notepad(++)
* refresh parsed Steam and/or Epic Games appinfo
* open root game directories and important DLL directories in Explorer
* open SteamDB, ScreamDB, Steam Store, Epic Games Store, Steam Community, Ubisoft Store, and official game website links (where applicable) in the default browser

---
#### Features:
* Automatic download and installation of SteamCMD as necessary whenever a Steam game is chosen. *for gathering appinfo such as name, buildid, listofdlc, depots*
* Automatic gathering and caching of appinfo for all selected Steam and Epic games and **ALL** of their DLCs.
* Automatic generation of SmokeAPI.json, ScreamAPI.json, UplayR1Unlocker.jsonc and/or UplayR2Unlocker.jsonc configuration and installation of DLC unlocker DLLs.
* Automatic uninstallation of DLC unlocker DLLs and SmokeAPI.json, ScreamAPI.json, UplayR1Unlocker.jsonc and/or UplayR2Unlocker.jsonc configurations.
* Automatic reparation of the Paradox Launcher via the right-click context menu "Repair" option. *for when the launcher updates whilst you have CreamAPI, SmokeAPI or ScreamAPI installed to it*

---
#### Installation:
1. Click [here](https://github.com/pointfeev/CreamInstaller/releases/latest/download/CreamInstaller.zip) to download the latest release from [GitHub](https://github.com/pointfeev/CreamInstaller).
2. Extract the executable to anywhere on your computer you want. *it's completely self-contained*

---
#### **NOTE:** This program does not automatically download nor install actual DLC files for you. As the title of the program says, it's only a DLC Unlocker installer. Should the game you wish to unlock DLC for not already come with the DLCs installed (very many do not), you have to find, download, and install those yourself. Preferably, you should be referring to the proper cs.rin.ru post for the game(s) you're tinkering with; you'll usually find any answer to your problems there.

---
#### Usage:
1. Start the program executable.
2. Choose which programs/games the program should scan for DLC. *the program automatically gathers all installed games from Steam, Epic and Ubisoft directories*
3. Wait for the program to download and install SteamCMD (if you chose a Steam game). *very fast, depends on internet speed*
4. Wait for the program to gather and cache the chosen Steam and/or Epic games' appinfo & DLCs. *may take a good amount of time on the first run, depends on how many games you chose and how many DLCs they have*
5. **CAREFULLY** select which games' DLCs you wish to unlock. *Obviously none of the DLC unlockers are tested for every single game!*
6. Click the **Generate and Install** button.
7. Click the **OK** button to close the program.
8. If any of the DLC unlockers cause problems with any of the games you installed them on, simply go back to step 5 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

---
##### Bugs/Crashes/Issues:
For reliable and quick assistance, all bugs, crashes and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!

---
##### More Information:
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) using [Onova](https://github.com/Tyrrrz/Onova). *updates can be ignored*
* The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).
