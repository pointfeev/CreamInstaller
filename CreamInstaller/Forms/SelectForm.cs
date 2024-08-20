﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Platforms.Epic;
using CreamInstaller.Platforms.Epic.Heroic;
using CreamInstaller.Platforms.Paradox;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Platforms.Ubisoft;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Forms;

internal sealed partial class SelectForm : CustomForm
{
    private const string HelpButtonListPrefix = "\n    •  ";

    private static SelectForm current;

    private readonly ConcurrentDictionary<string, string> remainingDLCs = new();

    private readonly ConcurrentDictionary<string, string> remainingGames = new();

    private List<(Platform platform, string id, string name)> programsToScan;

    private SelectForm()
    {
        InitializeComponent();
        selectionTreeView.TreeViewNodeSorter = PlatformIdComparer.NodeName;
        Text = Program.ApplicationName;
    }

    internal static SelectForm Current
    {
        get
        {
            if (current is not null && (current.Disposing || current.IsDisposed))
                current = null;
            return current ??= new();
        }
    }

    private static void UpdateRemaining(Label label, ConcurrentDictionary<string, string> list, string descriptor)
        => label.Text = list.IsEmpty
            ? ""
            : $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list.Values).Replace("&", "&&");

    private void UpdateRemainingGames() => UpdateRemaining(progressLabelGames, remainingGames, "games");

    private void AddToRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingGames[gameName] = gameName;
            UpdateRemainingGames();
        });
    }

    private void RemoveFromRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            _ = remainingGames.Remove(gameName, out _);
            UpdateRemainingGames();
        });
    }

    private void UpdateRemainingDLCs() => UpdateRemaining(progressLabelDLCs, remainingDLCs, "DLCs");

    private void AddToRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingDLCs[dlcId] = dlcId;
            UpdateRemainingDLCs();
        });
    }

    private void RemoveFromRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            _ = remainingDLCs.Remove(dlcId, out _);
            UpdateRemainingDLCs();
        });
    }

    private async Task GetApplicablePrograms(IProgress<int> progress, bool uninstallAll = false)
    {
        if (!uninstallAll && (programsToScan is null || programsToScan.Count < 1))
            return;
        int totalGameCount = 0;
        int completeGameCount = 0;

        void AddToRemainingGames(string gameName)
        {
            this.AddToRemainingGames(gameName);
            progress.Report(-Interlocked.Increment(ref totalGameCount));
            progress.Report(completeGameCount);
        }

        void RemoveFromRemainingGames(string gameName)
        {
            this.RemoveFromRemainingGames(gameName);
            progress.Report(Interlocked.Increment(ref completeGameCount));
        }

        if (Program.Canceled)
            return;
        remainingGames.Clear(); // for display purposes only, otherwise ignorable
        remainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Paradox))
        {
            AddToRemainingGames("Paradox Launcher");
            HashSet<string> dllDirectories =
                await ParadoxLauncher.InstallPath.GetDllDirectoriesFromGameDirectory(Platform.Paradox);
            if (dllDirectories is not null)
            {
                Selection selection = Selection.GetOrCreate(Platform.Paradox, "PL", "Paradox Launcher",
                    ParadoxLauncher.InstallPath, dllDirectories,
                    await ParadoxLauncher.InstallPath.GetExecutableDirectories(validFunc: path =>
                        !Path.GetFileName(path).Contains("bootstrapper")));
                if (uninstallAll)
                    selection.Enabled = true;
                else if (selection.TreeNode.TreeView is null)
                    _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                RemoveFromRemainingGames("Paradox Launcher");
            }
        }

        int steamGamesToCheck;
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Steam))
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> steamGames =
                await SteamLibrary.GetGames();
            steamGamesToCheck = steamGames.Count;
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in steamGames)
            {
                if (Program.Canceled)
                    return;
                if (!uninstallAll && (Program.IsGameBlocked(name, gameDirectory) ||
                                      !programsToScan.Any(c => c.platform is Platform.Steam && c.id == appId)))
                {
                    _ = Interlocked.Decrement(ref steamGamesToCheck);
                    continue;
                }

                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories =
                        await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Steam);
                    if (dllDirectories is null)
                    {
                        _ = Interlocked.Decrement(ref steamGamesToCheck);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Steam, appId, name, gameDirectory,
                            dllDirectories,
                            await gameDirectory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    StoreAppData storeAppData = await SteamStore.QueryStoreAPI(appId);
                    _ = Interlocked.Decrement(ref steamGamesToCheck);
                    CmdAppData cmdAppData = await SteamCMD.GetAppInfo(appId, branch, buildId);
                    if (storeAppData is null && cmdAppData is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<SelectionDLC, byte> dlc = new();
                    List<Task> dlcTasks = [];
                    HashSet<string> dlcIds = [];
                    if (storeAppData is not null)
                        foreach (string dlcId in await SteamStore.ParseDlcAppIds(storeAppData))
                            _ = dlcIds.Add(dlcId);
                    if (cmdAppData is not null)
                        foreach (string dlcId in await SteamCMD.ParseDlcAppIds(cmdAppData))
                            _ = dlcIds.Add(dlcId);
                    if (dlcIds.Count > 0)
                        foreach (string dlcAppId in dlcIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(dlcAppId);
                            Task task = Task.Run(async () =>
                            {
                                if (Program.Canceled)
                                    return;
                                do // give games steam store api limit priority
                                    Thread.Sleep(200);
                                while (!Program.Canceled && steamGamesToCheck > 0);
                                if (Program.Canceled)
                                    return;
                                string fullGameAppId = null;
                                string dlcName = null;
                                string dlcIcon = null;
                                bool onSteamStore = false;
                                StoreAppData dlcStoreAppData = await SteamStore.QueryStoreAPI(dlcAppId, true);
                                if (dlcStoreAppData is not null)
                                {
                                    dlcName = dlcStoreAppData.Name;
                                    dlcIcon = dlcStoreAppData.HeaderImage;
                                    onSteamStore = true;
                                    fullGameAppId = dlcStoreAppData.FullGame?.AppId;
                                }
                                else
                                {
                                    CmdAppData dlcCmdAppData = await SteamCMD.GetAppInfo(dlcAppId);
                                    if (dlcCmdAppData is not null)
                                    {
                                        dlcName = dlcCmdAppData.Common?.Name;
                                        string dlcIconStaticId = dlcCmdAppData.Common?.Icon;
                                        dlcIconStaticId ??= dlcCmdAppData.Common?.LogoSmall;
                                        dlcIconStaticId ??= dlcCmdAppData.Common?.Logo;
                                        if (dlcIconStaticId is not null)
                                            dlcIcon = IconGrabber.SteamAppImagesPath +
                                                      @$"\{dlcAppId}\{dlcIconStaticId}.jpg";
                                        fullGameAppId = dlcCmdAppData.Common?.Parent;
                                    }
                                }

                                if (fullGameAppId != null && fullGameAppId != appId)
                                {
                                    string fullGameName = null;
                                    string fullGameIcon = null;
                                    bool fullGameOnSteamStore = false;
                                    StoreAppData fullGameStoreAppData =
                                        await SteamStore.QueryStoreAPI(fullGameAppId, true);
                                    if (fullGameStoreAppData is not null)
                                    {
                                        fullGameName = fullGameStoreAppData.Name;
                                        fullGameIcon = fullGameStoreAppData.HeaderImage;
                                        fullGameOnSteamStore = true;
                                    }
                                    else
                                    {
                                        CmdAppData fullGameAppInfo = await SteamCMD.GetAppInfo(fullGameAppId);
                                        if (fullGameAppInfo is not null)
                                        {
                                            fullGameName = fullGameAppInfo.Common?.Name;
                                            string fullGameIconStaticId = fullGameAppInfo.Common?.Icon;
                                            fullGameIconStaticId ??= fullGameAppInfo.Common?.LogoSmall;
                                            fullGameIconStaticId ??= fullGameAppInfo.Common?.Logo;
                                            if (fullGameIconStaticId is not null)
                                                dlcIcon = IconGrabber.SteamAppImagesPath +
                                                          @$"\{fullGameAppId}\{fullGameIconStaticId}.jpg";
                                        }
                                    }

                                    if (Program.Canceled)
                                        return;
                                    if (!string.IsNullOrWhiteSpace(fullGameName))
                                    {
                                        SelectionDLC fullGameDlc = SelectionDLC.GetOrCreate(
                                            fullGameOnSteamStore ? DLCType.Steam : DLCType.SteamHidden, appId,
                                            fullGameAppId, fullGameName);
                                        fullGameDlc.Icon = fullGameIcon;
                                        _ = dlc.TryAdd(fullGameDlc, default);
                                    }
                                }

                                if (Program.Canceled)
                                    return;
                                if (string.IsNullOrWhiteSpace(dlcName))
                                    dlcName = "Unknown";
                                SelectionDLC _dlc = SelectionDLC.GetOrCreate(
                                    onSteamStore ? DLCType.Steam : DLCType.SteamHidden, appId, dlcAppId, dlcName);
                                _dlc.Icon = dlcIcon;
                                _ = dlc.TryAdd(_dlc, default);
                                RemoveFromRemainingDLCs(dlcAppId);
                            });
                            dlcTasks.Add(task);
                        }
                    else
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled)
                            return;
                        await task;
                    }

                    steamGamesToCheck = 0;
                    if (dlc.IsEmpty)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    Selection selection = Selection.GetOrCreate(Platform.Steam, appId, storeAppData?.Name ?? name,
                        gameDirectory, dllDirectories,
                        await gameDirectory.GetExecutableDirectories(true));
                    selection.Product = "https://store.steampowered.com/app/" + appId;
                    selection.Icon = IconGrabber.SteamAppImagesPath + @$"\{appId}\{cmdAppData?.Common?.Icon}.jpg";
                    selection.SubIcon = storeAppData?.HeaderImage ?? IconGrabber.SteamAppImagesPath
                        + @$"\{appId}\{cmdAppData?.Common?.ClientIcon}.ico";
                    selection.Publisher = storeAppData?.Publishers[0] ?? cmdAppData?.Extended?.Publisher;
                    selection.Website = storeAppData?.Website;
                    if (Program.Canceled)
                        return;
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                        foreach ((SelectionDLC dlc, _) in dlc)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                        }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Epic))
        {
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                if (Program.Canceled)
                    return;
                if (!uninstallAll && (Program.IsGameBlocked(name, directory) ||
                                      !programsToScan.Any(c => c.platform is Platform.Epic && c.id == @namespace)))
                    continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories = await directory.GetDllDirectoriesFromGameDirectory(Platform.Epic);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Epic, @namespace, name, directory,
                            dllDirectories,
                            await directory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    List<Task> dlcTasks = new();
                    ConcurrentDictionary<SelectionDLC, byte> catalogItems = new();
                    List<(string id, string name, string product, string icon, string developer)> catalogIds =
                        await EpicStore.QueryCatalog(@namespace);
                    if (catalogIds.Count > 0)
                        foreach ((string id, string name, string product, string icon, string developer) in catalogIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled)
                                    return;
                                SelectionDLC catalogItem = SelectionDLC.GetOrCreate(DLCType.Epic, @namespace, id, name);
                                catalogItem.Icon = icon;
                                catalogItem.Product = product;
                                catalogItem.Publisher = developer;
                                _ = catalogItems.TryAdd(catalogItem, default);
                                RemoveFromRemainingDLCs(id);
                            });
                            dlcTasks.Add(task);
                        }

                    if (Program.Canceled)
                        return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled)
                            return;
                        await task;
                    }

                    if (catalogItems.IsEmpty)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    Selection selection = Selection.GetOrCreate(Platform.Epic, @namespace, name, directory,
                        dllDirectories,
                        await directory.GetExecutableDirectories(true));
                    foreach ((SelectionDLC dlc, _) in catalogItems.Where(dlc => dlc.Key.Name == selection.Name))
                    {
                        if (Program.Canceled)
                            return;
                        selection.Product = "https://www.epicgames.com/store/product/" + dlc.Product;
                        selection.Icon = dlc.Icon;
                        selection.Publisher = dlc.Publisher;
                    }

                    if (Program.Canceled)
                        return;
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                        if (catalogItems.IsEmpty)
                            return;
                        foreach ((SelectionDLC dlc, _) in catalogItems)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                        }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Ubisoft))
        {
            List<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
            foreach ((string gameId, string name, string gameDirectory) in ubisoftGames)
            {
                if (Program.Canceled)
                    return;
                if (!uninstallAll && (Program.IsGameBlocked(name, gameDirectory) ||
                                      !programsToScan.Any(c => c.platform is Platform.Ubisoft && c.id == gameId)))
                    continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories =
                        await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Ubisoft);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Ubisoft, gameId, name, gameDirectory,
                            dllDirectories,
                            await gameDirectory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    Selection selection = Selection.GetOrCreate(Platform.Ubisoft, gameId, name, gameDirectory,
                        dllDirectories,
                        await gameDirectory.GetExecutableDirectories(true));
                    selection.Icon = IconGrabber.GetDomainFaviconUrl("store.ubi.com");
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }

        foreach (Task task in appTasks)
        {
            if (Program.Canceled)
                return;
            await task;
        }

        steamGamesToCheck = 0;
    }

    private async void OnLoad(bool forceScan = false, bool forceProvideChoices = false)
    {
        Program.Canceled = false;
        blockedGamesCheckBox.Enabled = false;
        blockProtectedHelpButton.Enabled = false;
        cancelButton.Enabled = true;
        scanButton.Enabled = false;
        noneFoundLabel.Visible = false;
        allCheckBox.Enabled = false;
        proxyAllCheckBox.Enabled = false;
        installButton.Enabled = false;
        uninstallButton.Enabled = installButton.Enabled;
        selectionTreeView.Enabled = false;
        saveButton.Enabled = false;
        loadButton.Enabled = false;
        resetButton.Enabled = false;
        progressLabel.Text = "Waiting for user to select which programs/games to scan . . .";
        ShowProgressBar();
        await ProgramData.Setup(this);
        bool scan = forceScan;
        if (!scan && (programsToScan is null || programsToScan.Count < 1 || forceProvideChoices))
        {
            List<(Platform platform, string id, string name, bool alreadySelected)> gameChoices = new();
            if (ParadoxLauncher.InstallPath.DirectoryExists())
                gameChoices.Add((Platform.Paradox, "PL", "Paradox Launcher",
                    programsToScan is not null &&
                    programsToScan.Any(p => p.platform is Platform.Paradox && p.id == "PL")));
            if (SteamLibrary.InstallPath.DirectoryExists())
                foreach ((string appId, string name, string _, int _, string _) in
                         (await SteamLibrary.GetGames()).Where(g
                             => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                    gameChoices.Add((Platform.Steam, appId, name,
                        programsToScan is not null &&
                        programsToScan.Any(p => p.platform is Platform.Steam && p.id == appId)));
            if (EpicLibrary.EpicManifestsPath.DirectoryExists() || HeroicLibrary.HeroicLibraryPath.FileExists())
                gameChoices.AddRange((await EpicLibrary.GetGames())
                    .Where(m => !Program.IsGameBlocked(m.DisplayName, m.InstallLocation)).Select(manifest
                        => (Platform.Epic, manifest.CatalogNamespace, manifest.DisplayName,
                            programsToScan is not null && programsToScan.Any(p =>
                                p.platform is Platform.Epic && p.id == manifest.CatalogNamespace))));
            foreach ((string gameId, string name, string _) in (await UbisoftLibrary.GetGames()).Where(g =>
                         !Program.IsGameBlocked(g.name, g.gameDirectory)))
                gameChoices.Add((Platform.Ubisoft, gameId, name,
                    programsToScan is not null &&
                    programsToScan.Any(p => p.platform is Platform.Ubisoft && p.id == gameId)));
            if (gameChoices.Count > 0)
            {
                using SelectDialogForm form = new(this);
                DialogResult selectResult = form.QueryUser("Choose which programs and/or games to scan:", gameChoices,
                    out List<(Platform platform, string id, string name)> choices);
                if (selectResult == DialogResult.Abort)
                {
                    int maxProgress = 0;
                    int curProgress = 0;
                    Progress<int> progress = new();
                    IProgress<int> iProgress = progress;
                    progress.ProgressChanged += (_, _progress) =>
                    {
                        if (Program.Canceled)
                            return;
                        if (_progress < 0 || _progress > maxProgress)
                            maxProgress = -_progress;
                        else
                            curProgress = _progress;
                        int p = Math.Max(Math.Min((int)((float)curProgress / maxProgress * 100), 100), 0);
                        progressLabel.Text = $"Quickly gathering games for uninstallation . . . {p}%";
                        progressBar.Value = p;
                    };
                    progressLabel.Text = "Quickly gathering games for uninstallation . . . ";
                    foreach (Selection selection in Selection.All.Keys)
                        selection.TreeNode.Remove();
                    await GetApplicablePrograms(iProgress, true);
                    if (!Program.Canceled)
                        OnUninstall(null, null);
                    Selection.All.Clear();
                    programsToScan = null;
                }
                else
                    scan = selectResult == DialogResult.OK && choices is not null && choices.Count > 0;

                const string retry = "\n\nPress the \"Rescan\" button to re-choose.";
                if (scan)
                {
                    programsToScan = choices;
                    noneFoundLabel.Text = "None of the chosen programs nor games were applicable!" + retry;
                }
                else
                    noneFoundLabel.Text = "You didn't choose any programs nor games!" + retry;
            }
            else
                noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
        }

        if (scan)
        {
            bool setup = true;
            int maxProgress = 0;
            int curProgress = 0;
            Progress<int> progress = new();
            IProgress<int> iProgress = progress;
            progress.ProgressChanged += (_, _progress) =>
            {
                if (Program.Canceled)
                    return;
                if (_progress < 0 || _progress > maxProgress)
                    maxProgress = -_progress;
                else
                    curProgress = _progress;
                int p = Math.Max(Math.Min((int)((float)curProgress / maxProgress * 100), 100), 0);
                progressLabel.Text =
                    setup
                        ? $"Setting up SteamCMD . . . {p}%"
                        : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };
            if (SteamLibrary.InstallPath.DirectoryExists() && programsToScan is not null &&
                programsToScan.Any(c => c.platform is Platform.Steam))
            {
                progressLabel.Text = "Setting up SteamCMD . . . ";
                if (!await SteamCMD.Setup(iProgress))
                {
                    HideProgressBar();
                    OnLoad(forceScan, true);
                    return;
                }
            }

            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            Selection.ValidateAll(programsToScan);
            foreach (Selection selection in Selection.All.Keys)
                selection.TreeNode.Remove();
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();
        }

        OnLoadSelections(null, null);
        HideProgressBar();
        selectionTreeView.Enabled = !Selection.All.IsEmpty;
        allCheckBox.Enabled = selectionTreeView.Enabled;
        proxyAllCheckBox.Enabled = selectionTreeView.Enabled;
        noneFoundLabel.Visible = !selectionTreeView.Enabled;
        installButton.Enabled = Selection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        saveButton.Enabled = CanSaveSelections();
        loadButton.Enabled = CanLoadSelections();
        resetButton.Enabled = CanResetSelections();
        cancelButton.Enabled = false;
        scanButton.Enabled = true;
        blockedGamesCheckBox.Enabled = true;
        blockProtectedHelpButton.Enabled = true;
    }

    private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown)
            return;
        TreeNode node = e.Node;
        if (node is null)
            return;
        SyncNodeAncestors(node);
        SyncNodeDescendants(node);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = EnumerateTreeNodes(selectionTreeView.Nodes)
            .All(node => node.Text == "Unknown" || node.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        installButton.Enabled = Selection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        if (sender is "OnLoadSelections" or "OnResetSelections")
            return;
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
    }

    private static void SyncNodeAncestors(TreeNode node)
    {
        TreeNode parentNode = node.Parent;
        if (parentNode is null)
            return;
        parentNode.Checked = parentNode.Nodes.Cast<TreeNode>().Any(childNode => childNode.Checked);
        SyncNodeAncestors(parentNode);
    }

    private static void SyncNodeDescendants(TreeNode node)
    {
        foreach (TreeNode childNode in node.Nodes)
        {
            if (childNode.Text == "Unknown")
                continue;
            childNode.Checked = node.Checked;
            SyncNodeDescendants(childNode);
        }
    }

    private static IEnumerable<TreeNode> EnumerateTreeNodes(TreeNodeCollection nodeCollection)
    {
        foreach (TreeNode rootNode in nodeCollection)
        {
            yield return rootNode;
            foreach (TreeNode childNode in EnumerateTreeNodes(rootNode.Nodes))
                yield return childNode;
        }
    }

    private void ShowProgressBar()
    {
        progressBar.Value = 0;
        progressLabelGames.Text = "Loading . . . ";
        progressLabel.Visible = true;
        progressLabelGames.Text = "";
        progressLabelGames.Visible = true;
        progressLabelDLCs.Text = "";
        progressLabelDLCs.Visible = true;
        progressBar.Visible = true;
        programsGroupBox.Size = programsGroupBox.Size with
        {
            Height = programsGroupBox.Size.Height - progressLabel.Size.Height - progressLabelGames.Size.Height -
                     progressLabelDLCs.Size.Height - progressBar.Size.Height - 6
        };
    }

    private void HideProgressBar()
    {
        progressBar.Value = 100;
        progressLabel.Visible = false;
        progressLabelGames.Visible = false;
        progressLabelDLCs.Visible = false;
        progressBar.Visible = false;
        programsGroupBox.Size = programsGroupBox.Size with
        {
            Height = programsGroupBox.Size.Height + progressLabel.Size.Height + progressLabelGames.Size.Height +
                     progressLabelDLCs.Size.Height + progressBar.Size.Height + 6
        };
    }

    internal void OnNodeRightClick(TreeNode node, Point location)
        => Invoke(() =>
        {
            ContextMenuStrip contextMenuStrip = new();
            ToolStripItemCollection items = contextMenuStrip.Items;
            string id = node.Name;
            Platform platform = (Platform)node.Tag;
            Selection selection = Selection.FromId(platform, id);
            SelectionDLC dlc = null;
            if (selection is null)
                dlc = SelectionDLC.FromId((DLCType)node.Tag, node.Parent?.Name, id);
            Selection dlcParentSelection = null;
            if (dlc is not null)
                dlcParentSelection = dlc.Selection;
            if (selection is null && dlcParentSelection is null)
                return;
            ContextMenuItem header = id == "PL"
                ? new(node.Text, "Paradox Launcher")
                : selection is not null
                    ? new(node.Text, (id, selection.Icon))
                    : new(node.Text, (id, dlc.Icon), (id, dlcParentSelection.Icon));
            _ = items.Add(header);
            string appInfoVDF = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
            string appInfoCmdJSON = $@"{SteamCMD.AppInfoPath}\{id}.cmd.json";
            string appInfoJSON = $@"{SteamCMD.AppInfoPath}\{id}.json";
            string cooldown = $@"{ProgramData.CooldownPath}\{id}.txt";
            if (appInfoVDF.FileExists() || appInfoCmdJSON.FileExists() || appInfoJSON.FileExists())
            {
                List<ContextMenuItem> queries = [];
                if (appInfoJSON.FileExists())
                {
                    string platformString = selection is null || selection.Platform is Platform.Steam
                        ? "Steam Store "
                        : selection.Platform is Platform.Epic
                            ? "Epic GraphQL "
                            : "";
                    queries.Add(new($"Open {platformString}Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoJSON)));
                }

                if (appInfoCmdJSON.FileExists())
                    queries.Add(new("Open SteamCMD.net Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoCmdJSON)));

                if (appInfoVDF.FileExists())
                    queries.Add(new("Open SteamCMD Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoVDF)));

                if (queries.Count > 0)
                {
                    _ = items.Add(new ToolStripSeparator());
                    foreach (ContextMenuItem query in queries)
                        _ = items.Add(query);
                    _ = items.Add(new ContextMenuItem("Refresh Queries", "Command Prompt", (_, _) =>
                    {
                        appInfoVDF.DeleteFile();
                        appInfoCmdJSON.DeleteFile();
                        appInfoJSON.DeleteFile();
                        cooldown.DeleteFile();
                        selection?.Remove();
                        if (dlc is not null)
                            dlc.Selection = null;
                        OnLoad(true);
                    }));
                }
            }

            if (selection is not null)
            {
                if (id == "PL")
                {
                    _ = items.Add(new ToolStripSeparator());

                    async void EventHandler(object sender, EventArgs e)
                    {
                        _ = await ParadoxLauncher.Repair(this, selection);
                        Program.Canceled = false;
                    }

                    _ = items.Add(new ContextMenuItem("Repair", "Command Prompt", EventHandler));
                }

                _ = items.Add(new ToolStripSeparator());
                _ = items.Add(new ContextMenuItem("Open Root Directory", "File Explorer",
                    (_, _) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory)));
                int executables = 0;
                foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories)
                    _ = items.Add(new ContextMenuItem(
                        $"Open Executable Directory #{++executables} ({(binaryType == BinaryType.BIT32 ? "32" : "64")}-bit)",
                        "File Explorer", (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                HashSet<string> directories = selection.DllDirectories;
                int steam = 0, epic = 0, r1 = 0, r2 = 0;
                if (selection.Platform is Platform.Steam or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string old_config,
                            out string config, out string old_log, out string log, out string cache);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            old_config.FileExists()
                            || config.FileExists() || old_log.FileExists() || log.FileExists() || cache.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Steamworks Directory #{++steam}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }

                if (selection.Platform is Platform.Epic or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string config,
                            out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open EOS Directory #{++epic}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }

                if (selection.Platform is Platform.Ubisoft)
                    foreach (string directory in directories)
                    {
                        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string config,
                            out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Uplay R1 Directory #{++r1}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                        directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32,
                            out api32_o, out api64, out api64_o, out config,
                            out log);
                        if (old_api32.FileExists() || old_api64.FileExists() || api32.FileExists() ||
                            api32_o.FileExists() || api64.FileExists()
                            || api64_o.FileExists() || config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Uplay R2 Directory #{++r2}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
            }

            if (id != "PL")
            {
                if (selection?.Platform is Platform.Steam || dlcParentSelection?.Platform is Platform.Steam)
                {
                    _ = items.Add(new ToolStripSeparator());
                    _ = items.Add(new ContextMenuItem("Open SteamDB", "SteamDB",
                        (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamdb.info/app/" + id)));
                }

                if (selection is not null)
                    switch (selection.Platform)
                    {
                        case Platform.Steam:
                            _ = items.Add(new ContextMenuItem("Open Steam Store", "Steam Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Product)));
                            _ = items.Add(new ContextMenuItem("Open Steam Community", ("Sub_" + id, selection.SubIcon),
                                "Steam Community",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" +
                                                                               id)));
                            break;
                        case Platform.Epic:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open ScreamDB", "ScreamDB",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" +
                                                                               id)));
                            _ = items.Add(new ContextMenuItem("Open Epic Games Store", "Epic Games",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Product)));
                            break;
                        case Platform.Ubisoft:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open Ubisoft Store", "Ubisoft Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(
                                    "https://store.ubi.com/us/" +
                                    selection.Name.Replace(" ", "-").ToLowerInvariant())));
                            break;
                    }
            }

            if (selection?.Website is not null)
                _ = items.Add(new ContextMenuItem("Open Official Website",
                    ("Web_" + id, IconGrabber.GetDomainFaviconUrl(selection.Website)),
                    (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Website)));
            contextMenuStrip.Show(selectionTreeView, location);
            contextMenuStrip.Refresh();
        });

    private void OnLoad(object sender, EventArgs _)
    {
        retry:
        try
        {
            HideProgressBar();
            selectionTreeView.AfterCheck += OnTreeViewNodeCheckedChanged;
            OnLoad(forceProvideChoices: true);
        }
        catch (Exception e)
        {
            if (e.HandleException(this))
                goto retry;
            Close();
        }
    }

    private void OnAccept(bool uninstall = false)
    {
        if (Selection.All.IsEmpty || !uninstall && ParadoxLauncher.DlcDialog(this))
            return;
        Hide();
        InstallForm form = new(uninstall);
        form.InheritLocation(this);
        form.FormClosing += (_, _) =>
        {
            if (form.Reselecting)
            {
                InheritLocation(form);
                Show();
#if DEBUG
                DebugForm.Current.Attach(this);
#endif
                OnLoad();
            }
            else
                Close();
        };
        form.Show();
        Hide();
#if DEBUG
        DebugForm.Current.Attach(form);
#endif
    }

    private void OnInstall(object sender, EventArgs e) => OnAccept();

    private void OnUninstall(object sender, EventArgs e) => OnAccept(true);

    private void OnScan(object sender, EventArgs e) => OnLoad(forceProvideChoices: true);

    private void OnCancel(object sender, EventArgs e)
    {
        progressLabel.Text = "Cancelling . . . ";
        Program.Cleanup();
    }

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldEnable = Selection.All.Keys.Any(s => !s.Enabled);
        foreach (Selection selection in Selection.All.Keys.Where(s => s.Enabled != shouldEnable))
        {
            selection.Enabled = shouldEnable;
            OnTreeViewNodeCheckedChanged("OnAllCheckBoxChanged", new(selection.TreeNode, TreeViewAction.ByMouse));
        }

        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = shouldEnable;
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    private void OnProxyAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldEnable = Selection.All.Keys.Any(selection => !selection.UseProxy);
        foreach (Selection selection in Selection.All.Keys)
            selection.UseProxy = shouldEnable;
        selectionTreeView.Invalidate();
        proxyAllCheckBox.CheckedChanged -= OnProxyAllCheckBoxChanged;
        proxyAllCheckBox.Checked = shouldEnable;
        proxyAllCheckBox.CheckedChanged += OnProxyAllCheckBoxChanged;
        resetButton.Enabled = CanResetSelections();
        saveButton.Enabled = CanSaveSelections();
    }

    private bool AreSelectionsDefault()
        => EnumerateTreeNodes(selectionTreeView.Nodes).All(node
            => node.Parent is null || node.Tag is not Platform and not DLCType ||
               (node.Text == "Unknown" ? !node.Checked : node.Checked));

    private static bool AreProxySelectionsDefault() => Selection.All.Keys.All(selection => !selection.UseProxy);

    private bool CanSaveDlc() =>
        installButton.Enabled && (ProgramData.ReadDlcChoices().Any() || !AreSelectionsDefault());

    private static bool CanSaveProxy() =>
        ProgramData.ReadProxyChoices().Any() || !AreProxySelectionsDefault();

    private bool CanSaveSelections() => CanSaveDlc() || CanSaveProxy();

    private void OnSaveSelections(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            _ = dlcChoices.RemoveAll(n =>
                n.platform == dlc.Selection.Platform && n.gameId == dlc.Selection.Id && n.dlcId == dlc.Id);
            if (dlc.Name == "Unknown" ? dlc.Enabled : !dlc.Enabled)
                dlcChoices.Add((dlc.Selection.Platform, dlc.Selection.Id, dlc.Id));
        }

        ProgramData.WriteDlcChoices(dlcChoices);

        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
        {
            _ = proxyChoices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.UseProxy)
                proxyChoices.Add((selection.Platform, selection.Id,
                    selection.Proxy == Selection.DefaultProxy ? null : selection.Proxy,
                    selection.UseProxy));
        }

        ProgramData.WriteProxyChoices(proxyChoices);

        loadButton.Enabled = CanLoadSelections();
        saveButton.Enabled = CanSaveSelections();
    }

    private static bool CanLoadDlc() => ProgramData.ReadDlcChoices().Any();

    private static bool CanLoadProxy() => ProgramData.ReadProxyChoices().Any();

    private bool CanLoadSelections() => CanLoadDlc() || CanLoadProxy();

    private void OnLoadSelections(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlcChoices.Any(c =>
                c.platform == dlc.Selection?.Platform && c.gameId == dlc.Selection?.Id && c.dlcId == dlc.Id)
                ? dlc.Name == "Unknown"
                : dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnLoadSelections", new(dlc.TreeNode, TreeViewAction.ByMouse));
        }

        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
            if (proxyChoices.Any(c => c.platform == selection.Platform && c.id == selection.Id))
            {
                (Platform platform, string id, string proxy, bool enabled)
                    choice = proxyChoices.First(c => c.platform == selection.Platform && c.id == selection.Id);
                (Platform platform, string id, string proxy, bool enabled) = choice;
                string currentProxy = proxy;
                if (proxy is not null && proxy.Contains('.')) // convert pre-v4.1.0.0 choices
                    proxy.GetProxyInfoFromIdentifier(out currentProxy, out _);
                if (proxy != currentProxy && proxyChoices.Remove(choice)) // convert pre-v4.1.0.0 choices
                    proxyChoices.Add((platform, id, currentProxy, enabled));
                if (currentProxy is null or Selection.DefaultProxy && !enabled)
                    _ = proxyChoices.RemoveAll(c => c.platform == platform && c.id == id);
                else
                {
                    selection.UseProxy = enabled;
                    selection.Proxy = currentProxy == Selection.DefaultProxy ? currentProxy : proxy;
                }
            }
            else
            {
                selection.UseProxy = false;
                selection.Proxy = null;
            }

        ProgramData.WriteProxyChoices(proxyChoices);
        loadButton.Enabled = CanLoadSelections();

        OnProxyChanged();
    }

    private bool CanResetDlc() => !AreSelectionsDefault();

    private static bool CanResetProxy() => !AreProxySelectionsDefault();

    private bool CanResetSelections() => CanResetDlc() || CanResetProxy();

    private void OnResetSelections(object sender, EventArgs e)
    {
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnResetSelections", new(dlc.TreeNode, TreeViewAction.ByMouse));
        }

        foreach (Selection selection in Selection.All.Keys)
        {
            selection.UseProxy = false;
            selection.Proxy = null;
        }

        OnProxyChanged();
    }

    internal void OnProxyChanged()
    {
        selectionTreeView.Invalidate();
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
        proxyAllCheckBox.CheckedChanged -= OnProxyAllCheckBoxChanged;
        proxyAllCheckBox.Checked = Selection.All.Keys.All(selection => selection.UseProxy);
        proxyAllCheckBox.CheckedChanged += OnProxyAllCheckBoxChanged;
    }

    private void OnBlockProtectedGamesCheckBoxChanged(object sender, EventArgs e)
    {
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        OnLoad(forceProvideChoices: true);
    }

    private void OnBlockProtectedGamesHelpButtonClicked(object sender, EventArgs e)
    {
        StringBuilder blockedGames = new();
        foreach (string name in Program.ProtectedGames)
            _ = blockedGames.Append(HelpButtonListPrefix + name);
        StringBuilder blockedDirectories = new();
        foreach (string path in Program.ProtectedGameDirectories)
            _ = blockedDirectories.Append(HelpButtonListPrefix + path);
        StringBuilder blockedDirectoryExceptions = new();
        foreach (string name in Program.ProtectedGameDirectoryExceptions)
            _ = blockedDirectoryExceptions.Append(HelpButtonListPrefix + name);
        using DialogForm form = new(this);
        _ = form.Show(SystemIcons.Information,
            "Blocks the program from caching and displaying games protected by anti-cheats."
            + "\nYou disable this option and install DLC unlockers to protected games at your own risk!" +
            "\n\nBlocked games: "
            + (string.IsNullOrWhiteSpace(blockedGames.ToString()) ? "(none)" : blockedGames) +
            "\n\nBlocked game sub-directories: "
            + (string.IsNullOrWhiteSpace(blockedDirectories.ToString()) ? "(none)" : blockedDirectories) +
            "\n\nBlocked game sub-directory exceptions: "
            + (string.IsNullOrWhiteSpace(blockedDirectoryExceptions.ToString())
                ? "(none)"
                : blockedDirectoryExceptions),
            customFormText: "Block Protected Games");
    }

    private void OnSortCheckBoxChanged(object sender, EventArgs e)
        => selectionTreeView.TreeViewNodeSorter =
            sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;
}