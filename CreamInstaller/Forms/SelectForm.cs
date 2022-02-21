using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Classes;
using CreamInstaller.Forms.Components;
using CreamInstaller.Resources;

using Gameloop.Vdf.Linq;

namespace CreamInstaller;

internal partial class SelectForm : CustomForm
{
    internal SelectForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
        Program.SelectForm = this;
    }

    private static async Task<List<string>> GameLibraryDirectories() => await Task.Run(() =>
    {
        List<string> gameDirectories = new();
        if (Program.Canceled) return gameDirectories;
        string steamInstallPath = Program.SteamInstallPath;
        if (steamInstallPath != null && Directory.Exists(steamInstallPath))
        {
            string libraryFolder = steamInstallPath + @"\steamapps";
            if (Directory.Exists(libraryFolder))
            {
                gameDirectories.Add(libraryFolder);
                string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
                if (File.Exists(libraryFolders) && ValveDataFile.TryDeserialize(File.ReadAllText(libraryFolders, Encoding.UTF8), out VProperty result))
                {
                    foreach (VProperty property in result.Value)
                        if (int.TryParse(property.Key, out int _))
                        {
                            string path = property.Value.GetChild("path")?.ToString();
                            if (string.IsNullOrWhiteSpace(path)) continue;
                            path += @"\steamapps";
                            if (Directory.Exists(path) && !gameDirectories.Contains(path)) gameDirectories.Add(path);
                        }
                }
            }
        }
        return gameDirectories;
    });

    private static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
    {
        List<string> dllDirectories = new();
        if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
        gameDirectory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
        if (File.Exists(api)
            || File.Exists(api_o)
            || File.Exists(api64)
            || File.Exists(api64_o)
            || File.Exists(cApi))
            dllDirectories.Add(gameDirectory);
        string[] directories = Directory.GetDirectories(gameDirectory);
        foreach (string _directory in directories)
        {
            if (Program.Canceled) return null;
            try
            {
                List<string> moreDllDirectories = await GetDllDirectoriesFromGameDirectory(_directory);
                if (moreDllDirectories is not null) dllDirectories.AddRange(moreDllDirectories);
            }
            catch { }
        }
        return !dllDirectories.Any() ? null : dllDirectories;
    });

    private static async Task<List<Tuple<int, string, string, int, string>>> GetGamesFromLibraryDirectory(string libraryDirectory) => await Task.Run(() =>
    {
        List<Tuple<int, string, string, int, string>> games = new();
        if (Program.Canceled || !Directory.Exists(libraryDirectory)) return null;
        string[] files = Directory.GetFiles(libraryDirectory);
        foreach (string file in files)
        {
            if (Program.Canceled) return null;
            if (Path.GetExtension(file) == ".acf" && ValveDataFile.TryDeserialize(File.ReadAllText(file, Encoding.UTF8), out VProperty result))
            {
                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId)
                    || string.IsNullOrWhiteSpace(installdir)
                    || string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(buildId))
                    continue;
                string branch = result.Value.GetChild("UserConfig")?.GetChild("betakey")?.ToString();
                if (string.IsNullOrWhiteSpace(branch)) branch = "public";
                string gameDirectory = libraryDirectory + @"\common\" + installdir;
                if (!int.TryParse(appId, out int appIdInt)) continue;
                if (!int.TryParse(buildId, out int buildIdInt)) continue;
                games.Add(new(appIdInt, name, branch, buildIdInt, gameDirectory));
            }
        }
        return !games.Any() ? null : games;
    });

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
    private async Task GetCreamApiApplicablePrograms(IProgress<int> progress)
    {
        if (Program.Canceled) return;
        List<Tuple<int, string, string, int, string>> applicablePrograms = new();
        if (Directory.Exists(Program.ParadoxLauncherInstallPath))
            applicablePrograms.Add(new(0, "Paradox Launcher", "", 0, Program.ParadoxLauncherInstallPath));
        List<string> gameLibraryDirectories = await GameLibraryDirectories();
        foreach (string libraryDirectory in gameLibraryDirectories)
        {
            List<Tuple<int, string, string, int, string>> games = await GetGamesFromLibraryDirectory(libraryDirectory);
            if (games is not null)
                foreach (Tuple<int, string, string, int, string> game in games)
                    if (!applicablePrograms.Any(_game => _game.Item1 == game.Item1))
                        applicablePrograms.Add(game);
        }

        int CompleteTasks = 0;
        RunningTasks.Clear(); // contains all running tasks including games AND their dlc
        RemainingGames.Clear(); // for display purposes only, otherwise ignorable
        RemainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        List<Task> appTasks = new();
        foreach (Tuple<int, string, string, int, string> program in applicablePrograms)
        {
            int appId = program.Item1;
            string name = program.Item2;
            string branch = program.Item3;
            int buildId = program.Item4;
            string directory = program.Item5;
            ProgramSelection selection = ProgramSelection.FromAppId(appId);
            if (Program.Canceled) return;
            if (Program.IsGameBlocked(name, directory)) continue;
            AddToRemainingGames(name);
            Task task = Task.Run(async () =>
            {
                if (Program.Canceled) return;
                List<string> dllDirectories = await GetDllDirectoriesFromGameDirectory(directory);
                if (dllDirectories is null)
                {
                    RemoveFromRemainingGames(name);
                    return;
                }
                VProperty appInfo = null;
                if (appId > 0) appInfo = await SteamCMD.GetAppInfo(appId, branch, buildId);
                if (appId > 0 && appInfo is null)
                {
                    RemoveFromRemainingGames(name);
                    return;
                }
                if (Program.Canceled) return;
                ConcurrentDictionary<int, (string name, string iconStaticId)> dlc = new();
                List<Task> dlcTasks = new();
                List<int> dlcIds = await SteamCMD.ParseDlcAppIds(appInfo);
                if (dlcIds.Count > 0)
                {
                    foreach (int id in dlcIds)
                    {
                        if (Program.Canceled) return;
                        AddToRemainingDLCs(id.ToString());
                        Task task = Task.Run(async () =>
                        {
                            if (Program.Canceled) return;
                            string dlcName = null;
                            string dlcIconStaticId = null;
                            VProperty dlcAppInfo = await SteamCMD.GetAppInfo(id);
                            if (dlcAppInfo is not null)
                            {
                                dlcName = dlcAppInfo.Value?.GetChild("common")?.GetChild("name")?.ToString();
                                dlcIconStaticId = dlcAppInfo.Value?.GetChild("common")?.GetChild("icon")?.ToString();
                                dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo_small")?.ToString();
                                dlcIconStaticId ??= dlcAppInfo.Value?.GetChild("common")?.GetChild("logo")?.ToString();
                            }
                            if (Program.Canceled) return;
                            if (!string.IsNullOrWhiteSpace(dlcName))
                                dlc[id] = (dlcName, dlcIconStaticId);
                            RemoveFromRemainingDLCs(id.ToString());
                            progress.Report(++CompleteTasks);
                        });
                        dlcTasks.Add(task);
                        RunningTasks.Add(task);
                        progress.Report(-RunningTasks.Count);
                        Thread.Sleep(10); // to reduce control & window freezing
                    }
                }
                else if (appId > 0)
                {
                    RemoveFromRemainingGames(name);
                    return;
                }
                if (Program.Canceled) return;
                if (string.IsNullOrWhiteSpace(name))
                {
                    RemoveFromRemainingGames(name);
                    return;
                }
                foreach (Task task in dlcTasks)
                {
                    if (Program.Canceled) return;
                    await task;
                }

                selection ??= new();
                selection.Usable = true;
                selection.SteamAppId = appId;
                selection.Name = name;
                selection.RootDirectory = directory;
                selection.SteamApiDllDirectories = dllDirectories;
                selection.AppInfo = appInfo;
                selection.IconStaticID = appInfo?.Value?.GetChild("common")?.GetChild("icon")?.ToString();
                selection.ClientIconStaticID = appInfo?.Value?.GetChild("common")?.GetChild("clienticon")?.ToString();
                if (allCheckBox.Checked) selection.Enabled = true;

                if (Program.Canceled) return;
                Program.Invoke(selectionTreeView, delegate
                {
                    if (Program.Canceled) return;
                    TreeNode programNode = TreeNodes.Find(s => s.Name == "" + appId) ?? new();
                    programNode.Name = "" + appId;
                    programNode.Text = /*(appId > 0 ? $"[{appId}] " : "") +*/ name;
                    programNode.Checked = selection.Enabled;
                    programNode.Remove();
                    selectionTreeView.Nodes.Add(programNode);
                    if (appId == 0) // paradox launcher
                    {
                        // maybe add game and/or dlc choice here?
                    }
                    else
                    {
                        foreach (KeyValuePair<int, (string name, string iconStaticId)> pair in dlc)
                        {
                            if (Program.Canceled || programNode is null) return;
                            int appId = pair.Key;
                            (string name, string iconStaticId) dlcApp = pair.Value;
                            selection.AllSteamDlc[appId] = dlcApp;
                            if (allCheckBox.Checked) selection.SelectedSteamDlc[appId] = dlcApp;
                            TreeNode dlcNode = TreeNodes.Find(s => s.Name == "" + appId) ?? new();
                            dlcNode.Name = appId.ToString();
                            dlcNode.Text = dlcApp.name;
                            dlcNode.Checked = selection.SelectedSteamDlc.ContainsKey(appId);
                            dlcNode.Remove();
                            programNode.Nodes.Add(dlcNode);
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

            progressLabel.Text = $"Setting up SteamCMD . . . ";
            await SteamCMD.Setup(iProgress);

            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            ProgramSelection.ValidateAll();
            TreeNodes.ForEach(node =>
            {
                if (!int.TryParse(node.Name, out int appId) || node.Parent is null && ProgramSelection.FromAppId(appId) is null) node.Remove();
            });
            //DateTime start = DateTime.Now;
            await GetCreamApiApplicablePrograms(iProgress);
            //DateTime end = DateTime.Now;
            //TimeSpan t = end - start;
            //new DialogForm(null).Show("GetCreamApiApplicablePrograms", SystemIcons.Information, "Gathering took " + t.ToString(@"mm\:ss"), "OK");
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
        if (node is not null && int.TryParse(node.Name, out int appId))
        {
            ProgramSelection selection = ProgramSelection.FromAppId(appId);
            if (selection is null)
            {
                TreeNode parent = node.Parent;
                if (parent is not null && int.TryParse(parent.Name, out int gameAppId))
                {
                    ProgramSelection.FromAppId(gameAppId).ToggleDlc(appId, node.Checked);
                    parent.Checked = parent.Nodes.Cast<TreeNode>().ToList().Any(treeNode => treeNode.Checked);
                }
            }
            else
            {
                if (selection.AllSteamDlc.Any())
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

    private class TreeNodeSorter : IComparer
    {
        public int Compare(object a, object b)
        {
            if (!int.TryParse((a as TreeNode).Name, out int A)) return 1;
            if (!int.TryParse((b as TreeNode).Name, out int B)) return 0;
            return A > B ? 1 : 0;
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
            if (Directory.Exists(Program.ParadoxLauncherInstallPath))
            {
                foreach (string file in Directory.GetFiles(Program.ParadoxLauncherInstallPath, "*.exe"))
                {
                    images["Paradox Launcher"] = Program.GetFileIconImage(file);
                    break;
                }
            }
            images["Notepad"] = Program.GetNotepadImage();
            images["Command Prompt"] = Program.GetCommandPromptImage();
            images["File Explorer"] = Program.GetFileExplorerImage();
            images["SteamDB"] = await Program.GetImageFromUrl("https://steamdb.info/favicon.ico");
            images["Steam Store"] = await Program.GetImageFromUrl("https://store.steampowered.com/favicon.ico");
            images["Steam Community"] = await Program.GetImageFromUrl("https://steamcommunity.com/favicon.ico");
        });
        Image Image(string identifier) => images.GetValueOrDefault(identifier, null);
        void TrySetImageAsync(ToolStripMenuItem menuItem, int appId, string iconStaticId, bool client = false) =>
            Task.Run(async () =>
            {
                menuItem.Image = client ? await Program.GetSteamClientIcon(appId, iconStaticId) : await Program.GetSteamIcon(appId, iconStaticId);
                images[client ? "ClientIcon_" + appId : "Icon_" + appId] = menuItem.Image;
            });
        selectionTreeView.NodeMouseClick += (sender, e) =>
        {
            TreeNode node = e.Node;
            TreeNode parentNode = node.Parent;
            if (!int.TryParse(node.Name, out int appId)) return;
            ProgramSelection selection = ProgramSelection.FromAppId(appId);
            (int gameAppId, (string name, string iconStaticId) app)? dlc = null;
            if (selection is null) dlc = ProgramSelection.GetDlcFromAppId(appId);
            if (e.Button == MouseButtons.Right && node.Bounds.Contains(e.Location))
            {
                selectionTreeView.SelectedNode = node;
                nodeContextMenu.Items.Clear();
                ToolStripMenuItem header = new(selection?.Name ?? node.Text, Image(appId == 0 ? "Paradox Launcher" : "Icon_" + node.Name));
                if (header.Image is null)
                {
                    string iconStaticId = dlc?.app.iconStaticId ?? selection?.IconStaticID;
                    if (iconStaticId is not null)
                        TrySetImageAsync(header, appId, iconStaticId);
                    else if (dlc is not null)
                    {
                        int gameAppId = dlc.Value.gameAppId;
                        header.Image = Image("Icon_" + gameAppId);
                        ProgramSelection gameSelection = ProgramSelection.FromAppId(gameAppId);
                        iconStaticId = gameSelection?.IconStaticID;
                        if (header.Image is null && iconStaticId is not null)
                            TrySetImageAsync(header, gameAppId, iconStaticId);
                    }
                }
                nodeContextMenu.Items.Add(header);
                string appInfo = $@"{SteamCMD.AppInfoPath}\{appId}.vdf";
                if (appId != 0 && Directory.Exists(Directory.GetDirectoryRoot(appInfo)) && File.Exists(appInfo))
                {
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open AppInfo", Image("Notepad"),
                        new EventHandler((sender, e) => Program.OpenFileInNotepad(appInfo))));
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
                    if (appId == 0)
                    {
                        nodeContextMenu.Items.Add(new ToolStripSeparator());
                        nodeContextMenu.Items.Add(new ToolStripMenuItem("Repair", Image("Command Prompt"),
                            new EventHandler(async (sender, e) =>
                            {
                                if (!Program.IsProgramRunningDialog(this, selection)) return;
                                bool shouldReinstall = false;
                                byte[] properApi = null;
                                byte[] properApi64 = null;
                                foreach (string directory in selection.SteamApiDllDirectories)
                                {
                                    directory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                                    shouldReinstall = File.Exists(cApi);
                                    await InstallForm.UninstallCreamAPI(directory);
                                    if (properApi is null && File.Exists(api) && !FileResourceExtensions.Equals(Properties.Resources.API, api))
                                        properApi = File.ReadAllBytes(api);
                                    if (properApi64 is null && File.Exists(api64) && !FileResourceExtensions.Equals(Properties.Resources.API64, api64))
                                        properApi64 = File.ReadAllBytes(api64);
                                }
                                if (properApi is not null || properApi64 is not null)
                                {
                                    bool neededRepair = false;
                                    foreach (string directory in selection.SteamApiDllDirectories)
                                    {
                                        directory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                                        if (properApi is not null && FileResourceExtensions.Equals(Properties.Resources.API, api))
                                        {
                                            properApi.Write(api);
                                            neededRepair = true;
                                        }
                                        if (properApi64 is not null && FileResourceExtensions.Equals(Properties.Resources.API64, api64))
                                        {
                                            properApi64.Write(api64);
                                            neededRepair = true;
                                        }
                                        if (shouldReinstall)
                                            await InstallForm.InstallCreamAPI(directory, selection);
                                    }
                                    if (neededRepair)
                                        new DialogForm(this).Show("Paradox Launcher Repair", Icon, "Paradox Launcher successfully repaired!", "OK");
                                    else
                                        new DialogForm(this).Show("Paradox Launcher Repair", SystemIcons.Information, "Paradox Launcher does not need to be repaired.", "OK");
                                }
                                else
                                    new DialogForm(this).Show("Paradox Launcher Repair", SystemIcons.Error, "Paradox Launcher repair failed!"
                                        + "\n\nAn original Steamworks API file could not be found."
                                        + "\nYou must reinstall Paradox Launcher to fix this issue.", "OK");
                            })));
                    }
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Root Directory", Image("File Explorer"),
                        new EventHandler((sender, e) => Program.OpenDirectoryInFileExplorer(selection.RootDirectory))));
                    for (int i = 0; i < selection.SteamApiDllDirectories.Count; i++)
                    {
                        string directory = selection.SteamApiDllDirectories[i];
                        nodeContextMenu.Items.Add(new ToolStripMenuItem($"Open Steamworks Directory ({i + 1})", Image("File Explorer"),
                            new EventHandler((sender, e) => Program.OpenDirectoryInFileExplorer(directory))));
                    }
                }
                if (appId != 0)
                {
                    nodeContextMenu.Items.Add(new ToolStripSeparator());
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open SteamDB", Image("SteamDB"),
                        new EventHandler((sender, e) => Program.OpenUrlInInternetBrowser("https://steamdb.info/app/" + appId))));
                    nodeContextMenu.Items.Add(new ToolStripMenuItem("Open Steam Store", Image("Steam Store"),
                        new EventHandler((sender, e) => Program.OpenUrlInInternetBrowser("https://store.steampowered.com/app/" + appId))));
                    if (selection is not null)
                    {
                        ToolStripMenuItem steamCommunity = new("Open Steam Community", Image("ClientIcon_" + node.Name),
                            new EventHandler((sender, e) => Program.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" + appId)));
                        nodeContextMenu.Items.Add(steamCommunity);
                        if (steamCommunity.Image is null)
                        {
                            steamCommunity.Image = Image("Steam Community");
                            TrySetImageAsync(steamCommunity, appId, selection.ClientIconStaticID, true);
                        }
                    }
                }
                nodeContextMenu.Show(selectionTreeView, e.Location);
            }
        };
        OnLoad();
    }

    private static void PopulateParadoxLauncherDlc(ProgramSelection paradoxLauncher = null)
    {
        paradoxLauncher ??= ProgramSelection.FromAppId(0);
        if (paradoxLauncher is not null)
        {
            paradoxLauncher.ExtraSteamAppIdDlc.Clear();
            foreach (ProgramSelection selection in ProgramSelection.AllUsableEnabled)
            {
                if (selection.Name == paradoxLauncher.Name) continue;
                if (selection.AppInfo.Value?.GetChild("extended")?.GetChild("publisher")?.ToString() != "Paradox Interactive") continue;
                paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.Name, selection.SelectedSteamDlc));
            }
            if (!paradoxLauncher.ExtraSteamAppIdDlc.Any())
                foreach (ProgramSelection selection in ProgramSelection.AllUsable)
                {
                    if (selection.Name == paradoxLauncher.Name) continue;
                    if (selection.AppInfo.Value?.GetChild("extended")?.GetChild("publisher")?.ToString() != "Paradox Interactive") continue;
                    paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.Name, selection.AllSteamDlc));
                }
        }
    }

    private static bool ParadoxLauncherDlcDialog(Form form)
    {
        ProgramSelection paradoxLauncher = ProgramSelection.FromAppId(0);
        if (paradoxLauncher is not null && paradoxLauncher.Enabled)
        {
            PopulateParadoxLauncherDlc(paradoxLauncher);
            if (!paradoxLauncher.ExtraSteamAppIdDlc.Any())
            {
                return new DialogForm(form).Show(Program.ApplicationName, SystemIcons.Warning,
                    $"WARNING: There are no installed games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling CreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
                    "Ignore", "Cancel") != DialogResult.OK;
            }
        }
        return false;
    }

    private void OnAccept(bool uninstall = false)
    {
        if (ProgramSelection.All.Any())
        {
            foreach (ProgramSelection selection in ProgramSelection.AllUsableEnabled)
                if (!Program.IsProgramRunningDialog(this, selection)) return;
            if (ParadoxLauncherDlcDialog(this)) return;
            Hide();
            InstallForm installForm = new(this, uninstall);
            installForm.ShowDialog();
            if (installForm.Reselecting)
            {
                this.InheritLocation(installForm);
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
        new DialogForm(this).Show(blockedGamesCheckBox.Text, SystemIcons.Information,
            "Blocks the program from caching and displaying games protected by DLL checks," +
            "\nanti-cheats, or that are confirmed not to be working with CreamAPI." +
            "\n\nBlocked game names:" + blockedGames +
            "\n\nBlocked game sub-directories:" + blockedDirectories +
            "\n\nBlocked game sub-directory exceptions (not blocked):" + blockedDirectoryExceptions,
            "OK");
    }
}
