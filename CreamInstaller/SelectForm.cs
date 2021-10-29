using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            string api = "";//gameDirectory + "\\steam_api.dll";
            string api64 = gameDirectory + "\\steam_api64.dll";
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

        private readonly List<CheckBox> checkBoxes = new();

        private readonly Dictionary<int, string> dlc = new();

        [DllImport("kernel32")]
        private static extern bool AllocConsole();

        private void GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            if (Program.Canceled) return;
            List<Tuple<string, string>> applicablePrograms = new();
            string launcherRootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Programs\\Paradox Interactive";
            if (Directory.Exists(launcherRootDirectory)) applicablePrograms.Add(new Tuple<string, string>("Paradox Launcher", launcherRootDirectory));
            foreach (string libraryDirectory in GameLibraryDirectories)
                if (GetGameDirectoriesFromLibraryDirectory(libraryDirectory, out List<string> gameDirectories))
                    foreach (string gameDirectory in gameDirectories)
                        applicablePrograms.Add(new Tuple<string, string>(Path.GetFileName(gameDirectory) ?? "unknown_" + applicablePrograms.Count, gameDirectory));
            List<Task> tasks = new();
            foreach (Tuple<string, string> program in applicablePrograms)
            {
                string name = program.Item1;
                string rootDirectory = program.Item2;
                if (Program.Canceled) return;
                Task task = new Task(() =>
                {
                    int steamAppId = 0;
                    if (Program.Canceled || Program.ProgramSelections.Any(s => s.Name == name)
                    || (name != "Paradox Launcher" && !GetSteamAppIdFromGameDirectory(rootDirectory, out steamAppId))
                    || !GetSteamApiDllDirectoriesFromGameDirectory(rootDirectory, out List<string> steamApiDllDirectories))
                        return;

                    Dictionary<string, string> appInfo = null;
                    if (Program.Canceled || (name != "Paradox Launcher" && !SteamCMD.GetAppInfo(steamAppId, out appInfo))) return;
                    string list = null;
                    if (!(appInfo is null) && appInfo.TryGetValue("listofdlc", out list))
                    {
                        if (Program.Canceled) return;
                        string[] nums = list.Split(",");
                        List<int> ids = new();
                        foreach (string s in nums) ids.Add(int.Parse(s));
                        foreach (int id in ids)
                        {
                            if (Program.Canceled) return;
                            string dlcName = null;
                            Dictionary<string, string> dlcAppInfo = null;
                            //if (SteamCMD.GetAppInfo(id, out dlcAppInfo)) dlcAppInfo.TryGetValue("name", out dlcName);
                            dlc.Add(id, dlcName);
                            Console.WriteLine(id + " = " + dlcName);
                        }
                    }
                    else if (name != "Paradox Launcher") return;

                    ProgramSelection selection = new();
                    selection.Name = name;
                    string displayName = name;
                    if (!(appInfo is null)) appInfo.TryGetValue("name", out displayName);
                    selection.DisplayName = displayName;
                    selection.RootDirectory = rootDirectory;
                    selection.SteamAppId = steamAppId;
                    selection.SteamApiDllDirectories = steamApiDllDirectories;

                    flowLayoutPanel1.Invoke((MethodInvoker)delegate
                    {
                        CheckBox checkBox = new();
                        checkBoxes.Add(checkBox);
                        checkBox.AutoSize = true;
                        checkBox.Parent = flowLayoutPanel1;
                        checkBox.Text = selection.DisplayName;
                        checkBox.Checked = true;
                        checkBox.Enabled = false;
                        checkBox.TabStop = true;
                        checkBox.TabIndex = 1 + checkBoxes.Count;

                        checkBox.CheckedChanged += (sender, e) =>
                        {
                            selection.Toggle(checkBox.Checked);
                            acceptButton.Enabled = Program.ProgramSelections.Any(selection => selection.Enabled);
                            allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
                            allCheckBox.Checked = checkBoxes.TrueForAll(checkBox => checkBox.Checked);
                            allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
                        };
                    });
                });
                tasks.Add(task);
                task.Start();
            }
            int max = tasks.Count;
            progress.Report(max);
            int cur = 0;
            progress.Report(cur);
            foreach (Task task in tasks)
            {
                progress.Report(cur++);
                task.Wait();
            }
            progress.Report(max);
        }

        private async void OnLoad()
        {
            Program.Canceled = false;
            cancelButton.Enabled = true;
            scanButton.Enabled = false;
            noneFoundLabel.Visible = false;
            allCheckBox.Enabled = false;
            acceptButton.Enabled = false;
            checkBoxes.ForEach(checkBox => checkBox.Enabled = false);

            label2.Visible = true;
            progressBar1.Visible = true;
            progressBar1.Value = 0;
            groupBox1.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height - 44);

            AllocConsole();

            bool setup = true;
            int maxProgress = 0;
            Progress<int> progress = new();
            IProgress<int> iProgress = progress;
            progress.ProgressChanged += (sender, _progress) =>
            {
                if (maxProgress == 0) maxProgress = _progress;
                else
                {
                    int p = Math.Max(Math.Min((int)((float)(_progress / (float)maxProgress) * 100), 100), 0);
                    if (setup) label2.Text = "Setting up SteamCMD . . . " + p + "% (" + _progress + "/" + maxProgress + ")";
                    else label2.Text = "Scanning for CreamAPI-applicable programs on your computer . . . " + p + "% (" + _progress + "/" + maxProgress + ")";
                    progressBar1.Value = p;
                }
            };

            int max = 1660; // not exact, number varies
            iProgress.Report(max);
            int cur = 0;
            iProgress.Report(cur);
            label2.Text = "Setting up SteamCMD . . . ";
            FileSystemWatcher watcher = new FileSystemWatcher(SteamCMD.DirectoryPath);
            watcher.Changed += (sender, e) => iProgress.Report(++cur);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            await Task.Run(() => SteamCMD.Setup());
            watcher.Dispose();
            Clipboard.SetText(cur.ToString());

            maxProgress = 0;
            setup = false;
            label2.Text = "Scanning for CreamAPI-applicable programs on your computer . . . ";
            await Task.Run(() => GetCreamApiApplicablePrograms(iProgress));

            Program.ProgramSelections.ForEach(selection => selection.SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory)));
            Program.ProgramSelections.RemoveAll(selection => !Directory.Exists(selection.RootDirectory) || !selection.SteamApiDllDirectories.Any());
            foreach (CheckBox checkBox in checkBoxes)
            {
                if (!Program.ProgramSelections.Any(selection => selection.DisplayName == checkBox.Text))
                {
                    checkBox.Dispose();
                }
            }

            progressBar1.Value = 100;
            groupBox1.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height + 44);
            label2.Visible = false;
            progressBar1.Visible = false;

            if (Program.ProgramSelections.Any())
            {
                allCheckBox.Enabled = true;
                checkBoxes.ForEach(checkBox => checkBox.Enabled = true);
                if (Program.ProgramSelections.Any(selection => selection.Enabled))
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

        private void OnLoad(object sender, EventArgs e)
        {
            OnLoad();
        }

        private void OnAccept(object sender, EventArgs e)
        {
            if (Program.ProgramSelections.Count > 0)
            {
                foreach (ProgramSelection selection in Program.ProgramSelections)
                {
                    if (!Program.IsProgramRunningDialog(this, selection))
                    {
                        return;
                    }
                }

                Hide();
                InstallForm installForm = new InstallForm(this);
                installForm.ShowDialog();
                if (installForm.Reselecting)
                {
                    foreach (CheckBox checkBox in checkBoxes)
                    {
                        checkBox.Checked = !checkBox.Checked;
                        checkBox.Checked = !checkBox.Checked; // to fire CheckChanged
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
            foreach (CheckBox checkBox in checkBoxes)
            {
                if (!checkBox.Checked)
                {
                    shouldCheck = true;
                }
            }
            foreach (CheckBox checkBox in checkBoxes)
            {
                checkBox.Checked = shouldCheck;
            }
            allCheckBox.Checked = shouldCheck;
        }
    }
}