### CreamInstaller: CreamAPI/ScreamAPI Installer & Configuration Generator

![Program Preview Image](https://i.imgur.com/VAx1LRa.png)

###### Refer to [this post](https://cs.rin.ru/forum/viewtopic.php?f=29&t=70576) if you don't know what CreamAPI is! ;;)
###### Refer to [this post](https://cs.rin.ru/forum/viewtopic.php?f=29&t=106474) and/or [this repository](https://github.com/acidicoala/ScreamAPI) if you don't know what ScreamAPI is! ;)

###### The program utilizes CreamAPI v4.5.0.0 Hotfix and ScreamAPI v3.0.0 downloaded from those posts and embedded into the program itself; no download necessary on your part!
---
#### Description:
Automatically finds all installed Steam/Epic games and their respective Steamworks/Epic Online Services DLL locations on the user's computer,
automatically parses SteamCMD, Steam Store, and Epic Games Store for those games' DLCs, then provides a very simple graphical interface utilizing the gathered information.

The primary function of the program is to **automatically generate and install CreamAPI or ScreamAPI** for whichever
games and DLCs the user selects, however through the use of the **right-click context menu** the user can also:
* automatically repair the Paradox Launcher
* open the parsed SteamCMD/GraphQL AppInfo in Notepad
* refresh the parsed SteamCMD/GraphQL AppInfo
* open Root directories or Steamworks/Epic Online Services DLL directories in Explorer
* open SteamDB/ScreamDB/Steam Store/Epic Games Store/Steam Community links in the default browser

---
#### Features:
* Automatic downloading and installing of SteamCMD (if Steam is installed). *for gathering appinfo such as name, buildid, listofdlc, depots*
* Automatic gathering and caching of appinfo for **ALL** installed Steam and Epic games and **ALL** of their DLCs.
* Automatic generation of cream_api.ini/ScreamAPI.json configuration and installation of CreamAPI/ScreamAPI DLLs.
* Automatic uninstallation of CreamAPI/ScreamAPI DLLs and cream_api.ini/ScreamAPI.json configuration.
* Automatic repairing of the Paradox Launcher via the right-click context menu "Repair" option. *for when the launcher updates whilst you have CreamAPI or ScreamAPI installed to it*

---
#### Installation:
1. Click [here](https://github.com/pointfeev/CreamInstaller/releases/latest/download/CreamInstaller.zip) to download the latest release from [GitHub](https://github.com/pointfeev/CreamInstaller).
2. Extract the executable to anywhere on your computer you want. *it's completely self-contained*

---
#### Usage:
1. Start the program executable.
2. Choose which programs/games the program should scan for DLC.
3. Wait for the program to download and install SteamCMD (if you have Steam installed). *very fast, depends on internet speed*
4. Wait for the program to gather and cache installed Steam and Epic games and their DLCs. *may take a good amount of time on the first run, depends on how many games you have installed and how many DLCs they have*
5. **CAREFULLY** select what games and DLCs you wish to unlock. *CreamAPI and ScreamAPI are not tested for every game!*
6. Click the **Generate and Install** button.
7. Click the **OK** button to close the program.
8. If CreamAPI or ScreamAPI don't work for any of the games you installed them on, simply go back to step 4 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

---
##### Bugs/Crashes/Issues:
All bugs, crashes, and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!

---
##### More Information:
* You can right click on any game or DLC in the selection tree view to open a context menu with multiple shortcuts.
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) using [Onova](https://github.com/Tyrrrz/Onova). *updates can be ignored*
* The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).
