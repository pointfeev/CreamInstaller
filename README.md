### CreamInstaller: Automatic DLC Unlocker Installer & Configuration Generator

![Program Preview Image](https://raw.githubusercontent.com/pointfeev/CreamInstaller/main/preview.png)

###### **NOTE:** This is simply a preview image; this is not a list of supported games nor configurations!

##### The program utilizes the latest versions of [Koaloader](https://github.com/acidicoala/Koaloader), [SmokeAPI](https://github.com/acidicoala/SmokeAPI), [ScreamAPI](https://github.com/acidicoala/ScreamAPI), [Uplay R1 Unlocker](https://github.com/acidicoala/UplayR1Unlocker) and [Uplay R2 Unlocker](https://github.com/acidicoala/UplayR2Unlocker), all by the wonderful [acidicoala](https://github.com/acidicoala), and all downloaded from the posts above and embedded into the program itself; no further downloads necessary on your part!
---
#### Description:
Automatically finds all installed Steam, Epic and Ubisoft games with their respective DLC-related DLL locations on the user's computer,
parses SteamCMD, Steam Store and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface
utilizing the gathered information for the maintenance of DLC unlockers.

The primary function of the program is to **automatically generate and install DLC unlockers** for whichever
games and DLCs the user selects; however, through the use of **right-click context menus** the user can also:
* automatically repair the Paradox Launcher
* open parsed Steam and/or Epic Games appinfo in Notepad(++)
* refresh parsed Steam and/or Epic Games appinfo
* open root game directories and important DLL directories in Explorer
* open SteamDB, ScreamDB, Steam Store, Epic Games Store, Steam Community, Ubisoft Store, and official game website links (where applicable) in the default browser

---
#### Features:
* Automatic download and installation of SteamCMD as necessary whenever a Steam game is chosen. *For gathering appinfo such as name, buildid, listofdlc, depots, etc.*
* Automatic gathering and caching of information for all selected Steam and Epic games and **ALL** of their DLCs.
* Automatic DLL installation and configuration generation for Koaloader, SmokeAPI, ScreamAPI, Uplay R1 Unlocker and Uplay R2 Unlocker.
* Automatic uninstallation of DLLs and configurations for Koaloader, CreamAPI, SmokeAPI, ScreamAPI, Uplay R1 Unlocker and Uplay R2 Unlocker.
* Automatic reparation of the Paradox Launcher (and manually via the right-click context menu "Repair" option). *For when the launcher updates whilst you have CreamAPI, SmokeAPI or ScreamAPI installed to it.*

---
#### Installation:
1. Click [here](https://github.com/pointfeev/CreamInstaller/releases/latest/download/CreamInstaller.zip) to download the latest release from [GitHub](https://github.com/pointfeev/CreamInstaller).
2. Extract the executable from the ZIP file to anywhere on your computer you want. *It's completely self-contained.*

If the program doesn't seem to launch, try downloading and installing [.NET Desktop Runtime 8.0.4](https://download.visualstudio.microsoft.com/download/pr/c1d08a81-6e65-4065-b606-ed1127a954d3/14fe55b8a73ebba2b05432b162ab3aa8/windowsdesktop-runtime-8.0.4-win-x64.exe) and restarting your computer. Note that the program currently only supports Windows 8+ 64-bit machines, as .NET 7+ no longer support Windows 7.

---
#### Usage:
1. Start the program executable. *Read above under Installation if it doesn't launch.*
2. Choose which programs and/or games the program should scan for DLC. *The program automatically gathers all installed games from Steam, Epic and Ubisoft directories.*
3. Wait for the program to download and install SteamCMD (if you chose a Steam game). *Very fast, depends on internet speed.*
4. Wait for the program to gather and cache the chosen games' information & DLCs. *May take a good amount of time on the first run, depends on how many games you chose and how many DLCs they have.*
5. **CAREFULLY** select which games' DLCs you wish to unlock. *Obviously none of the DLC unlockers are tested for every single game!*
6. Choose whether or not to install with Koaloader, and if so then also pick the proxy DLL to use. *If the default version.dll doesn't work, then see [here](https://cs.rin.ru/forum/viewtopic.php?p=2552172#p2552172) to find one that does.*
7. Click the **Generate and Install** button.
8. Click the **OK** button to close the program.
9. If any of the DLC unlockers cause problems with any of the games you installed them on, simply go back to step 5 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

##### **NOTE:** This program does not automatically download nor install actual DLC files for you; as the title of the program states, this program is only a *DLC Unlocker* installer. Should the game you wish to unlock DLC for not already come with the DLCs installed, as is the case with a good majority of games, you must find, download and install those to the game yourself. This process includes manually installing new DLCs and manually updating the previously manually installed DLCs after game updates.

---
#### FAQ / Common Issues:

**Q:** The program is not launching.
**A:** First and foremost, note that the program currently only supports Windows 8+ 64-bit machines, as .NET 7+ no longer support Windows 7. If that does not apply to you, then make sure you've extracted the executable from the ZIP file before you've launched it, resolved your anti-virus, and have tried downloading the .NET Desktop Runtime mentioned under [installation instructions](https://github.com/pointfeev/CreamInstaller#installation) above and restarting your computer. If none of the above work, then I simply cannot do anything about it, I do not control .NET. Either your system is not supported by the current version of .NET, or something is wrong/corrupted with your system.

**Q:** The game I installed the unlocker(s) to is not working/the DLCs are not unlocked.
**A:** Make sure you've read the note under [Usage](https://github.com/pointfeev/CreamInstaller#usage) above! Assuming the program functioned as it was supposed to by properly installing DLC unlockers to your chosen games, this is not an issue I can do anything about and it's entirely up to you to seek the appropriate resources to fix it yourself (hint: https://cs.rin.ru/forum/viewforum.php?f=10).

**Q:** The program and/or files installed by the program are detected as a virus/trojan/malware.
**A:** The "issue" of the program's outputted Koaloader DLLs being detected as false positives such as Mamson.A!ac, Phonzy.A!ml, Wacatac.H!ml, Malgent!MSR, Tiggre!rfn, and many many others, has already been posted and explained dozens of times now in many different manners... please do not post it again, you will just be ignored; instead, refer to the explanations within issue #40 and its linked issues: https://github.com/pointfeev/CreamInstaller/issues/40.

---
##### Bugs/Crashes/Issues:
For reliable and quick assistance, all bugs, crashes and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!

##### **HOWEVER**: Please read the [FAQ entry](https://github.com/pointfeev/CreamInstaller#faq--common-issues) above and/or [template issue](https://github.com/pointfeev/CreamInstaller/issues/new/choose) corresponding to your problem should one exist! Also, note that the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page is not your personal assistance hotline, rather it is for genuine bugs/crashes/issues with the program itself. If you post an issue which is off-topic or has already been explained within the FAQ, template issues, and/or within this text in general, I will just close it and you will be ignored.

---
##### More Information:
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) by choice of the user through a dialog on startup.
* The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).
* Credit to [Mattahan](https://www.mattahan.com) for the program icon.
