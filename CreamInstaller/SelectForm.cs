using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Components;
using CreamInstaller.Epic;
using CreamInstaller.Paradox;
using CreamInstaller.Resources;
using CreamInstaller.Steam;
using CreamInstaller.Utility;

using Gameloop.Vdf.Linq;

namespace CreamInstaller;

internal partial class SelectForm : CustomForm
{
    internal SelectForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
    }

    private static void UpdateRemaining(Label label, List<string> list, string descriptor) =>
        label.Text = list.Any() ? $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list).Replace("&", "&&") : "";

    private readonly List<string> RemainingGames = new();
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
            if (RemainingGames.Contains(gameName))
                RemainingGames.Remove(gameName);
            UpdateRemainingGames();
        });
    }

    private readonly List<string> RemainingDLCs = new();
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
            if (RemainingDLCs.Contains(dlcId))
                RemainingDLCs.Remove(dlcId);
            UpdateRemainingDLCs();
        });
    }

    internal readonly List<Task> RunningTasks = new();
    private async Task GetApplicablePrograms(IProgress<int> progress)
    {
        if (Program.Canceled) return;
        int CompleteTasks = 0;
        RunningTasks.Clear(); // contains all running tasks including games AND their dlc
        RemainingGames.Clear(); // for display purposes only, otherwise ignorable
        RemainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        if (Directory.Exists(ParadoxLauncher.InstallPath))
        {
            ProgramSelection selection = ProgramSelection.FromId("ParadoxLauncher");
            selection ??= new();
            if (allCheckBox.Checked) selection.Enabled = true;
            selection.Usable = true;
            selection.Id = "ParadoxLauncher";
            selection.Name = "Paradox Launcher";
            selection.RootDirectory = ParadoxLauncher.InstallPath;
            List<string> steamDllDirectories = await SteamLibrary.GetDllDirectoriesFromGameDirectory(selection.RootDirectory);
            selection.DllDirectories = steamDllDirectories ?? await EpicLibrary.GetDllDirectoriesFromGameDirectory(selection.RootDirectory);
            selection.IsSteam = steamDllDirectories is not null;

            TreeNode programNode = TreeNodes.Find(s => s.Name == selection.Id) ?? new();
            programNode.Name = selection.Id;
            programNode.Text = selection.Name;
            programNode.Checked = selection.Enabled;
            programNode.Remove();
            selectionTreeView.Nodes.Add(programNode);
        }
        if (Directory.Exists(SteamLibrary.InstallPath))
        {
            List<Tuple<string, string, string, int, string>> steamGames = await SteamLibrary.GetGames();
            foreach (Tuple<string, string, string, int, string> program in steamGames)
            {
                string appId = program.Item1;
                string name = program.Item2;
                string branch = program.Item3;
                int buildId = program.Item4;
                string directory = program.Item5;
                ProgramSelection selection = ProgramSelection.FromId(appId);
                if (Program.Canceled) return;
                if (Program.IsGameBlocked(name, directory)) continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled) return;
                    List<string> dllDirectories = await SteamLibrary.GetDllDirectoriesFromGameDirectory(directory);
                    if (dllDirectories is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    VProperty appInfo = appInfo = await SteamCMD.GetAppInfo(appId, branch, buildId);
                    if (appInfo is null)
                    {
                        RemoveFromRemainingGames(name);
                        return;
                    }
                    if (Program.Canceled) return;
                    ConcurrentDictionary<string, (DlcType type, string name, string icon)> dlc = new();
                    List<Task> dlcTasks = new();
                    List<string> dlcIds = await SteamCMD.ParseDlcAppIds(appInfo);
                    await SteamStore.ParseDlcAppIds(appId, dlcIds);
                    if (dlcIds.Count > 0)
                    {
                        foreach (string dlcAppId in dlcIds)
                        {
                            if (Program.Canceled) return;
                            AddToRemainingDLCs(dlcAppId);
                            Task task = Task.Run(async () =>
                            {
                                if (Program.Canceled) return;
                                string dlcName = null;
                                string dlcIconStaticId = null;
                                VProperty dlcAppInfo = await SteamCMD.GetAppInfo(dlcAppId);
                                if (dlcAppInfo is not null)
                                {
                                    dlcName = dlcAppInfo.Value?.GetChild("common")?.GetChild("name")?.ToString();
                                    dlcIconStaticId = dlcAppInfo.Value?.GetChild("common")?.GetChild("icon")?.ToString();
                                    dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo_small")?.ToString();
                                    dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo")?.ToString();
                                    if (dlcIconStaticId is not null)
                                        dlcIconStaticId = IconGrabber.SteamAppImagesPath + @$"\{dlcAppId}\{dlcIconStaticId}.jpg";
                                }
                                if (Program.Canceled) return;
                                if (!string.IsNullOrWhiteSpace(dlcName))
                                    dlc[dlcAppId] = (DlcType.Default, dlcName, dlcIconStaticId);
                                RemoveFromRemainingDLCs(dlcAppId);
                                progress.Report(++CompleteTasks);
                            });
                            dlcTasks.Add(task);
                            RunningTasks.Add(task);
                            progress.Report(-RunningTasks.Count);
                            Thread.Sleep(10); // to reduce control & window freezing
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

                    selection ??= new();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any();
                    selection.Usable = true;
                    selection.Id = appId;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.DllDirectories = dllDirectories;
                    selection.IsSteam = true;
                    selection.AppInfo = appInfo;
                    selection.ProductUrl = "https://store.steampowered.com/app/" + appId;
                    selection.IconUrl = IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value?.GetChild("common")?.GetChild("icon")?.ToString()}.jpg";
                    selection.ClientIconUrl = IconGrabber.SteamAppImagesPath + @$"\{appId}\{appInfo?.Value?.GetChild("common")?.GetChild("clienticon")?.ToString()}.ico";
                    selection.Publisher = appInfo?.Value?.GetChild("extended")?.GetChild("publisher")?.ToString();

                    if (Program.Canceled) return;
                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = TreeNodes.Find(s => s.Name == appId) ?? new();
                        programNode.Name = appId;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        selectionTreeView.Nodes.Add(programNode);
                        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in dlc)
                        {
                            if (Program.Canceled || programNode is null) return;
                            string appId = pair.Key;
                            (DlcType type, string name, string icon) dlcApp = pair.Value;
                            selection.AllDlc[appId] = dlcApp;
                            if (allCheckBox.Checked) selection.SelectedDlc[appId] = dlcApp;
                            TreeNode dlcNode = TreeNodes.Find(s => s.Name == appId) ?? new();
                            dlcNode.Name = appId;
                            dlcNode.Text = dlcApp.name;
                            dlcNode.Checked = selection.SelectedDlc.ContainsKey(appId);
                            dlcNode.Remove();
                            programNode.Nodes.Add(dlcNode);
                        }
                    });
                    if (Program.Canceled) return;
                    RemoveFromRemainingGames(name);
                    progress.Report(++CompleteTasks);
                });
                appTasks.Add(task);
                RunningTasks.Add(task);
                progress.Report(-RunningTasks.Count);
            }
        }
        if (Directory.Exists(EpicLibrary.EpicAppDataPath))
        {
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                ProgramSelection selection = ProgramSelection.FromId(@namespace);
                if (Program.Canceled) return;
                if (Program.IsGameBlocked(name, directory)) continue;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled) return;
                    List<string> dllDirectories = await EpicLibrary.GetDllDirectoriesFromGameDirectory(directory);
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
                                progress.Report(++CompleteTasks);
                            });
                            dlcTasks.Add(task);
                            RunningTasks.Add(task);
                            progress.Report(-RunningTasks.Count);
                            Thread.Sleep(10); // to reduce control & window freezing
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

                    selection ??= new();
                    selection.Enabled = allCheckBox.Checked || selection.SelectedDlc.Any();
                    selection.Usable = true;
                    selection.Id = @namespace;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.DllDirectories = dllDirectories;
                    foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements)
                        if (pair.Value.name == selection.Name)
                        {
                            selection.ProductUrl = "https://www.epicgames.com/store/product/" + pair.Value.product;
                            selection.IconUrl = pair.Value.icon;
                            selection.Publisher = pair.Value.developer;
                        }

                    if (Program.Canceled) return;
                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = TreeNodes.Find(s => s.Name == @namespace) ?? new();
                        programNode.Name = @namespace;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        selectionTreeView.Nodes.Add(programNode);
                        /*TreeNode catalogItemsNode = TreeNodes.Find(s => s.Name == @namespace + "_catalogItems") ?? new();
                        catalogItemsNode.Name = @namespace + "_catalogItems";
                        catalogItemsNode.Text = "Catalog Items";
                        catalogItemsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.CatalogItem);
                        catalogItemsNode.Remove();
                        programNode.Nodes.Add(catalogItemsNode);*/
                        if (entitlements.Any())
                        {
                            /*TreeNode entitlementsNode = TreeNodes.Find(s => s.Name == @namespace + "_entitlements") ?? new();
                            entitlementsNode.Name = @namespace + "_entitlements";
                            entitlementsNode.Text = "Entitlements";
                            entitlementsNode.Checked = selection.SelectedDlc.Any(pair => pair.Value.type == DlcType.Entitlement);
                            entitlementsNode.Remove();
                            programNode.Nodes.Add(entitlementsNode);*/
                            foreach (KeyValuePair<string, (string name, string product, string icon, string developer)> pair in entitlements)
                            {
                                if (Program.Canceled || programNode is null/* || entitlementsNode is null*/) return;
                                string dlcId = pair.Key;
                                (DlcType type, string name, string icon) dlcApp = (DlcType.Entitlement, pair.Value.name, pair.Value.icon);
                                selection.AllDlc[dlcId] = dlcApp;
                                if (allCheckBox.Checked) selection.SelectedDlc[dlcId] = dlcApp;
                                TreeNode dlcNode = TreeNodes.Find(s => s.Name == dlcId) ?? new();
                                dlcNode.Name = dlcId;
                                dlcNode.Text = dlcApp.name;
                                dlcNode.Checked = selection.SelectedDlc.ContainsKey(dlcId);
                                dlcNode.Remove();
                                programNode.Nodes.Add(dlcNode); //entitlementsNode.Nodes.Add(dlcNode);
                            }
                        }
                    });
                    if (Program.Canceled) return;
                    RemoveFromRemainingGames(name);
                    progress.Report(++CompleteTasks);
                });
                appTasks.Add(task);
                RunningTasks.Add(task);
                progress.Report(-RunningTasks.Count);
            }
        }
        foreach (Task task in appTasks)
        {
            if (Program.Canceled) return;
            await task;
        }
        progress.Report(RunningTasks.Count);
    }

    private async void OnLoad()
    {
    retry:
        try
        {
            Program.Canceled = false;
            blockedGamesCheckBox.Enabled = false;
            blockProtectedHelpButton.Enabled = false;
            cancelButton.Enabled = true;
            scanButton.Enabled = false;
            noneFoundLabel.Visible = false;
            allCheckBox.Enabled = false;
            installButton.Enabled = false;
            uninstallButton.Enabled = installButton.Enabled;
            selectionTreeView.Enabled = false;
            ShowProgressBar();

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
                int p = Math.Max(Math.Min((int)((float)(curProgress / (float)maxProgress) * 100), 100), 0);
                progressLabel.Text = setup ? $"Setting up SteamCMD . . . {p}%"
                    : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };

            await ProgramData.Setup();
            if (Directory.Exists(SteamLibrary.InstallPath))
            {
                progressLabel.Text = $"Setting up SteamCMD . . . ";
                await SteamCMD.Setup(iProgress);
            }
            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            ProgramSelection.ValidateAll();
            TreeNodes.ForEach(node =>
            {
                if (node.Parent is null && ProgramSelection.FromId(node.Name) is null)
                    node.Remove();
            });
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();

            HideProgressBar();
            selectionTreeView.Enabled = ProgramSelection.All.Any();
            allCheckBox.Enabled = selectionTreeView.Enabled;
            noneFoundLabel.Visible = !selectionTreeView.Enabled;
            installButton.Enabled = ProgramSelection.AllUsableEnabled.Any();
            uninstallButton.Enabled = installButton.Enabled;
            cancelButton.Enabled = false;
            scanButton.Enabled = true;
            blockedGamesCheckBox.Enabled = true;
            blockProtectedHelpButton.Enabled = true;
        }
        catch (Exception e)
        {
            if (ExceptionHandler.OutputException(e)) goto retry;
            Close();
        }
    }

    private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown) return;
        TreeNode node = e.Node;
        if (node is null) return;
        CheckNode(node);
        SyncNodeParents(node);
        SyncNodeDescendants(node);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = TreeNodes.TrueForAll(treeNode => treeNode.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        installButton.Enabled = ProgramSelection.AllUsableEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
    }

    private static void SyncNodeParents(TreeNode node)
    {
        TreeNode parentNode = node.Parent;
        if (parentNode is not null)
        {
            parentNode.Checked = parentNode.Nodes.Cast<TreeNode>().ToList().Any(childNode => childNode.Checked);
            SyncNodeParents(parentNode);
        }
    }

    private static void SyncNodeDescendants(TreeNode node) =>
        node.Nodes.Cast<TreeNode>().ToList().ForEach(childNode =>
        {
            childNode.Checked = node.Checked;
            CheckNode(childNode);
            SyncNodeDescendants(childNode);
        });

    private static void CheckNode(TreeNode node)
    {
        (string gameId, (DlcType type, string name, string icon) app)? dlc = ProgramSelection.GetDlcFromId(node.Name);
        if (dlc.HasValue)
        {
            (string gameId, _) = dlc.Value;
            ProgramSelection selection = ProgramSelection.FromId(gameId);
            if (selection is not null)
                selection.ToggleDlc(node.Name, node.Checked);
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

    private class TreeNodeSorter : IComparer
    {
        public int Compare(object a, object b)
        {
            string aId = (a as TreeNode).Name;
            string bId = (b as TreeNode).Name;
            return aId == "ParadoxLauncher" ? -1
                : bId == "ParadoxLauncher" ? 1
                : !int.TryParse(aId, out _) && !int.TryParse(bId, out _) ? string.Compare(aId, bId)
                : !int.TryParse(aId, out int A) ? 1
                : !int.TryParse(bId, out int B) ? -1
                : A > B ? 1
                : A < B ? -1
                : 0;
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
        groupBox1.Size = new(groupBox1.Size.Width, groupBox1.Size.Height - 3
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
        groupBox1.Size = new(groupBox1.Size.Width, groupBox1.Size.Height + 3
            + progressLabel.Size.Height
            + progressLabelGames.Size.Height
            + progressLabelDLCs.Size.Height
            + progressBar.Size.Height);
    }

    private void OnLoad(object sender, EventArgs _)
    {
        HideProgressBar();
        selectionTreeView.TreeViewNodeSorter = new TreeNodeSorter();
        selectionTreeView.AfterCheck += OnTreeViewNodeCheckedChanged;
        Dictionary<string, Image> images = new();
        Task.Run(async () =>
        {
            if (Directory.Exists(ParadoxLauncher.InstallPath))
            {
                foreach (string file in Directory.GetFiles(ParadoxLauncher.InstallPath, "*.exe"))
                {
                    images["Icon_ParadoxLauncher"] = IconGrabber.GetFileIconImage(file);
                    break;
                }
            }
            images["Notepad"] = IconGrabber.GetNotepadImage();
            images["Command Prompt"] = IconGrabber.GetCommandPromptImage();
            images["File Explorer"] = IconGrabber.GetFileExplorerImage();
            images["SteamDB"] = await HttpClientManager.GetImageFromUrl("https://steamdb.info/favicon.ico");
            images["Steam Store"] = await HttpClientManager.GetImageFromUrl("https://store.steampowered.com/favicon.ico");
            images["Steam Community"] = await HttpClientManager.GetImageFromUrl("https://steamcommunity.com/favicon.ico");
            images["ScreamDB"] = await HttpClientManager.GetImageFromUrl("https://scream-db.web.app/favicon.ico");
            images["Epic Games"] = await HttpClientManager.GetImageFromUrl("https://www.epicgames.com/favicon.ico");
        });
        Image Image(string identifier) => images.GetValueOrDefault(identifier, null);
        void TrySetImageAsync(ToolStripMenuItem menuItem, string appId, string iconUrl, bool client = false) =>
            Task.Run(async () =>
            {
                menuItem.Image = await HttpClientManager.GetImageFromUrl(iconUrl);
                images[client ? "ClientIcon_" + appId : "Icon_" + appId] = menuItem.Image;
            });
        selectionTreeView.NodeMouseClick += (sender, e) =>
        {
            TreeNode node = e.Node;
            if (!node.Bounds.Contains(e.Location) || e.Button != MouseButtons.Right) return;
            selectionTreeView.SelectedNode = node;
            string id = node.Name;
            ProgramSelection selection = ProgramSelection.FromId(id);
            (string gameAppId, (DlcType type, string name, string icon) app)? dlc = null;
            if (selection is null) dlc = ProgramSelection.GetDlcFromId(id);
            if (selection is not null || dlc is not null)
            {
                nodeContextMenu.Items.Clear();
                ToolStripMenuItem header = new(selection?.Name ?? node.Text, Image("Icon_" + id));
                if (header.Image is null)
                {
                    string icon = dlc?.app.icon ?? selection?.IconUrl;
                    if (icon is not null)
                        TrySetImageAsync(header, id, icon);
                    else if (dlc is not null)
                    {
                        string gameAppId = dlc.Value.gameAppId;
                        header.Image = Image("Icon_" + gameAppId);
                        ProgramSelection gameSelection = ProgramSelection.FromId(gameAppId);
                        icon = gameSelection?.IconUrl;
                        if (header.Image is null && icon is not null)
                            TrySetImageAsync(header, gameAppId, icon);
                    }
                }
                nodeContextMenu.Items.Add(header);
                string appInfo = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
                string appInfoEpic = $@"{SteamCMD.AppInfoPath}\{id}.json";
                if (Directory.Exists(Directory.GetDirectoryRoot(appInfo)) && (File.Exists(appInfo) || File.Exists(appInfoEpic)))
                {
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open AppInfo", Image("Notepad"),
                        new EventHandler((sender, e) =>
                        {
                            if (File.Exists(appInfo))
                                Diagnostics.OpenFileInNotepad(appInfo);
                            else
                                Diagnostics.OpenFileInNotepad(appInfoEpic);
                        })));
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Refresh AppInfo", Image("Command Prompt"),
                        new EventHandler((sender, e) =>
                        {
                            try
                            {
                                File.Delete(appInfo);
                            }
                            catch { }
                            try
                            {
                                File.Delete(appInfoEpic);
                            }
                            catch { }
                            OnLoad();
                        })));
                }
                if (selection is not null)
                {
                    if (id == "ParadoxLauncher")
                    {
                        nodeContextMenu.Items.Add(new ToolStripSeparator());
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Repair", Image("Command Prompt"),
                            new EventHandler(async (sender, e) =>
                            {
                                if (!Program.IsProgramRunningDialog(this, selection)) return;

                                byte[] cApiIni = null;
                                byte[] properApi = null;
                                byte[] properApi64 = null;

                                byte[] sApiJson = null;
                                byte[] properSdk = null;
                                byte[] properSdk64 = null;

                                foreach (string directory in selection.DllDirectories)
                                {
                                    directory.GetCreamApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                                    if (cApiIni is null && File.Exists(cApi))
                                        cApiIni = File.ReadAllBytes(cApi);
                                    await InstallForm.UninstallCreamAPI(directory);
                                    if (properApi is null && File.Exists(api) && !Properties.Resources.API.EqualsFile(api))
                                        properApi = File.ReadAllBytes(api);
                                    if (properApi64 is null && File.Exists(api64) && !Properties.Resources.API64.EqualsFile(api64))
                                        properApi64 = File.ReadAllBytes(api64);

                                    directory.GetScreamApiComponents(out string sdk, out string sdk_o, out string sdk64, out string sdk64_o, out string sApi);
                                    if (sApiJson is null && File.Exists(sApi))
                                        sApiJson = File.ReadAllBytes(sApi);
                                    await InstallForm.UninstallScreamAPI(directory);
                                    if (properSdk is null && File.Exists(sdk) && !Properties.Resources.SDK.EqualsFile(sdk))
                                        properSdk = File.ReadAllBytes(sdk);
                                    if (properSdk64 is null && File.Exists(sdk64) && !Properties.Resources.SDK64.EqualsFile(sdk64))
                                        properSdk64 = File.ReadAllBytes(sdk64);
                                }
                                if (properApi is not null || properApi64 is not null || properSdk is not null || properSdk64 is not null)
                                {
                                    bool neededRepair = false;
                                    foreach (string directory in selection.DllDirectories)
                                    {
                                        directory.GetCreamApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                                        if (properApi is not null && Properties.Resources.API.EqualsFile(api))
                                        {
                                            properApi.Write(api);
                                            neededRepair = true;
                                        }
                                        if (properApi64 is not null && Properties.Resources.API64.EqualsFile(api64))
                                        {
                                            properApi64.Write(api64);
                                            neededRepair = true;
                                        }
                                        if (cApiIni is not null)
                                        {
                                            await InstallForm.InstallCreamAPI(directory, selection);
                                            cApiIni.Write(cApi);
                                        }

                                        directory.GetScreamApiComponents(out string sdk, out string sdk_o, out string sdk64, out string sdk64_o, out string sApi);
                                        if (properSdk is not null && Properties.Resources.SDK.EqualsFile(sdk))
                                        {
                                            properSdk.Write(sdk);
                                            neededRepair = true;
                                        }
                                        if (properSdk64 is not null && Properties.Resources.SDK64.EqualsFile(sdk64))
                                        {
                                            properSdk64.Write(sdk64);
                                            neededRepair = true;
                                        }
                                        if (sApiJson is not null)
                                        {
                                            await InstallForm.InstallScreamAPI(directory, selection);
                                            sApiJson.Write(sApi);
                                        }
                                    }
                                    if (neededRepair)
                                        new DialogForm(this).Show(Icon, "Paradox Launcher successfully repaired!", "OK");
                                    else
                                        new DialogForm(this).Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.", "OK");
                                }
                                else
                                    new DialogForm(this).Show(SystemIcons.Error, "Paradox Launcher repair failed!"
                                        + "\n\nAn original Steamworks API or Epic Online Services SDK file could not be found."
                                        + "\nYou must reinstall Paradox Launcher to fix this issue.", "OK");
                            })));
                    }
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Root Directory", Image("File Explorer"),
                        new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory))));
                    for (int i = 0; i < selection.DllDirectories.Count; i++)
                    {
                        string directory = selection.DllDirectories[i];
                        nodeContextMenu.Items.Add(new ToolStripMenuItem($"Open {(selection.IsSteam ? "Steamworks API" : "Epic Online Services SDK")} Directory ({i + 1})", Image("File Explorer"),
                            new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(directory))));
                    }
                }
                if (id != "ParadoxLauncher" && selection is not null)
                {
                    if (selection.IsSteam)
                    {
                        nodeContextMenu.Items.Add(new ToolStripSeparator());
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Open SteamDB", Image("SteamDB"),
                            new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://steamdb.info/app/" + id))));
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Steam Store", Image("Steam Store"),
                            new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl))));
                        ToolStripMenuItem steamCommunity = new("Open Steam Community", Image("ClientIcon_" + id),
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + id)));
                        nodeContextMenu.Items.Add(steamCommunity);
                        if (steamCommunity.Image is null)
                        {
                            steamCommunity.Image = Image("Steam Community");
                            TrySetImageAsync(steamCommunity, id, selection.ClientIconUrl, true);
                        }
                    }
                    else
                    {
                        nodeContextMenu.Items.Add(new ToolStripSeparator());
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Open ScreamDB", Image("ScreamDB"),
                            new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" + id))));
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Epic Games Store", Image("Epic Games"),
                            new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser(selection.ProductUrl))));
                    }
                }
                nodeContextMenu.Show(selectionTreeView, e.Location);
            }
        };
        OnLoad();
    }

    private void OnAccept(bool uninstall = false)
    {
        if (ProgramSelection.All.Any())
        {
            foreach (ProgramSelection selection in ProgramSelection.AllUsableEnabled)
                if (!Program.IsProgramRunningDialog(this, selection)) return;
            if (ParadoxLauncher.DlcDialog(this)) return;
            Hide();
            InstallForm installForm = new(this, uninstall);
            installForm.ShowDialog();
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

    private void OnScan(object sender, EventArgs e) => OnLoad();

    private void OnCancel(object sender, EventArgs e)
    {
        progressLabel.Text = "Cancelling . . . ";
        Program.Cleanup();
    }

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = false;
        TreeNodes.ForEach(node =>
        {
            if (node.Parent is null)
            {
                if (!node.Checked) shouldCheck = true;
                if (node.Checked != shouldCheck)
                {
                    node.Checked = shouldCheck;
                    OnTreeViewNodeCheckedChanged(null, new(node, TreeViewAction.ByMouse));
                }
            }
        });
        allCheckBox.Checked = shouldCheck;
    }

    private void OnBlockProtectedGamesCheckBoxChanged(object sender, EventArgs e)
    {
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        OnLoad();
    }

    private readonly string helpButtonListPrefix = "\n    •  ";
    private void OnBlockProtectedGamesHelpButtonClicked(object sender, EventArgs e)
    {
        string blockedGames = "";
        foreach (string name in Program.ProtectedGameNames)
            blockedGames += helpButtonListPrefix + name;
        string blockedDirectories = "";
        foreach (string path in Program.ProtectedGameDirectories)
            blockedDirectories += helpButtonListPrefix + path;
        string blockedDirectoryExceptions = "";
        foreach (string name in Program.ProtectedGameDirectoryExceptions)
            blockedDirectoryExceptions += helpButtonListPrefix + name;
        new DialogForm(this).Show(SystemIcons.Information,
            "Blocks the program from caching and displaying games protected by DLL checks," +
            "\nanti-cheats, or that are confirmed not to be working with CreamAPI or ScreamAPI." +
            "\n\nBlocked game names:" + blockedGames +
            "\n\nBlocked game sub-directories:" + blockedDirectories +
            "\n\nBlocked game sub-directory exceptions (not blocked):" + blockedDirectoryExceptions,
            "OK");
    }
}
