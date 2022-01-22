using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

using Microsoft.Win32;

namespace CreamInstaller
{
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
            string steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
            steamInstallPath ??= Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null) as string;
            if (steamInstallPath != null && Directory.Exists(steamInstallPath))
            {
                string libraryFolder = steamInstallPath + @"\steamapps";
                if (Directory.Exists(libraryFolder))
                {
                    gameDirectories.Add(libraryFolder);
                    try
                    {
                        string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
                        if (File.Exists(libraryFolders))
                        {
                            dynamic property = VdfConvert.Deserialize(File.ReadAllText(libraryFolders, Encoding.UTF8)).Value;
                            foreach (dynamic _property in property)
                                if (int.TryParse(_property.Key, out int _))
                                {
                                    string path = _property.Value.path.ToString() + @"\steamapps";
                                    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) continue;
                                    if (!gameDirectories.Contains(path)) gameDirectories.Add(path);
                                }
                        }
                    }
                    catch { }
                }
            }
            return gameDirectories;
        });

        private static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
        {
            List<string> dllDirectories = new();
            if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
            string api = gameDirectory + @"\steam_api.dll";
            string api64 = gameDirectory + @"\steam_api64.dll";
            if (File.Exists(api) || File.Exists(api64)) dllDirectories.Add(gameDirectory);
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
            if (!dllDirectories.Any()) return null;
            return dllDirectories;
        });

        private static async Task<List<Tuple<int, string, string, int, string>>> GetGamesFromLibraryDirectory(string libraryDirectory) => await Task.Run(() =>
        {
            List<Tuple<int, string, string, int, string>> games = new();
            if (Program.Canceled || !Directory.Exists(libraryDirectory)) return null;
            string[] files = Directory.GetFiles(libraryDirectory);
            foreach (string file in files)
            {
                if (Program.Canceled) return null;
                if (Path.GetExtension(file) == ".acf")
                {
                    try
                    {
                        dynamic property = VdfConvert.Deserialize(File.ReadAllText(file, Encoding.UTF8));
                        string _appid = property.Value.appid.ToString();
                        string installdir = property.Value.installdir.ToString();
                        string name = property.Value.name.ToString();
                        string _buildid = property.Value.buildid.ToString();
                        if (string.IsNullOrWhiteSpace(_appid)
                            || string.IsNullOrWhiteSpace(installdir)
                            || string.IsNullOrWhiteSpace(name)
                            || string.IsNullOrWhiteSpace(_buildid))
                            continue;
                        string branch = property.Value.UserConfig?.betakey?.ToString();
                        if (string.IsNullOrWhiteSpace(branch)) branch = "public";
                        string gameDirectory = libraryDirectory + @"\common\" + installdir;
                        if (!int.TryParse(_appid, out int appid)) continue;
                        if (!int.TryParse(_buildid, out int buildid)) continue;
                        games.Add(new(appid, name, branch, buildid, gameDirectory));
                    }
                    catch { }
                }
            }
            if (!games.Any()) return null;
            return games;
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

        internal List<Task> RunningTasks = new();

        private async Task GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            if (Program.Canceled) return;
            List<Tuple<int, string, string, int, string>> applicablePrograms = new();
            string launcherRootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Programs\\Paradox Interactive";
            if (Directory.Exists(launcherRootDirectory))
                applicablePrograms.Add(new(0, "Paradox Launcher", "", 0, launcherRootDirectory));
            List<string> gameLibraryDirectories = await GameLibraryDirectories();
            foreach (string libraryDirectory in gameLibraryDirectories)
            {
                List<Tuple<int, string, string, int, string>> games = await GetGamesFromLibraryDirectory(libraryDirectory);
                if (games is not null)
                    foreach (Tuple<int, string, string, int, string> game in games)
                        applicablePrograms.Add(game);
            }

            int cur = 0;
            RunningTasks.Clear();
            foreach (Tuple<int, string, string, int, string> program in applicablePrograms)
            {
                int appId = program.Item1;
                string name = program.Item2;
                string branch = program.Item3;
                int buildId = program.Item4;
                string directory = program.Item5;
                ProgramSelection selection = ProgramSelection.FromAppId(appId);
                if (Program.Canceled) return;
                if (Program.BlockProtectedGames)
                {
                    bool blockedGame = Program.ProtectedGameNames.Contains(name);
                    if (!Program.ProtectedGameDirectoryExceptions.Contains(name))
                        foreach (string path in Program.ProtectedGameDirectories)
                            if (Directory.Exists(directory + path))
                                blockedGame = true;
                    if (blockedGame)
                    {
                        if (selection is not null)
                        {
                            selection.Enabled = false;
                            selection.Usable = false;
                        }
                        continue;
                    }
                }
                RunningTasks.Add(Task.Run(async () =>
                {
                    if (Program.Canceled) return;
                    List<string> dllDirectories = await GetDllDirectoriesFromGameDirectory(directory);
                    if (dllDirectories is null) return;
                    VProperty appInfo = null;
                    if (appId > 0) appInfo = await SteamCMD.GetAppInfo(appId, branch, buildId);
                    if (appId > 0 && appInfo is null) return;
                    if (Program.Canceled) return;
                    ConcurrentDictionary<int, string> dlc = new();
                    List<Task> dlcTasks = new();
                    List<int> dlcIds = await SteamCMD.ParseDlcAppIds(appInfo);
                    if (dlcIds.Count > 0)
                    {
                        foreach (int id in dlcIds)
                        {
                            if (Program.Canceled) return;
                            Task task = Task.Run(async () =>
                            {
                                if (Program.Canceled) return;
                                string dlcName = null;
                                VProperty dlcAppInfo = await SteamCMD.GetAppInfo(id);
                                if (dlcAppInfo is not null)
                                    dlcName = dlcAppInfo?.Value?["common"]?["name"]?.ToString();
                                if (Program.Canceled) return;
                                if (string.IsNullOrWhiteSpace(dlcName)) return; //dlcName = "Unknown DLC";
                                dlc[id] = /*$"[{id}] " +*/ dlcName;
                                progress.Report(++cur);
                            });
                            dlcTasks.Add(task);
                            RunningTasks.Add(task);
                        }
                        progress.Report(-RunningTasks.Count);
                    }
                    else if (appId > 0) return;
                    if (Program.Canceled) return;
                    if (string.IsNullOrWhiteSpace(name)) return;

                    selection ??= new();
                    selection.Usable = true;
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.SteamAppId = appId;
                    selection.SteamApiDllDirectories = dllDirectories;
                    selection.AppInfo = appInfo;
                    if (allCheckBox.Checked) selection.Enabled = true;

                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled) return;
                        await task;
                    }
                    if (Program.Canceled) return;
                    selectionTreeView.Invoke((MethodInvoker)delegate
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
                            foreach (KeyValuePair<int, string> dlcApp in dlc)
                            {
                                if (Program.Canceled || programNode is null) return;
                                selection.AllSteamDlc[dlcApp.Key] = dlcApp.Value;
                                if (allCheckBox.Checked) selection.SelectedSteamDlc[dlcApp.Key] = dlcApp.Value;
                                TreeNode dlcNode = TreeNodes.Find(s => s.Name == "" + dlcApp.Key) ?? new();
                                dlcNode.Name = "" + dlcApp.Key;
                                dlcNode.Text = dlcApp.Value;
                                dlcNode.Checked = selection.SelectedSteamDlc.Contains(dlcApp);
                                dlcNode.Remove();
                                programNode.Nodes.Add(dlcNode);
                            }
                        }
                    });
                    progress.Report(++cur);
                }));
            }
            progress.Report(-RunningTasks.Count);
            progress.Report(cur);
            foreach (Task task in RunningTasks.ToList())
            {
                if (Program.Canceled) return;
                await task;
            }
            progress.Report(RunningTasks.Count);
        }

        private async void OnLoad(bool validating = false)
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
                progressLabel.Visible = true;
                progressBar1.Visible = true;
                progressBar1.Value = 0;
                groupBox1.Size = new(groupBox1.Size.Width, groupBox1.Size.Height - 44);

                bool setup = true;
                int maxProgress = 0;
                int curProgress = 0;
                Progress<int> progress = new();
                IProgress<int> iProgress = progress;
                progress.ProgressChanged += (sender, _progress) =>
                {
                    if (_progress < 0) maxProgress = -_progress;
                    else curProgress = _progress;
                    int p = Math.Max(Math.Min((int)((float)(curProgress / (float)maxProgress) * 100), 100), 0);
                    if (validating) progressLabel.Text = $"Validating . . . {p}% ({curProgress}/{maxProgress})";
                    else if (setup) progressLabel.Text = $"Setting up SteamCMD . . . {p}% ({curProgress}/{maxProgress})";
                    else progressLabel.Text = $"Gathering and caching your applicable games and their DLCs . . . {p}% ({curProgress}/{maxProgress})";
                    progressBar1.Value = p;
                };

                iProgress.Report(-1660); // not exact, number varies
                int cur = 0;
                iProgress.Report(cur);
                if (!validating) progressLabel.Text = "Setting up SteamCMD . . . ";
                if (!Directory.Exists(SteamCMD.DirectoryPath)) Directory.CreateDirectory(SteamCMD.DirectoryPath);

                FileSystemWatcher watcher = new(SteamCMD.DirectoryPath);
                watcher.Changed += (sender, e) => iProgress.Report(++cur);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                await SteamCMD.Setup();
                watcher.Dispose();

                setup = false;
                if (!validating) progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";

                await GetCreamApiApplicablePrograms(iProgress);
                ProgramSelection.ValidateAll();
                TreeNodes.ForEach(node =>
                {
                    if (node.Parent is null && ProgramSelection.FromAppId(int.Parse(node.Name)) is null) node.Remove();
                });

                progressBar1.Value = 100;
                groupBox1.Size = new(groupBox1.Size.Width, groupBox1.Size.Height + 44);
                progressLabel.Visible = false;
                progressBar1.Visible = false;
                selectionTreeView.Enabled = ProgramSelection.All.Any();
                allCheckBox.Enabled = selectionTreeView.Enabled;
                noneFoundLabel.Visible = !selectionTreeView.Enabled;
                installButton.Enabled = ProgramSelection.AllSafeEnabled.Any();
                uninstallButton.Enabled = installButton.Enabled;
                cancelButton.Enabled = false;
                scanButton.Enabled = true;
                blockedGamesCheckBox.Enabled = true;
                blockProtectedHelpButton.Enabled = true;

                progressLabel.Text = "Validating . . . ";
                //if (!validating && !Program.Canceled) OnLoad(true);
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
                ProgramSelection selection = ProgramSelection.FromAppId(int.Parse(node.Name));
                if (selection is null)
                {
                    TreeNode parent = node.Parent;
                    if (parent is not null)
                    {
                        ProgramSelection.FromAppId(int.Parse(parent.Name)).ToggleDlc(int.Parse(node.Name), node.Checked);
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
            installButton.Enabled = ProgramSelection.AllSafeEnabled.Any();
            uninstallButton.Enabled = installButton.Enabled;
        }

        private class TreeNodeSorter : IComparer
        {
            public int Compare(object a, object b)
            {
                TreeNode A = a as TreeNode;
                TreeNode B = b as TreeNode;
                return int.Parse(A.Name) > int.Parse(B.Name) ? 1 : 0;
            }
        }

        private void OnLoad(object sender, EventArgs _)
        {
            selectionTreeView.TreeViewNodeSorter = new TreeNodeSorter();
            selectionTreeView.AfterCheck += OnTreeViewNodeCheckedChanged;
            selectionTreeView.NodeMouseClick += (sender, e) =>
            {
                TreeNode node = e.Node;
                string appId = node.Name;
                if (e.Button == MouseButtons.Right && node.Bounds.Contains(e.Location) && appId != "0")
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://steamdb.info/app/" + appId,
                        UseShellExecute = true
                    });
            };
            OnLoad();
        }

        private static void PopulateParadoxLauncherDlc(ProgramSelection paradoxLauncher = null)
        {
            paradoxLauncher ??= ProgramSelection.FromAppId(0);
            if (paradoxLauncher is not null)
            {
                paradoxLauncher.ExtraSteamAppIdDlc.Clear();
                foreach (ProgramSelection selection in ProgramSelection.AllSafeEnabled)
                {
                    if (selection.Name == paradoxLauncher.Name) continue;
                    if (selection.AppInfo.Value["extended"]["publisher"].ToString() != "Paradox Interactive") continue;
                    paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.Name, selection.SelectedSteamDlc));
                }
                if (!paradoxLauncher.ExtraSteamAppIdDlc.Any())
                    foreach (ProgramSelection selection in ProgramSelection.AllSafe)
                    {
                        if (selection.Name == paradoxLauncher.Name) continue;
                        if (selection.AppInfo.Value["extended"]["publisher"].ToString() != "Paradox Interactive") continue;
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
                    if (new DialogForm(form).Show(Program.ApplicationName, SystemIcons.Warning,
                    $"WARNING: There are no installed games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling CreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
                    "Ignore", "Cancel") == DialogResult.OK)
                        return false;
                    return true;
                }
            }
            return false;
        }

        private void OnAccept(bool uninstall = false)
        {
            if (ProgramSelection.All.Any())
            {
                foreach (ProgramSelection selection in ProgramSelection.AllSafeEnabled)
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
            Task.Run(async () =>
            {
                await Program.Cleanup();
            });
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
}