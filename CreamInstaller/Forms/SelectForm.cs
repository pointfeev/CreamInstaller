using System;
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
using CreamInstaller.Platforms.Paradox;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Platforms.Ubisoft;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using Gameloop.Vdf.Linq;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Forms;

internal sealed partial class SelectForm : CustomForm
{
    private const string HelpButtonListPrefix = "\n    •  ";

    private readonly SynchronizedCollection<string> remainingDLCs = new();

    private readonly SynchronizedCollection<string> remainingGames = new();

    private List<(Platform platform, string id, string name)> programsToScan;

    internal SelectForm()
    {
        InitializeComponent();
        Text = Program.ApplicationName;
    }

    public override ContextMenuStrip ContextMenuStrip => base.ContextMenuStrip ??= new();

    private List<TreeNode> TreeNodes => GatherTreeNodes(selectionTreeView.Nodes);

    private static void UpdateRemaining(Label label, SynchronizedCollection<string> list, string descriptor)
        => label.Text = list.Any() ? $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list).Replace("&", "&&") : "";

    private void UpdateRemainingGames() => UpdateRemaining(progressLabelGames, remainingGames, "games");

    private void AddToRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        progressLabelGames.Invoke(delegate
        {
            if (Program.Canceled)
                return;
            if (!remainingGames.Contains(gameName))
                remainingGames.Add(gameName);
            UpdateRemainingGames();
        });
    }

    private void RemoveFromRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        progressLabelGames.Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingGames.Remove(gameName);
            UpdateRemainingGames();
        });
    }

    private void UpdateRemainingDLCs() => UpdateRemaining(progressLabelDLCs, remainingDLCs, "DLCs");

    private void AddToRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        progressLabelDLCs.Invoke(delegate
        {
            if (Program.Canceled)
                return;
            if (!remainingDLCs.Contains(dlcId))
                remainingDLCs.Add(dlcId);
            UpdateRemainingDLCs();
        });
    }

    private void RemoveFromRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        progressLabelDLCs.Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingDLCs.Remove(dlcId);
            UpdateRemainingDLCs();
        });
    }

    private async Task GetApplicablePrograms(IProgress<int> progress)
    {
        if (programsToScan is null || !programsToScan.Any())
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
        List<TreeNode> treeNodes = TreeNodes;
        remainingGames.Clear(); // for display purposes only, otherwise ignorable
        remainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        if (programsToScan.Any(c => c.platform is Platform.Paradox))
        {
            List<string> dllDirectories = await ParadoxLauncher.InstallPath.GetDllDirectoriesFromGameDirectory(Platform.Paradox);
            if (dllDirectories is not null)
            {
                ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Paradox, "PL");
                selection ??= new();
                if (allCheckBox.Checked)
                    selection.Enabled = true;
                if (koaloaderAllCheckBox.Checked)
                    selection.Koaloader = true;
                selection.Id = "PL";
                selection.Name = "Paradox Launcher";
                selection.RootDirectory = ParadoxLauncher.InstallPath;
                selection.ExecutableDirectories = await ParadoxLauncher.GetExecutableDirectories(selection.RootDirectory);
                selection.DllDirectories = dllDirectories;
                selection.Platform = Platform.Paradox;
                TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Paradox && s.Name == selection.Id) ?? new TreeNode();
                programNode.Tag = selection.Platform;
                programNode.Name = selection.Id;
                programNode.Text = selection.Name;
                programNode.Checked = selection.Enabled;
                if (programNode.TreeView is null)
                    _ = selectionTreeView.Nodes.Add(programNode);
            }
        }
        int steamGamesToCheck;
        if (programsToScan.Any(c => c.platform is Platform.Steam))
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> steamGames = await SteamLibrary.GetGames();
            steamGamesToCheck = steamGames.Count;
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in steamGames)
            {
                if (Program.Canceled)
                    return;
                if (Program.IsGameBlocked(name, gameDirectory) || !programsToScan.Any(c => c.platform is Platform.Steam && c.id == appId))
                {
                    Interlocked.Decrement(ref steamGamesToCheck);
                    continue;
                }
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    List<string> dllDirectories = await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Steam);
                    if (dllDirectories is null)
                    {
                        Interlocked.Decrement(ref steamGamesToCheck);
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    AppData appData = await SteamStore.QueryStoreAPI(appId);
                    Interlocked.Decrement(ref steamGamesToCheck);
                    VProperty appInfo = await SteamCMD.GetAppInfo(appId, branch, buildId);
                    if (appData is null && appInfo is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<string, (DlcType type, string name, string icon)> dlc = new();
                    List<Task> dlcTasks = new();
                    List<string> dlcIds = new();
                    if (appData is not null)
                        dlcIds.AddRange(await SteamStore.ParseDlcAppIds(appData));
                    if (appInfo is not null)
                        dlcIds.AddRange(await SteamCMD.ParseDlcAppIds(appInfo));
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
                                string dlcName = null;
                                string dlcIcon = null;
                                bool onSteamStore = false;
                                AppData dlcAppData = await SteamStore.QueryStoreAPI(dlcAppId, true);
                                if (dlcAppData is not null)
                                {
                                    dlcName = dlcAppData.name;
                                    dlcIcon = dlcAppData.header_image;
                                    onSteamStore = true;
                                }
                                else
                                {
                                    VProperty dlcAppInfo = await SteamCMD.GetAppInfo(dlcAppId);
                                    if (dlcAppInfo is not null)
                                    {
                                        dlcName = dlcAppInfo.Value.GetChild("common")?.GetChild("name")?.ToString();
                                        string dlcIconStaticId = dlcAppInfo.Value.GetChild("common")?.GetChild("icon")?.ToString();
                                        dlcIconStaticId ??= dlcAppInfo.Value.GetChild("common")?.GetChild("logo_small")?.ToString();
                                        dlcIconStaticId ??= dlcAppInfo.Value.GetChild("common")?.GetChild("logo")?.ToString();
                                        if (dlcIconStaticId is not null)
                                            dlcIcon = IconGrabber.SteamAppImagesPath + @$"\{dlcAppId}\{dlcIconStaticId}.jpg";
                                    }
                                }
                                if (Program.Canceled)
                                    return;
                                if (string.IsNullOrWhiteSpace(dlcName))
                                    dlcName = "Unknown";
                                dlc[dlcAppId] = (onSteamStore ? DlcType.Steam : DlcType.SteamHidden, dlcName, dlcIcon);
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
                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Steam, appId) ?? new ProgramSelection();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = appId;
                    selection.Name = appData?.name ?? name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await SteamLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Steam;
                    selection.ProductUrl = "https://store.steampowered.com/app/" + appId;
                    selection.IconUrl = IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value.GetChild("common")?.GetChild("icon")}.jpg";
                    selection.SubIconUrl = appData?.header_image ?? IconGrabber.SteamAppImagesPath
                      + @$"\{appId}\{appInfo?.Value.GetChild("common")?.GetChild("clienticon")}.ico";
                    selection.Publisher = appData?.publishers[0] ?? appInfo?.Value.GetChild("extended")?.GetChild("publisher")?.ToString();
                    selection.WebsiteUrl = appData?.website;
                    if (Program.Canceled)
                        return;
                    selectionTreeView.Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == appId) ?? new TreeNode();
                        programNode.Tag = selection.Platform;
                        programNode.Name = appId;
                        programNode.Text = appData?.name ?? name;
                        programNode.Checked = selection.Enabled;
                        if (programNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(programNode);
                        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in dlc)
                        {
                            if (Program.Canceled)
                                return;
                            string appId = pair.Key;
                            (DlcType type, string name, string icon) dlcApp = pair.Value;
                            selection.AllDlc[appId] = dlcApp;
                            if (allCheckBox.Checked && dlcApp.name != "Unknown")
                                selection.SelectedDlc[appId] = dlcApp;
                            TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == appId) ?? new TreeNode();
                            dlcNode.Tag = selection.Platform;
                            dlcNode.Name = appId;
                            dlcNode.Text = dlcApp.name;
                            dlcNode.Checked = selection.SelectedDlc.ContainsKey(appId);
                            if (dlcNode.Parent is null)
                                _ = programNode.Nodes.Add(dlcNode);
                        }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }
        if (programsToScan.Any(c => c.platform is Platform.Epic))
        {
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                if (Program.Canceled)
                    return;
                if (Program.IsGameBlocked(name, directory) || !programsToScan.Any(c => c.platform is Platform.Epic && c.id == @namespace))
                    continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    List<string> dllDirectories = await directory.GetDllDirectoriesFromGameDirectory(Platform.Epic);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<string, (string name, string product, string icon, string developer)> entitlements = new();
                    List<Task> dlcTasks = new();
                    List<(string id, string name, string product, string icon, string developer)> entitlementIds
                        = await EpicStore.QueryEntitlements(@namespace);
                    if (entitlementIds.Any())
                        foreach ((string id, string name, string product, string icon, string developer) in entitlementIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled)
                                    return;
                                entitlements[id] = (name, product, icon, developer);
                                RemoveFromRemainingDLCs(id);
                            });
                            dlcTasks.Add(task);
                        }
                    if ( /*!catalogItems.Any() && */!entitlements.Any())
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
                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Epic, @namespace) ?? new ProgramSelection();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = @namespace;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.ExecutableDirectories = await EpicLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Epic;
                    foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements.Where(p
                                 => p.Value.name == selection.Name))
                    {
                        selection.ProductUrl = "https://www.epicgames.com/store/product/" + pair.Value.product;
                        selection.IconUrl = pair.Value.icon;
                        selection.Publisher = pair.Value.developer;
                    }
                    if (Program.Canceled)
                        return;
                    selectionTreeView.Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace) ?? new TreeNode();
                        programNode.Tag = selection.Platform;
                        programNode.Name = @namespace;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        if (programNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(programNode);
                        /*TreeNode catalogItemsNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace + "_catalogItems") ?? new();
                        catalogItemsNode.Tag = selection.Platform;
                        catalogItemsNode.Name = @namespace + "_catalogItems";
                        catalogItemsNode.Text = "Catalog Items";
                        catalogItemsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.CatalogItem);
                        if (catalogItemsNode.Parent is null)
                            programNode.Nodes.Add(catalogItemsNode);*/
                        if (entitlements.Any())
                            /*TreeNode entitlementsNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace + "_entitlements") ?? new();
                            entitlementsNode.Tag = selection.Platform;
                            entitlementsNode.Name = @namespace + "_entitlements";
                            entitlementsNode.Text = "Entitlements";
                            entitlementsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.Entitlement);
                            if (entitlementsNode.Parent is null)
                                programNode.Nodes.Add(entitlementsNode);*/
                            foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements)
                            {
                                string dlcId = pair.Key;
                                (DlcType type, string name, string icon) dlcApp = (DlcType.EpicEntitlement, pair.Value.name, pair.Value.icon);
                                selection.AllDlc[dlcId] = dlcApp;
                                if (allCheckBox.Checked)
                                    selection.SelectedDlc[dlcId] = dlcApp;
                                TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == dlcId) ?? new TreeNode();
                                dlcNode.Tag = selection.Platform;
                                dlcNode.Name = dlcId;
                                dlcNode.Text = dlcApp.name;
                                dlcNode.Checked = selection.SelectedDlc.ContainsKey(dlcId);
                                if (dlcNode.Parent is null)
                                    _ = programNode.Nodes.Add(dlcNode); //entitlementsNode.Nodes.Add(dlcNode);
                            }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }
        if (programsToScan.Any(c => c.platform is Platform.Ubisoft))
        {
            List<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
            foreach ((string gameId, string name, string gameDirectory) in ubisoftGames)
            {
                if (Program.Canceled)
                    return;
                if (Program.IsGameBlocked(name, gameDirectory) || !programsToScan.Any(c => c.platform is Platform.Ubisoft && c.id == gameId))
                    continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    List<string> dllDirectories = await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Ubisoft);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Ubisoft, gameId) ?? new ProgramSelection();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = gameId;
                    selection.Name = name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await UbisoftLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Ubisoft;
                    selection.IconUrl = IconGrabber.GetDomainFaviconUrl("store.ubi.com");
                    selectionTreeView.Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Ubisoft && s.Name == gameId) ?? new TreeNode();
                        programNode.Tag = selection.Platform;
                        programNode.Name = gameId;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        if (programNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(programNode);
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
        koaloaderAllCheckBox.Enabled = false;
        installButton.Enabled = false;
        uninstallButton.Enabled = installButton.Enabled;
        selectionTreeView.Enabled = false;
        saveButton.Enabled = false;
        loadButton.Enabled = false;
        resetButton.Enabled = false;
        saveKoaloaderButton.Enabled = false;
        loadKoaloaderButton.Enabled = false;
        resetKoaloaderButton.Enabled = false;
        progressLabel.Text = "Waiting for user to select which programs/games to scan . . .";
        ShowProgressBar();
        await ProgramData.Setup();
        bool scan = forceScan;
        if (!scan && (programsToScan is null || !programsToScan.Any() || forceProvideChoices))
        {
            List<(Platform platform, string id, string name, bool alreadySelected)> gameChoices = new();
            if (Directory.Exists(ParadoxLauncher.InstallPath))
                gameChoices.Add((Platform.Paradox, "PL", "Paradox Launcher",
                    programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Paradox && p.id == "PL")));
            if (Directory.Exists(SteamLibrary.InstallPath))
                foreach ((string appId, string name, string _, int _, string _) in (await SteamLibrary.GetGames()).Where(g
                             => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                    gameChoices.Add((Platform.Steam, appId, name,
                        programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Steam && p.id == appId)));
            if (Directory.Exists(EpicLibrary.EpicManifestsPath))
                foreach (Manifest manifest in (await EpicLibrary.GetGames()).Where(m => !Program.IsGameBlocked(m.DisplayName, m.InstallLocation)))
                    gameChoices.Add((Platform.Epic, manifest.CatalogNamespace, manifest.DisplayName,
                        programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Epic && p.id == manifest.CatalogNamespace)));
            foreach ((string gameId, string name, string _) in (await UbisoftLibrary.GetGames()).Where(g => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                gameChoices.Add((Platform.Ubisoft, gameId, name,
                    programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Ubisoft && p.id == gameId)));
            if (gameChoices.Any())
            {
                using SelectDialogForm form = new(this);
                List<(Platform platform, string id, string name)> choices = form.QueryUser("Choose which programs and/or games to scan for DLC:", gameChoices);
                scan = choices is not null && choices.Any();
                const string retry = "\n\nPress the \"Rescan Programs / Games\" button to re-choose.";
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
                progressLabel.Text = setup ? $"Setting up SteamCMD . . . {p}%" : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };
            if (Directory.Exists(SteamLibrary.InstallPath) && programsToScan is not null && programsToScan.Any(c => c.platform is Platform.Steam))
            {
                progressLabel.Text = "Setting up SteamCMD . . . ";
                await SteamCMD.Setup(iProgress);
            }
            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            ProgramSelection.ValidateAll(programsToScan);
            /*TreeNodes.ForEach(node =>
            {
                if (node.Tag is not Platform platform
                || node.Name is not string platformId
                || ProgramSelection.FromPlatformId(platform, platformId) is null
                && ProgramSelection.GetDlcFromPlatformId(platform, platformId) is null)
                    node.Remove();
            });*/
            TreeNodes.ForEach(node => node.Remove()); // nodes cause lots of lag during rescan for now
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();
        }
        OnLoadDlc(null, null);
        OnLoadKoaloader(null, null);
        HideProgressBar();
        selectionTreeView.Enabled = ProgramSelection.All.Any();
        allCheckBox.Enabled = selectionTreeView.Enabled;
        koaloaderAllCheckBox.Enabled = selectionTreeView.Enabled;
        noneFoundLabel.Visible = !selectionTreeView.Enabled;
        installButton.Enabled = ProgramSelection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        saveButton.Enabled = CanSaveDlc();
        loadButton.Enabled = CanLoadDlc();
        resetButton.Enabled = CanResetDlc();
        saveKoaloaderButton.Enabled = CanSaveKoaloader();
        loadKoaloaderButton.Enabled = CanLoadKoaloader();
        resetKoaloaderButton.Enabled = CanResetKoaloader();
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
        SyncNode(node);
        SyncNodeAncestors(node);
        SyncNodeDescendants(node);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = TreeNodes.TrueForAll(node => node.Text == "Unknown" || node.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        installButton.Enabled = ProgramSelection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        saveButton.Enabled = CanSaveDlc();
        resetButton.Enabled = CanResetDlc();
    }

    private static void SyncNodeAncestors(TreeNode node)
    {
        while (true)
        {
            TreeNode parentNode = node.Parent;
            if (parentNode is not null)
            {
                parentNode.Checked = parentNode.Nodes.Cast<TreeNode>().Any(childNode => childNode.Checked);
                node = parentNode;
                continue;
            }
            break;
        }
    }

    private static void SyncNodeDescendants(TreeNode node)
        => node.Nodes.Cast<TreeNode>().ToList().ForEach(childNode =>
        {
            if (childNode.Text == "Unknown")
                return;
            childNode.Checked = node.Checked;
            SyncNode(childNode);
            SyncNodeDescendants(childNode);
        });

    private static void SyncNode(TreeNode node)
    {
        string id = node.Name;
        Platform platform = (Platform)node.Tag;
        (string gameId, (DlcType type, string name, string icon) app)? dlc = ProgramSelection.GetDlcFromPlatformId(platform, id);
        if (dlc.HasValue)
        {
            (string gameId, _) = dlc.Value;
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, gameId);
            selection?.ToggleDlc(node.Name, node.Checked);
        }
        else
        {
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, id);
            if (selection is not null)
                selection.Enabled = node.Checked;
        }
    }

    private List<TreeNode> GatherTreeNodes(TreeNodeCollection nodeCollection)
    {
        List<TreeNode> treeNodes = new();
        foreach (TreeNode rootNode in nodeCollection)
        {
            treeNodes.Add(rootNode);
            treeNodes.AddRange(GatherTreeNodes(rootNode.Nodes));
        }
        return treeNodes;
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
            Height = programsGroupBox.Size.Height - 3 - progressLabel.Size.Height - progressLabelGames.Size.Height - progressLabelDLCs.Size.Height
                   - progressBar.Size.Height
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
            Height = programsGroupBox.Size.Height + 3 + progressLabel.Size.Height + progressLabelGames.Size.Height + progressLabelDLCs.Size.Height
                   + progressBar.Size.Height
        };
    }

    internal void OnNodeRightClick(TreeNode node, Point location)
        => Invoke(() =>
        {
            ContextMenuStrip contextMenuStrip = ContextMenuStrip;
            while (ContextMenuStrip.Tag is true)
                Thread.Sleep(100);
            ContextMenuStrip.Tag = true;
            ToolStripItemCollection items = contextMenuStrip.Items;
            items.Clear();
            string id = node.Name;
            Platform platform = (Platform)node.Tag;
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, id);
            (string gameAppId, (DlcType type, string name, string icon) app)? dlc = null;
            if (selection is null)
                dlc = ProgramSelection.GetDlcFromPlatformId(platform, id);
            ProgramSelection dlcParentSelection = null;
            if (dlc is not null)
                dlcParentSelection = ProgramSelection.FromPlatformId(platform, dlc.Value.gameAppId);
            if (selection is null && dlcParentSelection is null)
                return;
            ContextMenuItem header;
            if (id == "PL")
                header = new(node.Text, "Paradox Launcher");
            else if (selection is not null)
                header = new(node.Text, (id, selection.IconUrl));
            else
                header = new(node.Text, (id, dlc.Value.app.icon), (id, dlcParentSelection.IconUrl));
            items.Add(header);
            string appInfoVDF = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
            string appInfoJSON = $@"{SteamCMD.AppInfoPath}\{id}.json";
            string cooldown = $@"{ProgramData.CooldownPath}\{id}.txt";
            if (File.Exists(appInfoVDF) || File.Exists(appInfoJSON))
            {
                List<ContextMenuItem> queries = new();
                if (File.Exists(appInfoJSON))
                {
                    string platformString = selection is null || selection.Platform is Platform.Steam
                        ? "Steam Store "
                        : selection.Platform is Platform.Epic
                            ? "Epic GraphQL "
                            : "";
                    queries.Add(new($"Open {platformString}Query", "Notepad", (_, _) => Diagnostics.OpenFileInNotepad(appInfoJSON)));
                }
                if (File.Exists(appInfoVDF))
                    queries.Add(new("Open SteamCMD Query", "Notepad", (_, _) => Diagnostics.OpenFileInNotepad(appInfoVDF)));
                if (queries.Any())
                {
                    items.Add(new ToolStripSeparator());
                    foreach (ContextMenuItem query in queries)
                        items.Add(query);
                    items.Add(new ContextMenuItem("Refresh Queries", "Command Prompt", (_, _) =>
                    {
                        try
                        {
                            File.Delete(appInfoVDF);
                        }
                        catch
                        {
                            // ignored
                        }
                        try
                        {
                            File.Delete(appInfoJSON);
                        }
                        catch
                        {
                            // ignored
                        }
                        try
                        {
                            File.Delete(cooldown);
                        }
                        catch
                        {
                            // ignored
                        }
                        OnLoad(true);
                    }));
                }
            }
            if (selection is not null)
            {
                if (id == "PL")
                {
                    items.Add(new ToolStripSeparator());
                    async void EventHandler(object sender, EventArgs e) => await ParadoxLauncher.Repair(this, selection);
                    items.Add(new ContextMenuItem("Repair", "Command Prompt", EventHandler));
                }
                items.Add(new ToolStripSeparator());
                items.Add(new ContextMenuItem("Open Root Directory", "File Explorer",
                    (_, _) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory)));
                int executables = 0;
                foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories.ToList())
                    items.Add(new ContextMenuItem($"Open Executable Directory #{++executables} ({(binaryType == BinaryType.BIT32 ? "32" : "64")}-bit)",
                        "File Explorer", (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                List<string> directories = selection.DllDirectories.ToList();
                int steam = 0, epic = 0, r1 = 0, r2 = 0;
                if (selection.Platform is Platform.Steam or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                            out string config, out string old_log, out string log, out string cache);
                        if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(old_config)
                         || File.Exists(config) || File.Exists(old_log) || File.Exists(log) || File.Exists(cache))
                            items.Add(new ContextMenuItem($"Open Steamworks Directory #{++steam}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
                if (selection.Platform is Platform.Epic or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                            out string log);
                        if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config) || File.Exists(log))
                            items.Add(new ContextMenuItem($"Open EOS Directory #{++epic}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
                if (selection.Platform is Platform.Ubisoft)
                    foreach (string directory in directories)
                    {
                        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                            out string log);
                        if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config) || File.Exists(log))
                            items.Add(new ContextMenuItem($"Open Uplay R1 Directory #{++r1}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                        directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config,
                            out log);
                        if (File.Exists(old_api32) || File.Exists(old_api64) || File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64)
                         || File.Exists(api64_o) || File.Exists(config) || File.Exists(log))
                            items.Add(new ContextMenuItem($"Open Uplay R2 Directory #{++r2}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
            }
            if (id != "PL")
            {
                if (selection?.Platform is Platform.Steam || dlcParentSelection?.Platform is Platform.Steam)
                {
                    items.Add(new ToolStripSeparator());
                    items.Add(new ContextMenuItem("Open SteamDB", "SteamDB", (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamdb.info/app/" + id)));
                }
                if (selection is not null)
                    switch (selection.Platform)
                    {
                        case Platform.Steam:
                            items.Add(new ContextMenuItem("Open Steam Store", "Steam Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl)));
                            items.Add(new ContextMenuItem("Open Steam Community", ("Sub_" + id, selection.SubIconUrl), "Steam Community",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + id)));
                            break;
                        case Platform.Epic:
                            items.Add(new ToolStripSeparator());
                            items.Add(new ContextMenuItem("Open ScreamDB", "ScreamDB",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" + id)));
                            items.Add(new ContextMenuItem("Open Epic Games Store", "Epic Games",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl)));
                            break;
                        case Platform.Ubisoft:
                            items.Add(new ToolStripSeparator());
                            items.Add(new ContextMenuItem("Open Ubisoft Store", "Ubisoft Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(
                                    "https://store.ubi.com/us/" + selection.Name.Replace(" ", "-").ToLowerInvariant())));
                            break;
                    }
            }
            if (selection?.WebsiteUrl != null)
                items.Add(new ContextMenuItem("Open Official Website", ("Web_" + id, IconGrabber.GetDomainFaviconUrl(selection.WebsiteUrl)),
                    (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.WebsiteUrl)));
            contextMenuStrip.Show(selectionTreeView, location);
            contextMenuStrip.Refresh();
            ContextMenuStrip.Tag = null;
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
        if (!ProgramSelection.All.Any())
            return;
        if (ProgramSelection.AllEnabled.Any(selection => !Program.IsProgramRunningDialog(this, selection)))
            return;
        if (!uninstall && ParadoxLauncher.DlcDialog(this))
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
        bool shouldCheck = TreeNodes.Any(node => node.Parent is null && !node.Checked);
        foreach (TreeNode node in TreeNodes.Where(node => node.Parent is null && node.Checked != shouldCheck))
        {
            node.Checked = shouldCheck;
            OnTreeViewNodeCheckedChanged(null, new(node, TreeViewAction.ByMouse));
        }
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = shouldCheck;
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    private void OnKoaloaderAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = ProgramSelection.AllSafe.Any(selection => !selection.Koaloader);
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            selection.Koaloader = shouldCheck;
        selectionTreeView.Invalidate();
        koaloaderAllCheckBox.CheckedChanged -= OnKoaloaderAllCheckBoxChanged;
        koaloaderAllCheckBox.Checked = shouldCheck;
        koaloaderAllCheckBox.CheckedChanged += OnKoaloaderAllCheckBoxChanged;
    }

    private bool AreSelectionsDefault()
        => TreeNodes.All(node => node.Parent is null || node.Tag is not Platform || (node.Text == "Unknown" ? !node.Checked : node.Checked));

    private bool CanSaveDlc() => installButton.Enabled && (ProgramData.ReadDlcChoices().Any() || !AreSelectionsDefault());

    private void OnSaveDlc(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> choices = ProgramData.ReadDlcChoices().ToList();
        foreach (TreeNode node in TreeNodes)
            if (node.Parent is { } parent && node.Tag is Platform platform)
            {
                if (node.Text == "Unknown" ? node.Checked : !node.Checked)
                    choices.Add((platform, node.Parent.Name, node.Name));
                else
                    choices.RemoveAll(n => n.platform == platform && n.gameId == parent.Name && n.dlcId == node.Name);
            }
        choices = choices.Distinct().ToList();
        ProgramData.WriteDlcChoices(choices);
        loadButton.Enabled = CanLoadDlc();
        saveButton.Enabled = CanSaveDlc();
    }

    private static bool CanLoadDlc() => ProgramData.ReadDlcChoices().Any();

    private void OnLoadDlc(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> choices = ProgramData.ReadDlcChoices().ToList();
        foreach (TreeNode node in TreeNodes)
            if (node.Parent is { } parent && node.Tag is Platform platform)
            {
                node.Checked = choices.Any(c => c.platform == platform && c.gameId == parent.Name && c.dlcId == node.Name)
                    ? node.Text == "Unknown"
                    : node.Text != "Unknown";
                OnTreeViewNodeCheckedChanged(null, new(node, TreeViewAction.ByMouse));
            }
    }

    private bool CanResetDlc() => !AreSelectionsDefault();

    private void OnResetDlc(object sender, EventArgs e)
    {
        foreach (TreeNode node in TreeNodes.Where(node => node.Parent is not null && node.Tag is Platform))
        {
            node.Checked = node.Text != "Unknown";
            OnTreeViewNodeCheckedChanged(null, new(node, TreeViewAction.ByMouse));
        }
        resetButton.Enabled = CanResetDlc();
    }

    private static bool AreKoaloaderSelectionsDefault() => ProgramSelection.AllSafe.All(selection => selection.Koaloader && selection.KoaloaderProxy is null);

    private static bool CanSaveKoaloader() => ProgramData.ReadKoaloaderChoices().Any() || !AreKoaloaderSelectionsDefault();

    private void OnSaveKoaloader(object sender, EventArgs e)
    {
        List<(Platform platform, string id, string proxy, bool enabled)> choices = ProgramData.ReadKoaloaderChoices().ToList();
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
        {
            _ = choices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.KoaloaderProxy is not null and not ProgramSelection.DefaultKoaloaderProxy || !selection.Koaloader)
                choices.Add((selection.Platform, selection.Id,
                    selection.KoaloaderProxy == ProgramSelection.DefaultKoaloaderProxy ? null : selection.KoaloaderProxy, selection.Koaloader));
        }
        ProgramData.WriteKoaloaderProxyChoices(choices);
        saveKoaloaderButton.Enabled = CanSaveKoaloader();
        loadKoaloaderButton.Enabled = CanLoadKoaloader();
    }

    private static bool CanLoadKoaloader() => ProgramData.ReadKoaloaderChoices().Any();

    private void OnLoadKoaloader(object sender, EventArgs e)
    {
        List<(Platform platform, string id, string proxy, bool enabled)> choices = ProgramData.ReadKoaloaderChoices().ToList();
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            if (choices.Any(c => c.platform == selection.Platform && c.id == selection.Id))
            {
                (Platform platform, string id, string proxy, bool enabled)
                    choice = choices.First(c => c.platform == selection.Platform && c.id == selection.Id);
                (Platform platform, string id, string proxy, bool enabled) = choice;
                string currentProxy = proxy;
                if (proxy is not null && proxy.Contains('.')) // convert pre-v4.1.0.0 choices
                    proxy.GetProxyInfoFromIdentifier(out currentProxy, out _);
                if (proxy != currentProxy && choices.Remove(choice)) // convert pre-v4.1.0.0 choices
                    choices.Add((platform, id, currentProxy, enabled));
                if (currentProxy is null or ProgramSelection.DefaultKoaloaderProxy && enabled)
                    _ = choices.RemoveAll(c => c.platform == platform && c.id == id);
                else
                {
                    selection.Koaloader = enabled;
                    selection.KoaloaderProxy = currentProxy == ProgramSelection.DefaultKoaloaderProxy ? currentProxy : proxy;
                }
            }
            else
            {
                selection.Koaloader = true;
                selection.KoaloaderProxy = null;
            }
        ProgramData.WriteKoaloaderProxyChoices(choices);
        loadKoaloaderButton.Enabled = CanLoadKoaloader();
        OnKoaloaderChanged();
    }

    private static bool CanResetKoaloader() => !AreKoaloaderSelectionsDefault();

    private void OnResetKoaloader(object sender, EventArgs e)
    {
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
        {
            selection.Koaloader = true;
            selection.KoaloaderProxy = null;
        }
        OnKoaloaderChanged();
    }

    internal void OnKoaloaderChanged()
    {
        selectionTreeView.Invalidate();
        saveKoaloaderButton.Enabled = CanSaveKoaloader();
        resetKoaloaderButton.Enabled = CanResetKoaloader();
        koaloaderAllCheckBox.CheckedChanged -= OnKoaloaderAllCheckBoxChanged;
        koaloaderAllCheckBox.Checked = ProgramSelection.AllSafe.TrueForAll(selection => selection.Koaloader);
        koaloaderAllCheckBox.CheckedChanged += OnKoaloaderAllCheckBoxChanged;
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
          + "\nYou disable this option and install DLC unlockers to protected games at your own risk!" + "\n\nBlocked games: "
          + (string.IsNullOrWhiteSpace(blockedGames.ToString()) ? "(none)" : blockedGames) + "\n\nBlocked game sub-directories: "
          + (string.IsNullOrWhiteSpace(blockedDirectories.ToString()) ? "(none)" : blockedDirectories) + "\n\nBlocked game sub-directory exceptions: "
          + (string.IsNullOrWhiteSpace(blockedDirectoryExceptions.ToString()) ? "(none)" : blockedDirectoryExceptions),
            customFormText: "Block Protected Games");
    }

    private void OnSortCheckBoxChanged(object sender, EventArgs e)
        => selectionTreeView.TreeViewNodeSorter = sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;
}