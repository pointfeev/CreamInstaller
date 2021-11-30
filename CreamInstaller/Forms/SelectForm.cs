using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
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

namespace CreamInstaller
{
    public partial class SelectForm : Form
    {
        public SelectForm(IWin32Window owner)
        {
            Owner = owner as Form;
            InitializeComponent();
            Program.SelectForm = this;
            Text = Program.ApplicationName;
            Icon = Properties.Resources.Icon;
        }

        private static List<string> GameLibraryDirectories
        {
            get
            {
                List<string> gameDirectories = new();
                if (Program.Canceled) return gameDirectories;
                string steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
                if (steamInstallPath == null) steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null) as string;
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
                                dynamic property = VdfConvert.Deserialize(File.ReadAllText(libraryFolders, Encoding.UTF8));
                                foreach (dynamic _property in property.Value)
                                {
                                    if (int.TryParse(_property.Key, out int _))
                                    {
                                        string path = _property.Value.path.ToString() + @"\steamapps";
                                        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) continue;
                                        if (!gameDirectories.Contains(path)) gameDirectories.Add(path);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                return gameDirectories;
            }
        }

        private static bool GetDllDirectoriesFromGameDirectory(string gameDirectory, out List<string> dllDirectories)
        {
            dllDirectories = new();
            if (Program.Canceled || !Directory.Exists(gameDirectory)) return false;
            string api = gameDirectory + @"\steam_api.dll";
            string api64 = gameDirectory + @"\steam_api64.dll";
            if (File.Exists(api) || File.Exists(api64)) dllDirectories.Add(gameDirectory);
            foreach (string _directory in Directory.GetDirectories(gameDirectory))
            {
                if (Program.Canceled) return false;
                try
                {
                    if (GetDllDirectoriesFromGameDirectory(_directory, out List<string> _dllDirectories))
                        dllDirectories.AddRange(_dllDirectories);
                }
                catch { }
            }
            if (!dllDirectories.Any()) return false;
            return true;
        }

        private static bool GetGamesFromLibraryDirectory(string libraryDirectory, out List<Tuple<int, string, string, int, string>> games)
        {
            games = new();
            if (Program.Canceled || !Directory.Exists(libraryDirectory)) return false;
            foreach (string directory in Directory.GetFiles(libraryDirectory))
            {
                if (Program.Canceled) return false;
                if (Path.GetExtension(directory) == ".acf")
                {
                    try
                    {
                        dynamic property = VdfConvert.Deserialize(File.ReadAllText(directory, Encoding.UTF8));
                        string _appid = property.Value.appid.ToString();
                        string installdir = property.Value.installdir.ToString();
                        string name = property.Value.name.ToString();
                        string _buildid = property.Value.buildid.ToString();
                        if (string.IsNullOrWhiteSpace(_appid)
                            || string.IsNullOrWhiteSpace(installdir)
                            || string.IsNullOrWhiteSpace(name)
                            || string.IsNullOrWhiteSpace(_buildid)) continue;
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
            if (!games.Any()) return false;
            return true;
        }

        private readonly List<TreeNode> treeNodes = new();
        internal List<Task> RunningTasks = new();

        private void GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            int cur = 0;
            if (Program.Canceled) return;
            List<Tuple<int, string, string, int, string>> applicablePrograms = new();
            string launcherRootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Programs\\Paradox Interactive";
            if (Directory.Exists(launcherRootDirectory)) applicablePrograms.Add(new(0, "Paradox Launcher", "", 0, launcherRootDirectory));
            foreach (string libraryDirectory in GameLibraryDirectories)
                if (GetGamesFromLibraryDirectory(libraryDirectory, out List<Tuple<int, string, string, int, string>> games))
                    foreach (Tuple<int, string, string, int, string> game in games)
                        applicablePrograms.Add(game);
            RunningTasks.Clear();
            foreach (Tuple<int, string, string, int, string> program in applicablePrograms)
            {
                int appId = program.Item1;
                string name = program.Item2;
                string branch = program.Item3;
                int buildId = program.Item4;
                string directory = program.Item5;
                if (Program.Canceled) return;
                // EasyAntiCheat detects DLL changes, so skip those games
                if (Directory.Exists(directory + @"\EasyAntiCheat")) continue;
                // BattlEye in DayZ detects DLL changes, but not in Arma3?
                if (name != "Arma 3" && Directory.Exists(directory + @"\BattlEye")) continue;
                Task task = Task.Run(() =>
                {
                    if (Program.Canceled || !GetDllDirectoriesFromGameDirectory(directory, out List<string> dllDirectories)) return;
                    VProperty appInfo = null;
                    if (Program.Canceled || (appId > 0 && !SteamCMD.GetAppInfo(appId, out appInfo, branch, buildId))) return;
                    if (Program.Canceled) return;
                    ConcurrentDictionary<int, string> dlc = new();
                    List<Task> dlcTasks = new();
                    List<int> dlcIds = SteamCMD.ParseDlcAppIds(appInfo);
                    if (dlcIds.Count > 0)
                    {
                        foreach (int id in dlcIds)
                        {
                            if (Program.Canceled) return;
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled) return;
                                string dlcName = null;
                                if (SteamCMD.GetAppInfo(id, out VProperty dlcAppInfo)) dlcName = dlcAppInfo?.Value?["common"]?["name"]?.ToString();
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
                    if (Program.Canceled) return;

                    ProgramSelection selection = ProgramSelection.FromAppId(appId) ?? new();
                    selection.Name = name;
                    selection.RootDirectory = directory;
                    selection.SteamAppId = appId;
                    selection.SteamApiDllDirectories = dllDirectories;
                    selection.AppInfo = appInfo;

                    foreach (Task task in dlcTasks.ToList())
                    {
                        if (Program.Canceled) return;
                        task.Wait();
                    }
                    if (Program.Canceled) return;
                    treeView1.Invoke((MethodInvoker)delegate
                    {
                        if (Program.Canceled) return;
                        TreeNode programNode = treeNodes.Find(s => s.Name == "" + appId) ?? new();
                        programNode.Name = "" + appId;
                        programNode.Text = /*(appId > 0 ? $"[{appId}] " : "") +*/ name;
                        programNode.Checked = selection.Enabled;
                        programNode.Remove();
                        treeView1.Nodes.Add(programNode);
                        treeNodes.Remove(programNode);
                        treeNodes.Add(programNode);
                        if (appId == 0) // paradox launcher
                        {
                            // maybe add game and/or dlc choice here?
                        }
                        else foreach (KeyValuePair<int, string> dlcApp in dlc.ToList())
                            {
                                if (Program.Canceled || programNode is null) return;
                                TreeNode dlcNode = treeNodes.Find(s => s.Name == "" + dlcApp.Key) ?? new();
                                dlcNode.Name = "" + dlcApp.Key;
                                dlcNode.Text = dlcApp.Value;
                                dlcNode.Checked = selection.SelectedSteamDlc.Contains(dlcApp);
                                dlcNode.Remove();
                                programNode.Nodes.Add(dlcNode);
                                treeNodes.Remove(dlcNode);
                                treeNodes.Add(dlcNode);
                                selection.AllSteamDlc[dlcApp.Key] = dlcApp.Value;
                            }
                    });
                    progress.Report(++cur);
                });
                RunningTasks.Add(task);
            }
            progress.Report(-RunningTasks.Count);
            progress.Report(cur);
            foreach (Task task in RunningTasks.ToList())
            {
                if (Program.Canceled) return;
                task.Wait();
            }
            progress.Report(RunningTasks.Count);
        }

        private async void OnLoad()
        {
            Program.Canceled = false;
            cancelButton.Enabled = true;
            scanButton.Enabled = false;
            noneFoundLabel.Visible = false;
            allCheckBox.Enabled = false;
            acceptButton.Enabled = false;
            treeView1.Enabled = false;

            label2.Visible = true;
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
                if (setup) label2.Text = $"Setting up SteamCMD . . . {p}% ({curProgress}/{maxProgress})";
                else label2.Text = $"Gathering and caching your applicable games and their DLCs . . . {p}% ({curProgress}/{maxProgress})";
                progressBar1.Value = p;
            };

            iProgress.Report(-1660); // not exact, number varies
            int cur = 0;
            iProgress.Report(cur);
            label2.Text = "Setting up SteamCMD . . . ";
            if (!Directory.Exists(SteamCMD.DirectoryPath)) Directory.CreateDirectory(SteamCMD.DirectoryPath);
            FileSystemWatcher watcher = new(SteamCMD.DirectoryPath);
            watcher.Changed += (sender, e) => iProgress.Report(++cur);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            await Task.Run(() => SteamCMD.Setup());
            watcher.Dispose();

            setup = false;
            label2.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            await Task.Run(() => GetCreamApiApplicablePrograms(iProgress));
            ProgramSelection.All.ForEach(selection => selection.SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory)));
            ProgramSelection.All.RemoveAll(selection => !Directory.Exists(selection.RootDirectory) || !selection.SteamApiDllDirectories.Any());
            foreach (TreeNode treeNode in treeNodes)
                if (treeNode.Parent is null && ProgramSelection.FromAppId(int.Parse(treeNode.Name)) is null)
                    treeNode.Remove();
            //SetMinimumSizeFromTreeView();

            progressBar1.Value = 100;
            groupBox1.Size = new(groupBox1.Size.Width, groupBox1.Size.Height + 44);
            label2.Visible = false;
            progressBar1.Visible = false;

            treeView1.Enabled = ProgramSelection.All.Any();
            allCheckBox.Enabled = treeView1.Enabled;
            noneFoundLabel.Visible = !treeView1.Enabled;

            acceptButton.Enabled = ProgramSelection.AllSafeEnabled.Any();
            cancelButton.Enabled = false;
            scanButton.Enabled = true;
        }

        /*private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;

        [DllImport("user32.dll", ExactSpelling = false, CharSet = CharSet.Auto)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int MINIMUM_SIZE_INCREMENT = 8;
        private void SetMinimumSizeFromTreeView()
        {
            Dictionary<TreeNode, bool> expansionState = new();
            foreach (TreeNode node in treeView1.Nodes) expansionState[node] = node.IsExpanded;
            treeView1.ExpandAll();
            Size minimumSize = MinimumSize;
            Point point;
            int style = GetWindowLong(treeView1.Handle, GWL_STYLE);
            while ((style & WS_HSCROLL) != 0)
            {
                minimumSize.Width += MINIMUM_SIZE_INCREMENT;
                point = Location;
                point.X -= MINIMUM_SIZE_INCREMENT / 2;
                Location = point;
                MinimumSize = minimumSize;
                style = GetWindowLong(treeView1.Handle, GWL_STYLE);
                Update();
            }
            foreach (TreeNode node in treeView1.Nodes) if (!expansionState[node]) node.Collapse();
        }*/

        private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;
            ProgramSelection selection = ProgramSelection.FromAppId(int.Parse(e.Node.Name));
            if (selection is null)
            {
                ProgramSelection.FromAppId(int.Parse(e.Node.Parent.Name)).ToggleDlc(int.Parse(e.Node.Name), e.Node.Checked);
                e.Node.Parent.Checked = e.Node.Parent.Nodes.Cast<TreeNode>().ToList().Any(treeNode => treeNode.Checked);
            }
            else
            {
                if (selection.AllSteamDlc.Any())
                {
                    selection.ToggleAllDlc(e.Node.Checked);
                    e.Node.Nodes.Cast<TreeNode>().ToList().ForEach(treeNode => treeNode.Checked = e.Node.Checked);
                }
                else selection.Enabled = e.Node.Checked;
                allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
                allCheckBox.Checked = treeNodes.TrueForAll(treeNode => treeNode.Checked);
                allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            }
            acceptButton.Enabled = ProgramSelection.AllSafeEnabled.Any();
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
            treeView1.TreeViewNodeSorter = new TreeNodeSorter();
            treeView1.AfterCheck += OnTreeViewNodeCheckedChanged;
            treeView1.NodeMouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ProgramSelection selection = ProgramSelection.FromAppId(int.Parse(e.Node.Name));
                    KeyValuePair<int, string>? dlc = ProgramSelection.GetAllSteamDlc(int.Parse(e.Node.Name));
                    int appId = selection?.SteamAppId ?? dlc?.Key ?? 0;
                    if (appId > 0) Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://steamdb.info/app/" + appId,
                        UseShellExecute = true
                    });
                }
            };
        retry:
            try
            {
                OnLoad();
            }
            catch (Exception e)
            {
                if (ExceptionHandler.OutputException(e)) goto retry;
                Close();
            }
        }

        private static void PopulateParadoxLauncherDlc(ProgramSelection paradoxLauncher = null)
        {
            paradoxLauncher ??= ProgramSelection.FromAppId(0);
            if (!(paradoxLauncher is null))
            {
                paradoxLauncher.ExtraSteamAppIdDlc.Clear();
                foreach (ProgramSelection selection in ProgramSelection.AllSafeEnabled)
                {
                    if (selection.Name == paradoxLauncher.Name) continue;
                    if (selection.AppInfo.Value["extended"]["publisher"].ToString() != "Paradox Interactive") continue;
                    paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.Name, selection.SelectedSteamDlc));
                }
                if (!paradoxLauncher.ExtraSteamAppIdDlc.Any())
                {
                    foreach (ProgramSelection selection in ProgramSelection.AllSafe)
                    {
                        if (selection.Name == paradoxLauncher.Name) continue;
                        if (selection.AppInfo.Value["extended"]["publisher"].ToString() != "Paradox Interactive") continue;
                        paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.Name, selection.AllSteamDlc));
                    }
                }
            }
        }

        private static bool ParadoxLauncherDlcDialog(Form form)
        {
            ProgramSelection paradoxLauncher = ProgramSelection.FromAppId(0);
            if (!(paradoxLauncher is null) && paradoxLauncher.Enabled)
            {
                PopulateParadoxLauncherDlc(paradoxLauncher);
                if (!paradoxLauncher.ExtraSteamAppIdDlc.Any())
                {
                    if (new DialogForm(form).Show(Program.ApplicationName, SystemIcons.Warning,
                    $"WARNING: There are no installed games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling CreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
                    "Ignore", "Cancel") == DialogResult.OK)
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private void OnAccept(object sender, EventArgs e)
        {
            if (ProgramSelection.All.Count > 0)
            {
                foreach (ProgramSelection selection in ProgramSelection.AllSafeEnabled)
                    if (!Program.IsProgramRunningDialog(this, selection)) return;
                if (ParadoxLauncherDlcDialog(this)) return;
                Hide();
                InstallForm installForm = new(this);
                installForm.ShowDialog();
                if (installForm.Reselecting)
                {
                    foreach (TreeNode treeNode in treeNodes)
                        if (!(treeNode.Parent is null) || int.Parse(treeNode.Name) == 0)
                            OnTreeViewNodeCheckedChanged(null, new(treeNode, TreeViewAction.ByMouse));
                    this.InheritLocation(installForm);
                    Show();
                }
                else
                {
                    Close();
                }
            }
        }

        private void OnScan(object sender, EventArgs e)
        {
            OnLoad();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Program.Cleanup();
        }

        private void OnAllCheckBoxChanged(object sender, EventArgs e)
        {
            bool shouldCheck = false;
            foreach (TreeNode treeNode in treeNodes)
            {
                if (treeNode.Parent is null)
                {
                    if (!treeNode.Checked) shouldCheck = true;
                    treeNode.Checked = shouldCheck;
                    OnTreeViewNodeCheckedChanged(null, new(treeNode, TreeViewAction.ByMouse));
                }
            }
            allCheckBox.Checked = shouldCheck;
        }
    }
}