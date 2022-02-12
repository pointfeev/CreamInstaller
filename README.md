### CreamInstaller: CreamAPI Generator & Installer


![Program Preview Image](https://i.imgur.com/BxGU99q.png)

###### Refer to [this post](https://cs.rin.ru/forum/viewtopic.php?f=29&t=70576) if you don't know what CreamAPI is! ;)

###### The program utilizes CreamAPI v4.5.0.0 Hotfix downloaded from that post and embedded into the program itself; no download necessary on your part!
---
#### Description:
Automatically generates and installs (or uninstalls) CreamAPI files for Steam games on the user's computer.
It can also generate and install (or uninstall) CreamAPI for the Paradox Launcher should the user select a Paradox Interactive game.

---
#### Features:
* Automatic downloading and installing of SteamCMD. *for gathering appinfo such as name, buildid, listofdlc, depots*
* Automatic gathering and caching of appinfo for **ALL** installed Steam games and **ALL** of their DLCs.
* Automatic generation of cream_api.ini configuration and installation of CreamAPI DLLs.
* Automatic uninstallation of CreamAPI DLLs and cream_api.ini configuration.

---
#### Installation:
1. Click [here](https://github.com/pointfeev/CreamInstaller/releases/latest/download/CreamInstaller.zip) to download the latest release from [GitHub](https://github.com/pointfeev/CreamInstaller).
2. Extract the executable to anywhere on your computer you want. *it's completely self-contained*

---
#### Usage:
1. Start the program executable.
2. Wait for the program to download and install SteamCMD. *very fast, depends on internet speed*
3. Wait for the program to gather and cache installed Steam games and their DLCs. *may take a good amount of time on the first run, depends on how many games you have installed and how many DLCs they have*
4. **CAREFULLY** select what games and DLCs you wish to unlock. *CreamAPI is not tested for every game!*
5. Click the **Generate and Install** button.
6. Click the **OK** button to close the program.
7. If CreamAPI doesn't work for any of the games you installed it on, simply go back to step 4 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

---
##### Bugs/Crashes/Issues:
All bugs, crashes, and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!

---
##### More Information:
* You can right click on any game or DLC in the selection tree view to open a context menu with multiple shortcuts.
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) using [Onova](https://github.com/Tyrrrz/Onova). *updates can be ignored*
* The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).
