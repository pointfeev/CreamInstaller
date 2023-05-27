using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
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

    private readonly ConcurrentDictionary<string, string> remainingDLCs = new();

    private readonly ConcurrentDictionary<string, string> remainingGames = new();

    private List<(Platform platform, string id, string name)> programsToScan;

    internal SelectForm()
    {
        InitializeComponent();
        Text = Program.ApplicationName;
    }

    public override ContextMenuStrip ContextMenuStrip => base.ContextMenuStrip ??= new();

    private List<TreeNode> TreeNodes => GatherTreeNodes(selectionTreeView.Nodes);

    private static void UpdateRemaining(Label label, ConcurrentDictionary<string, string> list, string descriptor)
        => label.Text = list.IsEmpty ? "" : $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list.Values).Replace("&", "&&");

    private void UpdateRemainingGames() => UpdateRemaining(progressLabelGames, remainingGames, "games");

    private void AddToRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        progressLabelGames.Invoke(delegate
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
        progressLabelGames.Invoke(delegate
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
        progressLabelDLCs.Invoke(delegate
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
        progressLabelDLCs.Invoke(delegate
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
        List<TreeNode> treeNodes = TreeNodes;
        remainingGames.Clear(); // for display purposes only, otherwise ignorable
        remainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Paradox))
        {
            AddToRemainingGames("Paradox Launcher");
            HashSet<string> dllDirectories = await ParadoxLauncher.InstallPath.GetDllDirectoriesFromGameDirectory(Platform.Paradox);
            if (dllDirectories is not null)
            {
                if (uninstallAll)
                {
                    Selection bareSelection = Selection.FromPlatformId(Platform.Paradox, "PL") ?? new();
                    bareSelection.Enabled = true;
                    bareSelection.Id = "PL";
                    bareSelection.Name = "Paradox Launcher";
                    bareSelection.RootDirectory = ParadoxLauncher.InstallPath;
                    bareSelection.ExecutableDirectories = await ParadoxLauncher.GetExecutableDirectories(bareSelection.RootDirectory);
                    bareSelection.DllDirectories = dllDirectories;
                    bareSelection.Platform = Platform.Paradox;
                }
                else
                {
                    Selection selection = Selection.FromPlatformId(Platform.Paradox, "PL") ?? new();
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
                RemoveFromRemainingGames("Paradox Launcher");
            }
        }
        int steamGamesToCheck;
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Steam))
        {
            HashSet<(string appId, string name, string branch, int buildId, string gameDirectory)> steamGames = await SteamLibrary.GetGames();
            steamGamesToCheck = steamGames.Count;
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in steamGames)
            {
                if (Program.Canceled)
                    return;
                if (!uninstallAll && (Program.IsGameBlocked(name, gameDirectory) || !programsToScan.Any(c => c.platform is Platform.Steam && c.id == appId)))
                {
                    _ = Interlocked.Decrement(ref steamGamesToCheck);
                    continue;
                }
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories = await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Steam);
                    if (dllDirectories is null)
                    {
                        _ = Interlocked.Decrement(ref steamGamesToCheck);
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.FromPlatformId(Platform.Steam, appId) ?? new Selection();
                        bareSelection.Enabled = true;
                        bareSelection.Id = appId;
                        bareSelection.Name = name;
                        bareSelection.RootDirectory = gameDirectory;
                        bareSelection.ExecutableDirectories = await SteamLibrary.GetExecutableDirectories(bareSelection.RootDirectory);
                        bareSelection.DllDirectories = dllDirectories;
                        bareSelection.Platform = Platform.Steam;
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    AppData appData = await SteamStore.QueryStoreAPI(appId);
                    _ = Interlocked.Decrement(ref steamGamesToCheck);
                    VProperty appInfo = await SteamCMD.GetAppInfo(appId, branch, buildId);
                    if (appData is null && appInfo is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<string, SelectionDLC> dlc = new();
                    List<Task> dlcTasks = new();
                    List<string> dlcIds = new();
                    if (appData is not null)
                        dlcIds.AddRange(await SteamStore.ParseDlcAppIds(appData));
                    if (appInfo is not null)
                        dlcIds.AddRange(await SteamCMD.ParseDlcAppIds(appInfo));
                    dlcIds = dlcIds.Distinct().ToList();
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
                                AppData dlcAppData = await SteamStore.QueryStoreAPI(dlcAppId, true);
                                if (dlcAppData is not null)
                                {
                                    dlcName = dlcAppData.Name;
                                    dlcIcon = dlcAppData.HeaderImage;
                                    onSteamStore = true;
                                    fullGameAppId = dlcAppData.FullGame?.AppId;
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
                                        fullGameAppId = dlcAppInfo.Value.GetChild("common")?.GetChild("parent")?.ToString();
                                    }
                                }
                                if (fullGameAppId != null && fullGameAppId != appId)
                                {
                                    string fullGameName = null;
                                    string fullGameIcon = null;
                                    bool fullGameOnSteamStore = false;
                                    AppData fullGameAppData = await SteamStore.QueryStoreAPI(fullGameAppId, true);
                                    if (fullGameAppData is not null)
                                    {
                                        fullGameName = fullGameAppData.Name;
                                        fullGameIcon = fullGameAppData.HeaderImage;
                                        fullGameOnSteamStore = true;
                                    }
                                    else
                                    {
                                        VProperty fullGameAppInfo = await SteamCMD.GetAppInfo(fullGameAppId);
                                        if (fullGameAppInfo is not null)
                                        {
                                            fullGameName = fullGameAppInfo.Value.GetChild("common")?.GetChild("name")?.ToString();
                                            string fullGameIconStaticId = fullGameAppInfo.Value.GetChild("common")?.GetChild("icon")?.ToString();
                                            fullGameIconStaticId ??= fullGameAppInfo.Value.GetChild("common")?.GetChild("logo_small")?.ToString();
                                            fullGameIconStaticId ??= fullGameAppInfo.Value.GetChild("common")?.GetChild("logo")?.ToString();
                                            if (fullGameIconStaticId is not null)
                                                dlcIcon = IconGrabber.SteamAppImagesPath + @$"\{fullGameAppId}\{fullGameIconStaticId}.jpg";
                                        }
                                    }
                                    if (Program.Canceled)
                                        return;
                                    if (!string.IsNullOrWhiteSpace(fullGameName))
                                        dlc[fullGameAppId] = new()
                                        {
                                            Id = fullGameAppId, Type = fullGameOnSteamStore ? DLCType.Steam : DLCType.SteamHidden, Name = fullGameName,
                                            Icon = fullGameIcon
                                        };
                                }
                                if (Program.Canceled)
                                    return;
                                if (string.IsNullOrWhiteSpace(dlcName))
                                    dlcName = "Unknown";
                                dlc[dlcAppId] = new()
                                {
                                    Id = dlcAppId, Type = onSteamStore ? DLCType.Steam : DLCType.SteamHidden, Name = dlcName, Icon = dlcIcon
                                };
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
                    Selection selection = Selection.FromPlatformId(Platform.Steam, appId) ?? new Selection();
                    selection.Enabled = allCheckBox.Checked || selection.DLC.Any(dlc => dlc.Enabled)
                                                            || selection.ExtraSelections.Any(extraSelection => extraSelection.DLC.Any(dlc => dlc.Enabled));
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = appId;
                    selection.Name = appData?.Name ?? name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await SteamLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Steam;
                    selection.Product = "https://store.steampowered.com/app/" + appId;
                    selection.Icon = IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value.GetChild("common")?.GetChild("icon")}.jpg";
                    selection.SubIcon = appData?.HeaderImage ?? IconGrabber.SteamAppImagesPath
                      + @$"\{appId}\{appInfo?.Value.GetChild("common")?.GetChild("clienticon")}.ico";
                    selection.Publisher = appData?.Publishers[0] ?? appInfo?.Value.GetChild("extended")?.GetChild("publisher")?.ToString();
                    selection.Website = appData?.Website;
                    if (Program.Canceled)
                        return;
                    selectionTreeView.Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == appId) ?? new TreeNode();
                        programNode.Tag = selection.Platform;
                        programNode.Name = appId;
                        programNode.Text = appData?.Name ?? name;
                        programNode.Checked = selection.Enabled;
                        if (programNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(programNode);
                        foreach ((_, SelectionDLC dlc) in dlc)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                            dlc.Enabled = dlc.Name != "Unknown" && allCheckBox.Checked;
                            TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == dlc.Id) ?? new TreeNode();
                            dlcNode.Tag = selection.Platform;
                            dlcNode.Name = dlc.Id;
                            dlcNode.Text = dlc.Name;
                            dlcNode.Checked = dlc.Enabled;
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
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Epic))
        {
            HashSet<Manifest> epicGames = await EpicLibrary.GetGames();
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                if (Program.Canceled)
                    return;
                if (!uninstallAll && (Program.IsGameBlocked(name, directory) || !programsToScan.Any(c => c.platform is Platform.Epic && c.id == @namespace)))
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
                        Selection bareSelection = Selection.FromPlatformId(Platform.Epic, @namespace) ?? new Selection();
                        bareSelection.Enabled = true;
                        bareSelection.Id = @namespace;
                        bareSelection.Name = name;
                        bareSelection.RootDirectory = directory;
                        bareSelection.ExecutableDirectories = await EpicLibrary.GetExecutableDirectories(bareSelection.RootDirectory);
                        bareSelection.DllDirectories = dllDirectories;
                        bareSelection.Platform = Platform.Epic;
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<string, SelectionDLC> catalogItems = new();
                    // get catalog items
                    ConcurrentDictionary<string, SelectionDLC> entitlements = new();
                    List<Task> dlcTasks = new();
                    List<(string id, string name, string product, string icon, string developer)>
                        entitlementIds = await EpicStore.QueryEntitlements(@namespace);
                    if (entitlementIds.Count > 0)
                        foreach ((string id, string name, string product, string icon, string developer) in entitlementIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled)
                                    return;
                                entitlements[id] = new()
                                {
                                    Id = id, Name = name, Product = product, Icon = icon,
                                    Publisher = developer
                                };
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
                    if (catalogItems.IsEmpty && entitlements.IsEmpty)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    Selection selection = Selection.FromPlatformId(Platform.Epic, @namespace) ?? new Selection();
                    selection.Enabled = allCheckBox.Checked || selection.DLC.Any(dlc => dlc.Enabled)
                                                            || selection.ExtraSelections.Any(extraSelection => extraSelection.DLC.Any(dlc => dlc.Enabled));
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = @namespace;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.ExecutableDirectories = await EpicLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Epic;
                    foreach (KeyValuePair<string, SelectionDLC> dlc in entitlements.Where(dlc => dlc.Value.Name == selection.Name))
                    {
                        if (Program.Canceled)
                            return;
                        selection.Product = "https://www.epicgames.com/store/product/" + dlc.Value.Product;
                        selection.Icon = dlc.Value.Icon;
                        selection.Publisher = dlc.Value.Publisher;
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
                        if (!catalogItems.IsEmpty)
                            /*TreeNode catalogItemsNode = treeNodes.Find(node => node.Tag is Platform.Epic && node.Name == @namespace + "_catalogItems") ?? new();
                            catalogItemsNode.Name = @namespace + "_catalogItems";
                            catalogItemsNode.Text = "Catalog Items";
                            catalogItemsNode.Checked = selection.DLC.Any(dlc => dlc.Type is DLCType.EpicCatalogItem && dlc.Enabled);
                            if (catalogItemsNode.Parent is null)
                                _ = programNode.Nodes.Add(catalogItemsNode);*/
                            foreach ((_, SelectionDLC dlc) in catalogItems)
                            {
                                if (Program.Canceled)
                                    return;
                                dlc.Selection = selection;
                                dlc.Enabled = allCheckBox.Checked;
                                TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == dlc.Id) ?? new TreeNode();
                                dlcNode.Tag = selection.Platform;
                                dlcNode.Name = dlc.Id;
                                dlcNode.Text = dlc.Name;
                                dlcNode.Checked = dlc.Enabled;
                                if (dlcNode.Parent is null)
                                    _ = programNode.Nodes.Add(dlcNode); //_ = catalogItemsNode.Nodes.Add(dlcNode);
                            }
                        if (entitlements.IsEmpty)
                            return;
                        /*TreeNode entitlementsNode = treeNodes.Find(node => node.Tag is Platform.Epic && node.Name == @namespace + "_entitlements") ?? new();
                        entitlementsNode.Name = @namespace + "_entitlements";
                        entitlementsNode.Text = "Entitlements";
                        entitlementsNode.Checked = selection.DLC.Any(dlc => dlc.Type is DLCType.EpicEntitlement && dlc.Enabled);
                        if (entitlementsNode.Parent is null)
                            _ = programNode.Nodes.Add(entitlementsNode);*/
                        foreach ((_, SelectionDLC dlc) in entitlements)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                            dlc.Enabled = allCheckBox.Checked;
                            TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == dlc.Id) ?? new TreeNode();
                            dlcNode.Tag = selection.Platform;
                            dlcNode.Name = dlc.Id;
                            dlcNode.Text = dlc.Name;
                            dlcNode.Checked = dlc.Enabled;
                            if (dlcNode.Parent is null)
                                _ = programNode.Nodes.Add(dlcNode); //_ = entitlementsNode.Nodes.Add(dlcNode);
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
            HashSet<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
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
                    HashSet<string> dllDirectories = await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Ubisoft);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.FromPlatformId(Platform.Ubisoft, gameId) ?? new Selection();
                        bareSelection.Enabled = true;
                        bareSelection.Id = gameId;
                        bareSelection.Name = name;
                        bareSelection.RootDirectory = gameDirectory;
                        bareSelection.ExecutableDirectories = await UbisoftLibrary.GetExecutableDirectories(bareSelection.RootDirectory);
                        bareSelection.DllDirectories = dllDirectories;
                        bareSelection.Platform = Platform.Ubisoft;
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled)
                        return;
                    Selection selection = Selection.FromPlatformId(Platform.Ubisoft, gameId) ?? new Selection();
                    selection.Enabled = allCheckBox.Checked || selection.DLC.Any(dlc => dlc.Enabled)
                                                            || selection.ExtraSelections.Any(extraSelection => extraSelection.DLC.Any(dlc => dlc.Enabled));
                    if (koaloaderAllCheckBox.Checked)
                        selection.Koaloader = true;
                    selection.Id = gameId;
                    selection.Name = name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await UbisoftLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Ubisoft;
                    selection.Icon = IconGrabber.GetDomainFaviconUrl("store.ubi.com");
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
        await ProgramData.Setup(this);
        bool scan = forceScan;
        if (!scan && (programsToScan is null || programsToScan.Count < 1 || forceProvideChoices))
        {
            List<(Platform platform, string id, string name, bool alreadySelected)> gameChoices = new();
            if (ParadoxLauncher.InstallPath.DirectoryExists())
                gameChoices.Add((Platform.Paradox, "PL", "Paradox Launcher",
                    programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Paradox && p.id == "PL")));
            if (SteamLibrary.InstallPath.DirectoryExists())
                foreach ((string appId, string name, string _, int _, string _) in (await SteamLibrary.GetGames()).Where(g
                             => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                    gameChoices.Add((Platform.Steam, appId, name,
                        programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Steam && p.id == appId)));
            if (EpicLibrary.EpicManifestsPath.DirectoryExists())
                gameChoices.AddRange((await EpicLibrary.GetGames()).Where(m => !Program.IsGameBlocked(m.DisplayName, m.InstallLocation)).Select(manifest
                    => (Platform.Epic, manifest.CatalogNamespace, manifest.DisplayName,
                        programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Epic && p.id == manifest.CatalogNamespace))));
            foreach ((string gameId, string name, string _) in (await UbisoftLibrary.GetGames()).Where(g => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                gameChoices.Add((Platform.Ubisoft, gameId, name,
                    programsToScan is not null && programsToScan.Any(p => p.platform is Platform.Ubisoft && p.id == gameId)));
            if (gameChoices.Count > 0)
            {
                using SelectDialogForm form = new(this);
                DialogResult selectResult = form.QueryUser("Choose which programs and/or games to scan:", gameChoices,
                    out List<(Platform platform, string id, string name)> choices);
                if (selectResult == DialogResult.Abort) // will be an uninstall all button
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
                    TreeNodes.ForEach(node => node.Remove());
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
                progressLabel.Text = setup ? $"Setting up SteamCMD . . . {p}%" : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };
            if (SteamLibrary.InstallPath.DirectoryExists() && programsToScan is not null && programsToScan.Any(c => c.platform is Platform.Steam))
            {
                progressLabel.Text = "Setting up SteamCMD . . . ";
                if (!await SteamCMD.Setup(iProgress))
                {
                    OnLoad(forceScan, true);
                    return;
                }
            }
            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            Selection.ValidateAll(programsToScan);
            TreeNodes.ForEach(node => node.Remove());
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();
        }
        OnLoadDlc(null, null);
        OnLoadKoaloader(null, null);
        HideProgressBar();
        selectionTreeView.Enabled = Selection.All.Count > 0;
        allCheckBox.Enabled = selectionTreeView.Enabled;
        koaloaderAllCheckBox.Enabled = selectionTreeView.Enabled;
        noneFoundLabel.Visible = !selectionTreeView.Enabled;
        installButton.Enabled = Selection.AllEnabled.Count > 0;
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
        installButton.Enabled = Selection.AllEnabled.Count > 0;
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
        Selection selection = Selection.FromPlatformId(platform, id);
        if (selection is not null)
        {
            selection.Enabled = node.Checked;
            return;
        }
        SelectionDLC dlc = SelectionDLC.FromPlatformId(platform, id);
        if (dlc is not null)
            dlc.Enabled = node.Checked;
    }

    private static List<TreeNode> GatherTreeNodes(TreeNodeCollection nodeCollection)
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
            ContextMenuStrip contextMenuStrip = new();
            ToolStripItemCollection items = contextMenuStrip.Items;
            string id = node.Name;
            Platform platform = (Platform)node.Tag;
            Selection selection = Selection.FromPlatformId(platform, id);
            SelectionDLC dlc = null;
            if (selection is null)
                dlc = SelectionDLC.FromPlatformId(platform, id);
            Selection dlcParentSelection = null;
            if (dlc is not null)
                dlcParentSelection = Selection.FromPlatformId(platform, dlc.Selection.Id);
            if (selection is null && dlcParentSelection is null)
                return;
            ContextMenuItem header = id == "PL"
                ? new(node.Text, "Paradox Launcher")
                : selection is not null
                    ? new(node.Text, (id, selection.Icon))
                    : new(node.Text, (id, dlc.Icon), (id, dlcParentSelection.Icon));
            _ = items.Add(header);
            string appInfoVDF = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
            string appInfoJSON = $@"{SteamCMD.AppInfoPath}\{id}.json";
            string cooldown = $@"{ProgramData.CooldownPath}\{id}.txt";
            if (appInfoVDF.FileExists() || appInfoJSON.FileExists())
            {
                List<ContextMenuItem> queries = new();
                if (appInfoJSON.FileExists())
                {
                    string platformString = selection is null || selection.Platform is Platform.Steam
                        ? "Steam Store "
                        : selection.Platform is Platform.Epic
                            ? "Epic GraphQL "
                            : "";
                    queries.Add(new($"Open {platformString}Query", "Notepad", (_, _) => Diagnostics.OpenFileInNotepad(appInfoJSON)));
                }
                if (appInfoVDF.FileExists())
                    queries.Add(new("Open SteamCMD Query", "Notepad", (_, _) => Diagnostics.OpenFileInNotepad(appInfoVDF)));
                if (queries.Count > 0)
                {
                    _ = items.Add(new ToolStripSeparator());
                    foreach (ContextMenuItem query in queries)
                        _ = items.Add(query);
                    _ = items.Add(new ContextMenuItem("Refresh Queries", "Command Prompt", (_, _) =>
                    {
                        appInfoVDF.DeleteFile();
                        appInfoJSON.DeleteFile();
                        cooldown.DeleteFile();
                        OnLoad(true);
                    }));
                }
            }
            if (selection is not null)
            {
                if (id == "PL")
                {
                    _ = items.Add(new ToolStripSeparator());
                    async void EventHandler(object sender, EventArgs e) => await ParadoxLauncher.Repair(this, selection);
                    _ = items.Add(new ContextMenuItem("Repair", "Command Prompt", EventHandler));
                }
                _ = items.Add(new ToolStripSeparator());
                _ = items.Add(new ContextMenuItem("Open Root Directory", "File Explorer",
                    (_, _) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory)));
                int executables = 0;
                foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories.ToList())
                    _ = items.Add(new ContextMenuItem($"Open Executable Directory #{++executables} ({(binaryType == BinaryType.BIT32 ? "32" : "64")}-bit)",
                        "File Explorer", (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                List<string> directories = selection.DllDirectories.ToList();
                int steam = 0, epic = 0, r1 = 0, r2 = 0;
                if (selection.Platform is Platform.Steam or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                            out string config, out string old_log, out string log, out string cache);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() || old_config.FileExists()
                         || config.FileExists() || old_log.FileExists() || log.FileExists() || cache.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Steamworks Directory #{++steam}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
                if (selection.Platform is Platform.Epic or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                            out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() || config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open EOS Directory #{++epic}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
                if (selection.Platform is Platform.Ubisoft)
                    foreach (string directory in directories)
                    {
                        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                            out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() || config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Uplay R1 Directory #{++r1}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                        directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config,
                            out log);
                        if (old_api32.FileExists() || old_api64.FileExists() || api32.FileExists() || api32_o.FileExists() || api64.FileExists()
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
                            _ = items.Add(new ContextMenuItem("Open Steam Community", ("Sub_" + id, selection.SubIcon), "Steam Community",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + id)));
                            break;
                        case Platform.Epic:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open ScreamDB", "ScreamDB",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" + id)));
                            _ = items.Add(new ContextMenuItem("Open Epic Games Store", "Epic Games",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Product)));
                            break;
                        case Platform.Ubisoft:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open Ubisoft Store", "Ubisoft Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(
                                    "https://store.ubi.com/us/" + selection.Name.Replace(" ", "-").ToLowerInvariant())));
                            break;
                    }
            }
            if (selection?.Website is not null)
                _ = items.Add(new ContextMenuItem("Open Official Website", ("Web_" + id, IconGrabber.GetDomainFaviconUrl(selection.Website)),
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
        if (Selection.All.Count < 1)
            return;
        if (Selection.AllEnabled.Any(selection => !Program.AreDllsLockedDialog(this, selection)))
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
        bool shouldCheck = Selection.AllSafe.Any(selection => !selection.Koaloader);
        foreach (Selection selection in Selection.AllSafe)
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
                    _ = choices.RemoveAll(n => n.platform == platform && n.gameId == parent.Name && n.dlcId == node.Name);
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

    private static bool AreKoaloaderSelectionsDefault() => Selection.AllSafe.All(selection => selection.Koaloader && selection.KoaloaderProxy is null);

    private static bool CanSaveKoaloader() => ProgramData.ReadKoaloaderChoices().Any() || !AreKoaloaderSelectionsDefault();

    private void OnSaveKoaloader(object sender, EventArgs e)
    {
        List<(Platform platform, string id, string proxy, bool enabled)> choices = ProgramData.ReadKoaloaderChoices().ToList();
        foreach (Selection selection in Selection.AllSafe)
        {
            _ = choices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.KoaloaderProxy is not null and not Selection.DefaultKoaloaderProxy || !selection.Koaloader)
                choices.Add((selection.Platform, selection.Id, selection.KoaloaderProxy == Selection.DefaultKoaloaderProxy ? null : selection.KoaloaderProxy,
                    selection.Koaloader));
        }
        ProgramData.WriteKoaloaderProxyChoices(choices);
        saveKoaloaderButton.Enabled = CanSaveKoaloader();
        loadKoaloaderButton.Enabled = CanLoadKoaloader();
    }

    private static bool CanLoadKoaloader() => ProgramData.ReadKoaloaderChoices().Any();

    private void OnLoadKoaloader(object sender, EventArgs e)
    {
        List<(Platform platform, string id, string proxy, bool enabled)> choices = ProgramData.ReadKoaloaderChoices().ToList();
        foreach (Selection selection in Selection.AllSafe)
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
                if (currentProxy is null or Selection.DefaultKoaloaderProxy && enabled)
                    _ = choices.RemoveAll(c => c.platform == platform && c.id == id);
                else
                {
                    selection.Koaloader = enabled;
                    selection.KoaloaderProxy = currentProxy == Selection.DefaultKoaloaderProxy ? currentProxy : proxy;
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
        foreach (Selection selection in Selection.AllSafe)
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
        koaloaderAllCheckBox.Checked = Selection.AllSafe.TrueForAll(selection => selection.Koaloader);
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