using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        }

        private List<string> GameLibraryDirectories
        {
            get
            {
                List<string> gameDirectories = new List<string>();
                if (Program.Canceled) return gameDirectories;
                string steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
                if (steamInstallPath == null) steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null) as string;
                if (steamInstallPath != null)
                {
                    string mainLibraryFolder = steamInstallPath + "\\steamapps\\common";
                    gameDirectories.Add(mainLibraryFolder);
                    string libraryFolders = steamInstallPath + "\\steamapps\\libraryfolders.vdf";
                    VProperty property = VdfConvert.Deserialize(File.ReadAllText(libraryFolders));
                    foreach (VProperty _property in property.Value)
                        if (int.TryParse(_property.Key, out _) && Directory.Exists(_property.Value.ToString()))
                            gameDirectories.Add(_property.Value.ToString());
                }
                return gameDirectories;
            }
        }

        private bool GetSteamApiDllDirectoriesFromGameDirectory(string gameDirectory, out List<string> steamApiDllDirectories)
        {
            steamApiDllDirectories = new();
            if (Program.Canceled) return false;
            string api = gameDirectory + @"\steam_api.dll";
            string api64 = gameDirectory + @"\steam_api64.dll";
            if (File.Exists(api) || File.Exists(api64)) steamApiDllDirectories.Add(gameDirectory);
            foreach (string _directory in Directory.GetDirectories(gameDirectory))
            {
                if (Program.Canceled) return false;
                try
                {
                    if (GetSteamApiDllDirectoriesFromGameDirectory(_directory, out List<string> _steamApiDllDirectories))
                        steamApiDllDirectories.AddRange(_steamApiDllDirectories);
                }
                catch { }
            }
            if (!steamApiDllDirectories.Any()) return false;
            return true;
        }

        private bool GetSteamAppIdFromGameDirectory(string gameDirectory, out int appId)
        {
            appId = 0;
            if (Program.Canceled) return false;
            string file = gameDirectory + "\\steam_appid.txt";
            if (File.Exists(file) && int.TryParse(File.ReadAllText(file), out appId)) return true;
            foreach (string _directory in Directory.GetDirectories(gameDirectory))
            {
                if (Program.Canceled) return false;
                if (GetSteamAppIdFromGameDirectory(_directory, out appId)) return true;
            }
            return false;
        }

        private bool GetGameDirectoriesFromLibraryDirectory(string libraryDirectory, out List<string> gameDirectories)
        {
            gameDirectories = new();
            if (Program.Canceled) return false;
            foreach (string _directory in Directory.GetDirectories(libraryDirectory))
            {
                if (Program.Canceled) return false;
                if (Directory.Exists(_directory)) gameDirectories.Add(_directory);
            }
            if (!gameDirectories.Any()) return false;
            return true;
        }

        private readonly List<TreeNode> treeNodes = new();

        internal readonly Dictionary<int, List<Tuple<int, string>>> DLC = new();

        internal List<Task> RunningTasks = null;

        private void GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            int cur = 0;
            if (Program.Canceled) return;
            List<Tuple<string, string>> applicablePrograms = new();
            string launcherRootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Programs\\Paradox Interactive";
            if (Directory.Exists(launcherRootDirectory)) applicablePrograms.Add(new Tuple<string, string>("Paradox Launcher", launcherRootDirectory));
            foreach (string libraryDirectory in GameLibraryDirectories)
                if (GetGameDirectoriesFromLibraryDirectory(libraryDirectory, out List<string> gameDirectories))
                    foreach (string gameDirectory in gameDirectories)
                        applicablePrograms.Add(new Tuple<string, string>(Path.GetFileName(gameDirectory) ?? "unknown_" + applicablePrograms.Count, gameDirectory));
            RunningTasks = new();
            foreach (Tuple<string, string> program in applicablePrograms)
            {
                string identifier = program.Item1;
                string rootDirectory = program.Item2;
                if (Program.Canceled) return;
                Task task = new(() =>
                {
                    try
                    {
                        int steamAppId = 0;
                        if (Program.Canceled
                        || (identifier != "Paradox Launcher" && !GetSteamAppIdFromGameDirectory(rootDirectory, out steamAppId))
                        || !GetSteamApiDllDirectoriesFromGameDirectory(rootDirectory, out List<string> steamApiDllDirectories))
                            return;

                        Dictionary<string, string> appInfo = null;
                        if (Program.Canceled || (identifier != "Paradox Launcher" && !SteamCMD.GetAppInfo(steamAppId, out appInfo))) return;
                        string list = null;
                        List<Tuple<int, string>> dlc = null;
                        if (!DLC.TryGetValue(steamAppId, out dlc))
                        {
                            dlc = new();
                            DLC.Add(steamAppId, dlc);
                        }
                        if (Program.Canceled) return;
                        List<Task> dlcTasks = new();
                        if (!(appInfo is null) && appInfo.TryGetValue("listofdlc", out list))
                        {
                            if (Program.Canceled) return;
                            string[] nums = Regex.Replace(list, "[^0-9,]+", "").Split(",");
                            List<int> ids = new();
                            foreach (string s in nums)
                            {
                                if (Program.Canceled) return;
                                ids.Add(int.Parse(s));
                            }
                            Task task = new(() =>
                            {
                                try
                                {
                                    foreach (int id in ids)
                                    {
                                        if (Program.Canceled) return;
                                        string dlcName = null;
                                        Dictionary<string, string> dlcAppInfo = null;
                                        if (SteamCMD.GetAppInfo(id, out dlcAppInfo)) dlcAppInfo.TryGetValue("name", out dlcName);
                                        if (Program.Canceled) return;
                                        if (string.IsNullOrWhiteSpace(dlcName)) dlcName = "Unknown DLC";
                                        dlc.Add(new Tuple<int, string>(id, dlcName));
                                    }
                                }
                                catch { }
                            });
                            dlcTasks.Add(task);
                            RunningTasks.Add(task);
                            task.Start();
                            progress.Report(-RunningTasks.Count);
                        }
                        else if (identifier != "Paradox Launcher") return;
                        if (Program.Canceled) return;

                        if (string.IsNullOrWhiteSpace(identifier)) return;
                        string displayName = identifier;
                        if (!(appInfo is null)) appInfo.TryGetValue("name", out displayName);
                        if (string.IsNullOrWhiteSpace(displayName)) displayName = "Unknown Game";
                        if (Program.Canceled) return;

                        ProgramSelection selection = ProgramSelection.FromIdentifier(identifier) ?? new();
                        selection.Identifier = identifier;
                        selection.DisplayName = displayName;
                        selection.RootDirectory = rootDirectory;
                        selection.SteamAppId = steamAppId;
                        selection.SteamApiDllDirectories = steamApiDllDirectories;
                        selection.AppInfo = appInfo;

                        foreach (Task task in dlcTasks.ToList())
                        {
                            if (Program.Canceled) return;
                            progress.Report(cur++);
                            task.Wait();
                        }
                        if (Program.Canceled) return;
                        treeView1.Invoke((MethodInvoker)delegate
                        {
                            if (Program.Canceled) return;
                            TreeNode programNode = treeNodes.Find(s => s.Text == displayName) ?? new();
                            programNode.Text = displayName;
                            programNode.Remove();
                            treeView1.Nodes.Add(programNode);
                            treeNodes.Remove(programNode);
                            treeNodes.Add(programNode);
                            foreach (Tuple<int, string> dlcApp in dlc.ToList())
                            {
                                if (Program.Canceled || programNode is null) return;
                                TreeNode dlcNode = treeNodes.Find(s => s.Text == dlcApp.Item2) ?? new();
                                dlcNode.Text = dlcApp.Item2;
                                dlcNode.Remove();
                                programNode.Nodes.Add(dlcNode);
                                treeNodes.Remove(dlcNode);
                                treeNodes.Add(dlcNode);
                                selection.AllSteamDlc.Add(dlcApp);
                                selection.SelectedSteamDlc.Add(dlcApp);
                            }
                        });
                    }
                    catch { }
                });
                RunningTasks.Add(task);
                task.Start();
            }
            progress.Report(-RunningTasks.Count);
            progress.Report(cur);
            foreach (Task task in RunningTasks.ToList())
            {
                if (Program.Canceled) return;
                progress.Report(cur++);
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
            treeView1.CheckBoxes = false;

            label2.Visible = true;
            progressBar1.Visible = true;
            progressBar1.Value = 0;
            groupBox1.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height - 44);

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
                if (setup) label2.Text = "Setting up SteamCMD . . . " + p + "%";
                else label2.Text = "Gathering your CreamAPI-applicable games and their DLCs . . . " + p + "%";
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
            label2.Text = "Gathering your CreamAPI-applicable games and their DLCs . . . ";
            await Task.Run(() => GetCreamApiApplicablePrograms(iProgress));

            ProgramSelection.All.ForEach(selection => selection.SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory)));
            ProgramSelection.All.RemoveAll(selection => !Directory.Exists(selection.RootDirectory) || !selection.SteamApiDllDirectories.Any());
            foreach (TreeNode treeNode in treeNodes)
            {
                if (treeNode.Parent is null && ProgramSelection.FromDisplayName(treeNode.Text) is null)
                {
                    treeNode.Remove();
                }
            }

            progressBar1.Value = 100;
            groupBox1.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height + 44);
            label2.Visible = false;
            progressBar1.Visible = false;

            if (ProgramSelection.All.Any())
            {
                allCheckBox.Enabled = true;
                treeView1.CheckBoxes = true;
                treeView1.ExpandAll();
                treeNodes.ForEach(node => node.Checked = true);
                if (ProgramSelection.AllSafeEnabled.Any())
                {
                    acceptButton.Enabled = true;
                }
            }
            else
            {
                noneFoundLabel.Visible = true;
            }

            cancelButton.Enabled = false;
            scanButton.Enabled = true;
        }

        private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
        {
            ProgramSelection selection = ProgramSelection.FromDisplayName(e.Node.Text);
            if (selection is null)
            {
                selection = ProgramSelection.FromDisplayName(e.Node.Parent.Text);
                treeView1.AfterCheck -= OnTreeViewNodeCheckedChanged;
                e.Node.Parent.Checked = e.Node.Parent.Nodes.Cast<TreeNode>().ToList().Any(treeNode => treeNode.Checked);
                treeView1.AfterCheck += OnTreeViewNodeCheckedChanged;
                selection.ToggleDlc(e.Node.Text, e.Node.Checked);
            }
            else
            {
                selection.Enabled = e.Node.Checked;
                treeView1.AfterCheck -= OnTreeViewNodeCheckedChanged;
                e.Node.Nodes.Cast<TreeNode>().ToList().ForEach(treeNode => treeNode.Checked = e.Node.Checked);
                treeView1.AfterCheck += OnTreeViewNodeCheckedChanged;
                acceptButton.Enabled = ProgramSelection.AllSafeEnabled.Any();
                allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
                allCheckBox.Checked = treeNodes.TrueForAll(treeNode => treeNode.Checked);
                allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            treeView1.BeforeCollapse += (sender, e) => e.Cancel = true;
            treeView1.AfterCheck += OnTreeViewNodeCheckedChanged;
            OnLoad();
        }

        private void OnAccept(object sender, EventArgs e)
        {
            if (ProgramSelection.All.Count > 0)
            {
                foreach (ProgramSelection selection in ProgramSelection.AllSafe)
                {
                    if (!Program.IsProgramRunningDialog(this, selection))
                    {
                        return;
                    }
                }

                ProgramSelection paradoxLauncher = ProgramSelection.FromIdentifier("Paradox Launcher");
                if (!(paradoxLauncher is null))
                {
                    paradoxLauncher.ExtraSteamAppIdDlc = new();
                    foreach (ProgramSelection selection in ProgramSelection.AllSafeEnabled)
                    {
                        if (selection.Identifier == paradoxLauncher.Identifier) continue;
                        if (!selection.AppInfo.TryGetValue("publisher", out string publisher) || publisher != "Paradox Interactive") continue;
                        paradoxLauncher.ExtraSteamAppIdDlc.Add(new(selection.SteamAppId, selection.DisplayName, selection.SelectedSteamDlc));
                    }
                }

                Hide();
                InstallForm installForm = new InstallForm(this);
                installForm.ShowDialog();
                if (installForm.Reselecting)
                {
                    foreach (TreeNode treeNode in treeNodes)
                    {
                        treeNode.Checked = !treeNode.Checked;
                        treeNode.Checked = !treeNode.Checked; // to fire checked event
                    }
                    int X = installForm.Location.X + installForm.Size.Width / 2 - Size.Width / 2;
                    int Y = installForm.Location.Y + installForm.Size.Height / 2 - Size.Height / 2;
                    Location = new Point(X, Y);
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
                if (treeNode.Parent is null && !treeNode.Checked)
                    shouldCheck = true;
            foreach (TreeNode treeNode in treeNodes)
                if (treeNode.Parent is null)
                    treeNode.Checked = shouldCheck;
            allCheckBox.Checked = shouldCheck;
        }
    }
}