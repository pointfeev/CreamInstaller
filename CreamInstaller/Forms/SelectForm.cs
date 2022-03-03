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

using CreamInstaller.Epic;
using CreamInstaller.Forms.Components;
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
                    ConcurrentDictionary<string, (string name, string iconStaticId)> dlc = new();
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
                                }
                                if (Program.Canceled) return;
                                if (!string.IsNullOrWhiteSpace(dlcName))
                                    dlc[dlcAppId] = (dlcName, dlcIconStaticId);
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
                    if (allCheckBox.Checked) selection.Enabled = true;
                    selection.Usable = true;
                    selection.Id = appId;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.DllDirectories = dllDirectories;
                    selection.IsSteam = true;
                    selection.AppInfo = appInfo;
                    selection.IconStaticID = appInfo?.Value?.GetChild("common")?.GetChild("icon")?.ToString();
                    selection.ClientIconStaticID = appInfo?.Value?.GetChild("common")?.GetChild("clienticon")?.ToString();

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
                        foreach (KeyValuePair<string, (string name, string iconStaticId)> pair in dlc)
                        {
                            if (Program.Canceled || programNode is null) return;
                            string appId = pair.Key;
                            (string name, string iconStaticId) dlcApp = pair.Value;
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
            Dictionary<string, List<string>> games = new();
            foreach (Manifest manifest in epicGames)
            {
                string id = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                ProgramSelection selection = ProgramSelection.FromId(id);
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
                    ConcurrentDictionary<string, string> dlc = new();
                    List<Task> dlcTasks = new();
                    List<(string id, string name)> dlcIds = await EpicStore.ParseDlcAppIds(id);
                    if (dlcIds.Count > 0)
                    {
                        foreach ((string id, string name) in dlcIds)
                        {
                            if (Program.Canceled) return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled) return;
                                dlc[id] = name;
                                RemoveFromRemainingDLCs(id);
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
                    if (allCheckBox.Checked) selection.Enabled = true;
                    selection.Usable = true;
                    selection.Id = id;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.DllDirectories = dllDirectories;

                    if (Program.Canceled) return;
                    Program.Invoke(selectionTreeView, delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = TreeNodes.Find(s => s.Name == id) ?? new();
                        programNode.Name = id;
                        programNode.Text = name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        selectionTreeView.Nodes.Add(programNode);
                        foreach (KeyValuePair<string, string> pair in dlc)
                        {
                            if (Program.Canceled || programNode is null) return;
                            string dlcId = pair.Key;
                            string dlcName = pair.Value;
                            (string name, string iconStaticId) dlcApp = (dlcName, null); // temporary?
                            selection.AllDlc[dlcId] = dlcApp;
                            if (allCheckBox.Checked) selection.SelectedDlc[dlcId] = dlcApp;
                            TreeNode dlcNode = TreeNodes.Find(s => s.Name == dlcId) ?? new();
                            dlcNode.Name = dlcId;
                            dlcNode.Text = dlcName;
                            dlcNode.Checked = selection.SelectedDlc.ContainsKey(dlcId);
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
                if (!int.TryParse(node.Name, out int appId) || node.Parent is null && ProgramSelection.FromId(node.Name) is null) node.Remove();
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
        if (node is not null)
        {
            string appId = node.Name;
            ProgramSelection selection = ProgramSelection.FromId(appId);
            if (selection is null)
            {
                TreeNode parent = node.Parent;
                if (parent is not null)
                {
                    string gameAppId = parent.Name;
                    ProgramSelection.FromId(gameAppId).ToggleDlc(appId, node.Checked);
                    parent.Checked = parent.Nodes.Cast<TreeNode>().ToList().Any(treeNode => treeNode.Checked);
                }
            }
            else
            {
                if (selection.AllDlc.Any())
                {
                    selection.ToggleAllDlc(node.Checked);
                    node.Nodes.Cast<TreeNode>().ToList().ForEach(treeNode => treeNode.Checked = node.Checked);
                }
                else selection.Enabled = node.Checked;
                allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
                allCheckBox.Checked = TreeNodes.TrueForAll(treeNode => treeNode.Checked);
                allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            }
        }
        installButton.Enabled = ProgramSelection.AllUsableEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
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
        });
        Image Image(string identifier) => images.GetValueOrDefault(identifier, null);
        void TrySetImageAsync(ToolStripMenuItem menuItem, string appId, string iconStaticId, bool client = false) =>
            Task.Run(async () =>
            {
                menuItem.Image = client ? await IconGrabber.GetSteamClientIcon(appId, iconStaticId) : await IconGrabber.GetSteamIcon(appId, iconStaticId);
                images[client ? "ClientIcon_" + appId : "Icon_" + appId] = menuItem.Image;
            });
        selectionTreeView.NodeMouseClick += (sender, e) =>
        {
            TreeNode node = e.Node;
            TreeNode parentNode = node.Parent;
            string id = node.Name;
            ProgramSelection selection = ProgramSelection.FromId(id);
            (string gameAppId, (string name, string iconStaticId) app)? dlc = null;
            if (selection is null) dlc = ProgramSelection.GetDlcFromId(id);
            if (e.Button == MouseButtons.Right && node.Bounds.Contains(e.Location))
            {
                selectionTreeView.SelectedNode = node;
                nodeContextMenu.Items.Clear();
                ToolStripMenuItem header = new(selection?.Name ?? node.Text, Image("Icon_" + id));
                if (header.Image is null)
                {
                    string iconStaticId = dlc?.app.iconStaticId ?? selection?.IconStaticID;
                    if (iconStaticId is not null)
                        TrySetImageAsync(header, id, iconStaticId);
                    else if (dlc is not null)
                    {
                        string gameAppId = dlc.Value.gameAppId;
                        header.Image = Image("Icon_" + gameAppId);
                        ProgramSelection gameSelection = ProgramSelection.FromId(gameAppId);
                        iconStaticId = gameSelection?.IconStaticID;
                        if (header.Image is null && iconStaticId is not null)
                            TrySetImageAsync(header, gameAppId, iconStaticId);
                    }
                }
                nodeContextMenu.Items.Add(header);
                string appInfo = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
                if (Directory.Exists(Directory.GetDirectoryRoot(appInfo)) && File.Exists(appInfo))
                {
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open AppInfo", Image("Notepad"),
                        new EventHandler((sender, e) => Diagnostics.OpenFileInNotepad(appInfo))));
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Refresh AppInfo", Image("Command Prompt"),
                        new EventHandler((sender, e) =>
                        {
                            try
                            {
                                File.Delete(appInfo);
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
                                    await InstallForm.UninstallCreamAPI(directory);
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
                                        + "\n\nAn original Steamworks API or EOS SDK file could not be found."
                                        + "\nYou must reinstall Paradox Launcher to fix this issue.", "OK");
                            })));
                    }
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Root Directory", Image("File Explorer"),
                        new EventHandler((sender, e) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory))));
                    for (int i = 0; i < selection.DllDirectories.Count; i++)
                    {
                        string directory = selection.DllDirectories[i];
                        nodeContextMenu.Items.Add(new ToolStripMenuItem($"Open {(selection.IsSteam ? "Steamworks API" : "EOS SDK")} Directory ({i + 1})", Image("File Explorer"),
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
                            new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://store.steampowered.com/app/" + id))));
                        ToolStripMenuItem steamCommunity = new("Open Steam Community", Image("ClientIcon_" + id),
                        new EventHandler((sender, e) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + id)));
                        nodeContextMenu.Items.Add(steamCommunity);
                        if (steamCommunity.Image is null)
                        {
                            steamCommunity.Image = Image("Steam Community");
                            TrySetImageAsync(steamCommunity, id, selection.ClientIconStaticID, true);
                        }
                    }
                    else
                    {
                        // Epic Games links?
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
