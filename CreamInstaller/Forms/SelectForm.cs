#pragma warning disable IDE0058

using CreamInstaller.Components;
using CreamInstaller.Epic;
using CreamInstaller.Paradox;
using CreamInstaller.Resources;
using CreamInstaller.Steam;
using CreamInstaller.Ubisoft;
using CreamInstaller.Utility;

using Gameloop.Vdf.Linq;

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

using static CreamInstaller.Resources.Resources;

namespace CreamInstaller;

internal partial class SelectForm : CustomForm
{
    internal SelectForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
    }

    public override ContextMenuStrip ContextMenuStrip => base.ContextMenuStrip ??= new();

    private static void UpdateRemaining(Label label, SynchronizedCollection<string> list, string descriptor) =>
        label.Text = list.Any() ? $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list).Replace("&", "&&") : "";

    private readonly SynchronizedCollection<string> RemainingGames = new();
    private void UpdateRemainingGames() => UpdateRemaining(progressLabelGames, RemainingGames, "games");
    private void AddToRemainingGames(string gameName)
    {
        if (Program.Canceled) return;
        Program.Invoke(progressLabelGames, delegate
        {
            if (Program.Canceled) return;
            if (!RemainingGames.Contains(gameName))
                RemainingGames.Add(gameName);
            UpdateRemainingGames();
        });
    }
    private void RemoveFromRemainingGames(string gameName)
    {
        if (Program.Canceled) return;
        Program.Invoke(progressLabelGames, delegate
        {
            if (Program.Canceled) return;
            RemainingGames.Remove(gameName);
            UpdateRemainingGames();
        });
    }

    private readonly SynchronizedCollection<string> RemainingDLCs = new();
    private void UpdateRemainingDLCs() => UpdateRemaining(progressLabelDLCs, RemainingDLCs, "DLCs");
    private void AddToRemainingDLCs(string dlcId)
    {
        if (Program.Canceled) return;
        Program.Invoke(progressLabelDLCs, delegate
        {
            if (Program.Canceled) return;
            if (!RemainingDLCs.Contains(dlcId))
                RemainingDLCs.Add(dlcId);
            UpdateRemainingDLCs();
        });
    }
    private void RemoveFromRemainingDLCs(string dlcId)
    {
        if (Program.Canceled) return;
        Program.Invoke(progressLabelDLCs, delegate
        {
            if (Program.Canceled) return;
            RemainingDLCs.Remove(dlcId);
            UpdateRemainingDLCs();
        });
    }

    private async Task GetApplicablePrograms(IProgress<int> progress)
    {
        if (ProgramsToScan is null || !ProgramsToScan.Any()) return;
        int TotalGameCount = 0;
        int CompleteGameCount = 0;
        void AddToRemainingGames(string gameName)
        {
            this.AddToRemainingGames(gameName);
            progress.Report(-Interlocked.Increment(ref TotalGameCount));
            progress.Report(CompleteGameCount);
        }
        void RemoveFromRemainingGames(string gameName)
        {
            this.RemoveFromRemainingGames(gameName);
            progress.Report(Interlocked.Increment(ref CompleteGameCount));
        }
        if (Program.Canceled) return;
        List<TreeNode> treeNodes = TreeNodes;
        RemainingGames.Clear(); // for display purposes only, otherwise ignorable
        RemainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        if (ProgramsToScan.Any(c => c.platform is Platform.Paradox))
        {
            List<string> dllDirectories = await ParadoxLauncher.InstallPath.GetDllDirectoriesFromGameDirectory(Platform.Paradox);
            if (dllDirectories is not null)
            {
                ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Paradox, "PL");
                selection ??= new();
                if (allCheckBox.Checked) selection.Enabled = true;
                if (koaloaderAllCheckBox.Checked) selection.Koaloader = true;
                selection.Id = "PL";
                selection.Name = "Paradox Launcher";
                selection.RootDirectory = ParadoxLauncher.InstallPath;
                selection.ExecutableDirectories = await ParadoxLauncher.GetExecutableDirectories(selection.RootDirectory);
                selection.DllDirectories = dllDirectories;
                selection.Platform = Platform.Paradox;

                TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Paradox && s.Name == selection.Id) ?? new();
                programNode.Tag = selection.Platform;
                programNode.Name = selection.Id;
                programNode.Text = selection.Name;
                programNode.Checked = selection.Enabled;
                programNode.Remove();
                _ = selectionTreeView.Nodes.Add(programNode);
            }
        }
        int steamGamesToCheck;
        if (ProgramsToScan.Any(c => c.platform is Platform.Steam))
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> steamGames = await SteamLibrary.GetGames();
            steamGamesToCheck = steamGames.Count;
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in steamGames)
            {
                if (Program.Canceled) return;
                if (Program.IsGameBlocked(name, gameDirectory) || !ProgramsToScan.Any(c => c.platform is Platform.Steam && c.id == appId))
                {
                    Interlocked.Decrement(ref steamGamesToCheck);
                    continue;
                }
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled) return;
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
                    if (Program.Canceled) return;
                    ConcurrentDictionary<string, (DlcType type, string name, string icon)> dlc = new();
                    List<Task> dlcTasks = new();
                    List<string> dlcIds = new();
                    if (appData is not null) dlcIds.AddRange(await SteamStore.ParseDlcAppIds(appData));
                    if (appInfo is not null) dlcIds.AddRange(await SteamCMD.ParseDlcAppIds(appInfo));
                    if (dlcIds.Count > 0)
                    {
                        foreach (string dlcAppId in dlcIds)
                        {
                            if (Program.Canceled) return;
                            AddToRemainingDLCs(dlcAppId);
                            Task task = Task.Run(async () =>
                            {
                                if (Program.Canceled) return;
                                do // give games steam store api limit priority
                                    Thread.Sleep(200);
                                while (!Program.Canceled && steamGamesToCheck > 0);
                                if (Program.Canceled) return;
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
                                        dlcName = dlcAppInfo.Value?.GetChild("common")?.GetChild("name")?.ToString();
                                        string dlcIconStaticId = dlcAppInfo.Value?.GetChild("common")?.GetChild("icon")?.ToString();
                                        dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo_small")?.ToString();
                                        dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo")?.ToString();
                                        if (dlcIconStaticId is not null)
                                            dlcIcon = IconGrabber.SteamAppImagesPath + @$"\{dlcAppId}\{dlcIconStaticId}.jpg";
                                    }
                                }
                                if (Program.Canceled) return;
                                if (string.IsNullOrWhiteSpace(dlcName))
                                    dlcName = "Unknown";
                                dlc[dlcAppId] = (onSteamStore ? DlcType.Steam : DlcType.SteamHidden, dlcName, dlcIcon);
                                RemoveFromRemainingDLCs(dlcAppId);
                            });
                            dlcTasks.Add(task);
                        }
                    }
                    else
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled) return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled) return;
                        await task;
                    }
                    steamGamesToCheck = 0;

                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Steam, appId) ?? new();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked) selection.Koaloader = true;
                    selection.Id = appId;
                    selection.Name = appData?.name ?? name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await SteamLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Steam;
                    selection.ProductUrl = "https://store.steampowered.com/app/" + appId;
                    selection.IconUrl = IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value?.GetChild("common")?.GetChild("icon")}.jpg";
                    selection.SubIconUrl = appData?.header_image ?? IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value?.GetChild("common")?.GetChild("clienticon")}.ico";
                    selection.Publisher = appData?.publishers[0] ?? appInfo?.Value?.GetChild("extended")?.GetChild("publisher")?.ToString();
                    selection.WebsiteUrl = appData?.website;

                    if (Program.Canceled) return;
                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == appId) ?? new();
                        programNode.Tag = selection.Platform;
                        programNode.Name = appId;
                        programNode.Text = appData?.name ?? name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        _ = selectionTreeView.Nodes.Add(programNode);
                        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in dlc)
                        {
                            if (Program.Canceled || programNode is null) return;
                            string appId = pair.Key;
                            (DlcType type, string name, string icon) dlcApp = pair.Value;
                            selection.AllDlc[appId] = dlcApp;
                            if (allCheckBox.Checked && dlcApp.name != "Unknown") selection.SelectedDlc[appId] = dlcApp;
                            TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Steam && s.Name == appId) ?? new();
                            dlcNode.Tag = selection.Platform;
                            dlcNode.Name = appId;
                            dlcNode.Text = dlcApp.name;
                            dlcNode.Checked = selection.SelectedDlc.ContainsKey(appId);
                            dlcNode.Remove();
                            _ = programNode.Nodes.Add(dlcNode);
                        }
                    });
                    if (Program.Canceled) return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }
        if (ProgramsToScan.Any(c => c.platform is Platform.Epic))
        {
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                if (Program.Canceled) return;
                if (Program.IsGameBlocked(name, directory) || !ProgramsToScan.Any(c => c.platform is Platform.Epic && c.id == @namespace)) continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled) return;
                    List<string> dllDirectories = await directory.GetDllDirectoriesFromGameDirectory(Platform.Epic);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled) return;
                    ConcurrentDictionary<string, (string name, string product, string icon, string developer)> entitlements = new();
                    List<Task> dlcTasks = new();
                    List<(string id, string name, string product, string icon, string developer)> entitlementIds = await EpicStore.QueryEntitlements(@namespace);
                    if (entitlementIds.Any())
                    {
                        foreach ((string id, string name, string product, string icon, string developer) in entitlementIds)
                        {
                            if (Program.Canceled) return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled) return;
                                entitlements[id] = (name, product, icon, developer);
                                RemoveFromRemainingDLCs(id);
                            });
                            dlcTasks.Add(task);
                        }
                    }
                    if (/*!catalogItems.Any() && */!entitlements.Any())
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled) return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled) return;
                        await task;
                    }

                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Epic, @namespace) ?? new();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked) selection.Koaloader = true;
                    selection.Id = @namespace;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.ExecutableDirectories = await EpicLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Epic;
                    foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements.Where(p => p.Value.name == selection.Name))
                    {
                        selection.ProductUrl = "https://www.epicgames.com/store/product/" + pair.Value.product;
                        selection.IconUrl = pair.Value.icon;
                        selection.Publisher = pair.Value.developer;
                    }

                    if (Program.Canceled) return;
                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace) ?? new();
                        programNode.Tag = selection.Platform;
                        programNode.Name = @namespace;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        _ = selectionTreeView.Nodes.Add(programNode);
                        /*TreeNode catalogItemsNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace + "_catalogItems") ?? new();
                        catalogItemsNode.Tag = selection.Platform;
                        catalogItemsNode.Name = @namespace + "_catalogItems";
                        catalogItemsNode.Text = "Catalog Items";
                        catalogItemsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.CatalogItem);
                        catalogItemsNode.Remove();
                        programNode.Nodes.Add(catalogItemsNode);*/
                        if (entitlements.Any())
                        {
                            /*TreeNode entitlementsNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == @namespace + "_entitlements") ?? new();
                            entitlementsNode.Tag = selection.Platform;
                            entitlementsNode.Name = @namespace + "_entitlements";
                            entitlementsNode.Text = "Entitlements";
                            entitlementsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.Entitlement);
                            entitlementsNode.Remove();
                            programNode.Nodes.Add(entitlementsNode);*/
                            foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements)
                            {
                                if (programNode is null/* || entitlementsNode is null*/) return;
                                string dlcId = pair.Key;
                                (DlcType type, string name, string icon) dlcApp = (DlcType.EpicEntitlement, pair.Value.name, pair.Value.icon);
                                selection.AllDlc[dlcId] = dlcApp;
                                if (allCheckBox.Checked) selection.SelectedDlc[dlcId] = dlcApp;
                                TreeNode dlcNode = treeNodes.Find(s => s.Tag is Platform.Epic && s.Name == dlcId) ?? new();
                                dlcNode.Tag = selection.Platform;
                                dlcNode.Name = dlcId;
                                dlcNode.Text = dlcApp.name;
                                dlcNode.Checked = selection.SelectedDlc.ContainsKey(dlcId);
                                dlcNode.Remove();
                                _ = programNode.Nodes.Add(dlcNode); //entitlementsNode.Nodes.Add(dlcNode);
                            }
                        }
                    });
                    if (Program.Canceled) return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }
        if (ProgramsToScan.Any(c => c.platform is Platform.Ubisoft))
        {
            List<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
            foreach ((string gameId, string name, string gameDirectory) in ubisoftGames)
            {
                if (Program.Canceled) return;
                if (Program.IsGameBlocked(name, gameDirectory) || !ProgramsToScan.Any(c => c.platform is Platform.Ubisoft && c.id == gameId)) continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled) return;
                    List<string> dllDirectories = await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Ubisoft);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled) return;

                    ProgramSelection selection = ProgramSelection.FromPlatformId(Platform.Ubisoft, gameId) ?? new();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any() || selection.ExtraSelectedDlc.Any();
                    if (koaloaderAllCheckBox.Checked) selection.Koaloader = true;
                    selection.Id = gameId;
                    selection.Name = name;
                    selection.RootDirectory = gameDirectory;
                    selection.ExecutableDirectories = await UbisoftLibrary.GetExecutableDirectories(selection.RootDirectory);
                    selection.DllDirectories = dllDirectories;
                    selection.Platform = Platform.Ubisoft;
                    selection.IconUrl = IconGrabber.GetDomainFaviconUrl("store.ubi.com");

                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = treeNodes.Find(s => s.Tag is Platform.Ubisoft && s.Name == gameId) ?? new();
                        programNode.Tag = selection.Platform;
                        programNode.Name = gameId;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        _ = selectionTreeView.Nodes.Add(programNode);
                    });
                    if (Program.Canceled) return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
        }
        foreach (Task task in appTasks)
        {
            if (Program.Canceled) return;
            await task;
        }
        steamGamesToCheck = 0;
    }

    private List<(Platform platform, string id, string name)> ProgramsToScan;
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
        progressLabel.Text = "Waiting for user to select which programs/games to scan . . .";
        ShowProgressBar();

        await ProgramData.Setup();

        bool scan = forceScan;
        if (!scan && (ProgramsToScan is null || !ProgramsToScan.Any() || forceProvideChoices))
        {
            List<(Platform platform, string id, string name, bool alreadySelected)> gameChoices = new();
            if (Directory.Exists(ParadoxLauncher.InstallPath))
                gameChoices.Add((Platform.Paradox, "PL", "Paradox Launcher", ProgramsToScan is not null && ProgramsToScan.Any(p => p.platform is Platform.Paradox && p.id == "PL")));
            if (Directory.Exists(SteamLibrary.InstallPath))
                foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in (await SteamLibrary.GetGames()).Where(g => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                    gameChoices.Add((Platform.Steam, appId, name, ProgramsToScan is not null && ProgramsToScan.Any(p => p.platform is Platform.Steam && p.id == appId)));
            if (Directory.Exists(EpicLibrary.EpicManifestsPath))
                foreach (Manifest manifest in (await EpicLibrary.GetGames()).Where(m => !Program.IsGameBlocked(m.DisplayName, m.InstallLocation)))
                    gameChoices.Add((Platform.Epic, manifest.CatalogNamespace, manifest.DisplayName, ProgramsToScan is not null && ProgramsToScan.Any(p => p.platform is Platform.Epic && p.id == manifest.CatalogNamespace)));
            foreach ((string gameId, string name, string gameDirectory) in (await UbisoftLibrary.GetGames()).Where(g => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                gameChoices.Add((Platform.Ubisoft, gameId, name, ProgramsToScan is not null && ProgramsToScan.Any(p => p.platform is Platform.Ubisoft && p.id == gameId)));
            if (gameChoices.Any())
            {
                using SelectDialogForm form = new(this);
                List<(Platform platform, string id, string name)> choices = form.QueryUser("Choose which programs and/or games to scan for DLC:", gameChoices);
                scan = choices is not null && choices.Any();
                string retry = "\n\nPress the \"Rescan Programs / Games\" button to re-choose.";
                if (scan)
                {
                    ProgramsToScan = choices;
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
            progress.ProgressChanged += (sender, _progress) =>
            {
                if (Program.Canceled) return;
                if (_progress < 0 || _progress > maxProgress) maxProgress = -_progress;
                else curProgress = _progress;
                int p = Math.Max(Math.Min((int)((float)curProgress / maxProgress * 100), 100), 0);
                progressLabel.Text = setup ? $"Setting up SteamCMD . . . {p}%"
                    : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };
            if (Directory.Exists(SteamLibrary.InstallPath) && ProgramsToScan is not null && ProgramsToScan.Any(c => c.platform is Platform.Steam))
            {
                progressLabel.Text = $"Setting up SteamCMD . . . ";
                await SteamCMD.Setup(iProgress);
            }
            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            ProgramSelection.ValidateAll(ProgramsToScan);
            TreeNodes.ForEach(node =>
            {
                if (node.Tag is not Platform platform
                || node.Name is not string platformId
                || ProgramSelection.FromPlatformId(platform, platformId) is null
                && ProgramSelection.GetDlcFromPlatformId(platform, platformId) is null)
                    node.Remove();
            });
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();
        }

        ProgramData.UpdateKoaloaderProxyChoices();

        HideProgressBar();
        selectionTreeView.Enabled = ProgramSelection.All.Any();
        allCheckBox.Enabled = selectionTreeView.Enabled;
        koaloaderAllCheckBox.Enabled = selectionTreeView.Enabled;
        noneFoundLabel.Visible = !selectionTreeView.Enabled;
        installButton.Enabled = ProgramSelection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        cancelButton.Enabled = false;
        scanButton.Enabled = true;
        blockedGamesCheckBox.Enabled = true;
        blockProtectedHelpButton.Enabled = true;
    }

    private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown) return;
        TreeNode node = e.Node;
        if (node is null) return;
        SyncNode(node);
        SyncNodeAncestors(node);
        SyncNodeDescendants(node);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = TreeNodes.TrueForAll(treeNode => treeNode.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        installButton.Enabled = ProgramSelection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
    }

    private static void SyncNodeAncestors(TreeNode node)
    {
        TreeNode parentNode = node.Parent;
        if (parentNode is not null)
        {
            parentNode.Checked = parentNode.Nodes.Cast<TreeNode>().Any(childNode => childNode.Checked);
            SyncNodeAncestors(parentNode);
        }
    }

    private static void SyncNodeDescendants(TreeNode node) =>
        node.Nodes.Cast<TreeNode>().ToList().ForEach(childNode =>
        {
            if (childNode.Text == "Unknown") return;
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
            if (selection is not null)
                selection.ToggleDlc(node.Name, node.Checked);
        }
        else
        {
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, id);
            if (selection is not null)
                selection.Enabled = node.Checked;
        }
    }

    internal List<TreeNode> TreeNodes => GatherTreeNodes(selectionTreeView.Nodes);
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
        programsGroupBox.Size = new(programsGroupBox.Size.Width, programsGroupBox.Size.Height - 3
            - progressLabel.Size.Height
            - progressLabelGames.Size.Height
            - progressLabelDLCs.Size.Height
            - progressBar.Size.Height);
    }
    private void HideProgressBar()
    {
        progressBar.Value = 100;
        progressLabel.Visible = false;
        progressLabelGames.Visible = false;
        progressLabelDLCs.Visible = false;
        progressBar.Visible = false;
        programsGroupBox.Size = new(programsGroupBox.Size.Width, programsGroupBox.Size.Height + 3
            + progressLabel.Size.Height
            + progressLabelGames.Size.Height
            + progressLabelDLCs.Size.Height
            + progressBar.Size.Height);
    }

    internal void OnNodeRightClick(TreeNode node, Point location)
    {
        ContextMenuStrip contextMenuStrip = ContextMenuStrip;
        contextMenuStrip.Items.Clear();
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
        ContextMenuItem header = null;
        if (id == "PL")
            header = new(node.Text, "Paradox Launcher");
        else if (selection is not null)
            header = new(node.Text, (id, selection.IconUrl));
        else if (dlc is not null && dlcParentSelection is not null)
            header = new(node.Text, (id, dlc.Value.app.icon), (id, dlcParentSelection.IconUrl));
        contextMenuStrip.Items.Add(header ?? new ContextMenuItem(node.Text));
        string appInfoVDF = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
        string appInfoJSON = $@"{SteamCMD.AppInfoPath}\{id}.json";
        string cooldown = $@"{ProgramData.CooldownPath}\{id}.txt";
        if ((File.Exists(appInfoVDF) || File.Exists(appInfoJSON)) && (selection is not null || dlc is not null))
        {
            List<ContextMenuItem> queries = new();
            if (File.Exists(appInfoJSON))
            {
                string platformString = (selection is null || selection.Platform is Platform.Steam) ? "Steam Store "
                    : selection.Platform is Platform.Epic ? "Epic GraphQL " : "";
                queries.Add(new ContextMenuItem($"Open {platformString}Query", "Notepad",
                    new EventHandler((sender, e) => Diagnostics.OpenFileInNotepad(appInfoJSON))));
            }
            if (File.Exists(appInfoVDF))
                queries.Add(new ContextMenuItem("Open SteamCMD Query", "Notepad",
                    new EventHandler((sender, e) => Diagnostics.OpenFileInNotepad(appInfoVDF))));
            if (queries.Any())
            {
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                foreach (ContextMenuItem query in queries)
                    contextMenuStrip.Items.Add(query);
                contextMenuStrip.Items.Add(new ContextMenuItem("Refresh Queries", "Command Prompt",
                    new EventHandler((sender, e) =>
                    {
                        try
                        {
                            File.Delete(appInfoVDF);
                        }
                        catch { }
                        try
                        {
                            File.Delete(appInfoJSON);
                        }
                        catch { }
                        try
                        {
                            File.Delete(cooldown);
                        }
                        catch { }
                        OnLoad(forceScan: true);
                    })));
            }
        }
        if (selection is not null)
        {
            if (id == "PL")
            {
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                contextMenuStrip.Items.Add(new ContextMenuItem("Repair", "Command Prompt",
                    new EventHandler(async (sender, e) => await ParadoxLauncher.Repair(this, selection))));
            }
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(new ContextMenuItem("Open Root Directory", "File Explorer",
                new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory))));
            int executables = 0;
            foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories.ToList())
            {
                contextMenuStrip.Items.Add(new ContextMenuItem($"Open Executable Directory #{++executables} ({(binaryType == BinaryType.BIT32 ? "32" : "64")}-bit)", "File Explorer",
                    new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
            }
            List<string> directories = selection.DllDirectories.ToList();
            int steam = 0, epic = 0, r1 = 0, r2 = 0;
            if (selection.Platform is Platform.Steam or Platform.Paradox)
                foreach (string directory in directories)
                {
                    directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string cache);
                    if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config) || File.Exists(cache))
                        contextMenuStrip.Items.Add(new ContextMenuItem($"Open Steamworks Directory #{++steam}", "File Explorer",
                            new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
                }
            if (selection.Platform is Platform.Epic or Platform.Paradox)
                foreach (string directory in directories)
                {
                    directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
                    if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config))
                        contextMenuStrip.Items.Add(new ContextMenuItem($"Open Epic Online Services Directory #{++epic}", "File Explorer",
                            new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
                }
            if (selection.Platform is Platform.Ubisoft)
                foreach (string directory in directories)
                {
                    directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
                    if (File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config))
                        contextMenuStrip.Items.Add(new ContextMenuItem($"Open Uplay R1 Directory #{++r1}", "File Explorer",
                            new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
                    directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config);
                    if (File.Exists(old_api32) || File.Exists(old_api64) || File.Exists(api32) || File.Exists(api32_o) || File.Exists(api64) || File.Exists(api64_o) || File.Exists(config))
                        contextMenuStrip.Items.Add(new ContextMenuItem($"Open Uplay R2 Directory #{++r2}", "File Explorer",
                            new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
                }
        }
        if (id != "PL")
        {
            if (selection is not null && selection.Platform is Platform.Steam
            || dlcParentSelection is not null && dlcParentSelection.Platform is Platform.Steam)
            {
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                contextMenuStrip.Items.Add(new ContextMenuItem("Open SteamDB", "SteamDB",
                    new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://steamdb.info/app/" + id))));
            }
            if (selection is not null)
            {
                if (selection.Platform is Platform.Steam)
                {
                    contextMenuStrip.Items.Add(new ContextMenuItem("Open Steam Store", "Steam Store",
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl))));
                    contextMenuStrip.Items.Add(new ContextMenuItem("Open Steam Community", ("Sub_" + id, selection.SubIconUrl), "Steam Community",
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + id))));
                }
                if (selection.Platform is Platform.Epic)
                {
                    contextMenuStrip.Items.Add(new ToolStripSeparator());
                    contextMenuStrip.Items.Add(new ContextMenuItem("Open ScreamDB", "ScreamDB",
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" + id))));
                    contextMenuStrip.Items.Add(new ContextMenuItem("Open Epic Games Store", "Epic Games",
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl))));
                }
                if (selection.Platform is Platform.Ubisoft)
                {
                    contextMenuStrip.Items.Add(new ToolStripSeparator());
#pragma warning disable CA1308 // Normalize strings to uppercase
                    contextMenuStrip.Items.Add(new ContextMenuItem("Open Ubisoft Store", "Ubisoft Store",
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://store.ubi.com/us/" + selection.Name.Replace(" ", "-").ToLowerInvariant()))));
#pragma warning restore CA1308 // Normalize strings to uppercase
                }
            }
        }
        if (selection is not null && selection.WebsiteUrl is not null)
        {
            contextMenuStrip.Items.Add(new ContextMenuItem("Open Official Website", ("Web_" + id, IconGrabber.GetDomainFaviconUrl(selection.WebsiteUrl)),
                new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser(selection.WebsiteUrl))));
        }
        contextMenuStrip.Show(selectionTreeView, location);
        contextMenuStrip.Refresh();
    }

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
            if (e.HandleException(form: this)) goto retry;
            Close();
        }
    }

    private void OnAccept(bool uninstall = false)
    {
        if (ProgramSelection.All.Any())
        {
            foreach (ProgramSelection selection in ProgramSelection.AllEnabled)
                if (!Program.IsProgramRunningDialog(this, selection)) return;
            if (!uninstall && ParadoxLauncher.DlcDialog(this)) return;
            Hide();
            using InstallForm installForm = new(this, uninstall);
            _ = installForm.ShowDialog();
            if (installForm.Reselecting)
            {
                InheritLocation(installForm);
                Show();
                OnLoad();
            }
            else Close();
        }
    }

    private void OnInstall(object sender, EventArgs e) => OnAccept(false);

    private void OnUninstall(object sender, EventArgs e) => OnAccept(true);

    private void OnScan(object sender, EventArgs e) => OnLoad(forceProvideChoices: true);

    private void OnCancel(object sender, EventArgs e)
    {
        progressLabel.Text = "Cancelling . . . ";
        Program.Cleanup();
    }

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = false;
        foreach (TreeNode node in TreeNodes)
            if (node.Parent is null && !node.Checked)
            {
                shouldCheck = true;
                break;
            }
        foreach (TreeNode node in TreeNodes)
            if (node.Parent is null && node.Checked != shouldCheck)
            {
                node.Checked = shouldCheck;
                OnTreeViewNodeCheckedChanged(null, new(node, TreeViewAction.ByMouse));
            }
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = shouldCheck;
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    internal CheckBox KoaloaderAllCheckBox() => koaloaderAllCheckBox;
    internal void OnKoaloaderAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = false;
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            if (!selection.Koaloader)
            {
                shouldCheck = true;
                break;
            }
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            selection.Koaloader = shouldCheck;
        selectionTreeView.Invalidate();
        koaloaderAllCheckBox.CheckedChanged -= OnKoaloaderAllCheckBoxChanged;
        koaloaderAllCheckBox.Checked = shouldCheck;
        koaloaderAllCheckBox.CheckedChanged += OnKoaloaderAllCheckBoxChanged;
    }

    private void OnBlockProtectedGamesCheckBoxChanged(object sender, EventArgs e)
    {
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        OnLoad(forceProvideChoices: true);
    }

    private readonly string helpButtonListPrefix = "\n    •  ";
    private void OnBlockProtectedGamesHelpButtonClicked(object sender, EventArgs e)
    {
        StringBuilder blockedGames = new();
        foreach (string name in Program.ProtectedGames)
            _ = blockedGames.Append(helpButtonListPrefix + name);
        StringBuilder blockedDirectories = new();
        foreach (string path in Program.ProtectedGameDirectories)
            _ = blockedDirectories.Append(helpButtonListPrefix + path);
        StringBuilder blockedDirectoryExceptions = new();
        foreach (string name in Program.ProtectedGameDirectoryExceptions)
            _ = blockedDirectoryExceptions.Append(helpButtonListPrefix + name);
        using DialogForm form = new(this);
        _ = form.Show(SystemIcons.Information,
            "Blocks the program from caching and displaying games protected by anti-cheats." +
            "\nYou disable this option and install DLC unlockers to protected games at your own risk!" +
            "\n\nBlocked games: " + (string.IsNullOrWhiteSpace(blockedGames.ToString()) ? "(none)" : blockedGames) +
            "\n\nBlocked game sub-directories: " + (string.IsNullOrWhiteSpace(blockedDirectories.ToString()) ? "(none)" : blockedDirectories) +
            "\n\nBlocked game sub-directory exceptions: " + (string.IsNullOrWhiteSpace(blockedDirectoryExceptions.ToString()) ? "(none)" : blockedDirectoryExceptions),
            "OK", customFormText: "Block Protected Games");
    }

    private void OnSortCheckBoxChanged(object sender, EventArgs e) => selectionTreeView.TreeViewNodeSorter
        = sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;
}

#pragma warning restore IDE0058