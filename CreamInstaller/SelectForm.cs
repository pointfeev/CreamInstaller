using CG.Web.MegaApiClient;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
                if (Program.Canceled) { return gameDirectories; }
                string steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
                if (steamInstallPath == null)
                {
                    steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null) as string;
                }
                if (steamInstallPath != null)
                {
                    string mainLibraryFolder = steamInstallPath + "\\steamapps\\common";
                    gameDirectories.Add(mainLibraryFolder);
                    string libraryFolders = steamInstallPath + "\\steamapps\\libraryfolders.vdf";
                    VProperty property = VdfConvert.Deserialize(File.ReadAllText(libraryFolders));
                    foreach (VProperty _property in property.Value)
                    {
                        if (int.TryParse(_property.Key, out _))
                        {
                            gameDirectories.Add(_property.Value.ToString());
                        }
                    }
                }
                return gameDirectories;
            }
        }

        private List<string> GetSteamApiDllDirectoriesFromGameDirectory(string gameDirectory, List<string> steamApiDllDirectories = null)
        {
            if (Program.Canceled) { return null; }
            if (steamApiDllDirectories is null)
            {
                steamApiDllDirectories = new();
            }
            string file = gameDirectory + "\\steam_api64.dll";
            if (File.Exists(file))
            {
                steamApiDllDirectories.Add(gameDirectory);
            }
            foreach (string _directory in Directory.GetDirectories(gameDirectory))
            {
                if (Program.Canceled) { return null; }
                GetSteamApiDllDirectoriesFromGameDirectory(_directory, steamApiDllDirectories);
            }
            if (!steamApiDllDirectories.Any())
            {
                return null;
            }
            return steamApiDllDirectories;
        }

        private string GetGameDirectoryFromLibraryDirectory(string gameName, string libraryDirectory)
        {
            if (Program.Canceled) { return null; }
            if (Path.GetFileName(libraryDirectory) == gameName)
            {
                return libraryDirectory;
            }
            try
            {
                foreach (string _directory in Directory.GetDirectories(libraryDirectory))
                {
                    if (Program.Canceled) { return null; }
                    string dir = GetGameDirectoryFromLibraryDirectory(gameName, _directory);
                    if (dir != null)
                    {
                        return dir;
                    }
                }
            }
            catch { }
            return null;
        }

        private readonly List<CheckBox> checkBoxes = new();
        private void GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            if (Program.Canceled) { return; }
            int maxProgress = 0;
            IEnumerable<INode> fileNodes = Program.MegaApiClient.GetNodesFromLink(new Uri("https://mega.nz/folder/45YBwIxZ#fsZNZZu9twY2PVLgrB86fA"));
            foreach (INode node in fileNodes)
            {
                if (Program.Canceled) { return; }
                if (node.Type == NodeType.Directory && node.Name != "CreamAPI" && node.Name != "Outdated")
                {
                    ++maxProgress;
                }
            }
            progress.Report(maxProgress);
            int curProgress = 0;
            progress.Report(curProgress);
            foreach (INode node in fileNodes)
            {
                if (Program.Canceled) { return; }
                if (node.Type == NodeType.Directory && node.Name != "CreamAPI" && node.Name != "Outdated")
                {
                    progress.Report(++curProgress);
                    if (Program.ProgramSelections.Any(selection => selection.ProgramName == node.Name)) { continue; }
                    string rootDirectory;
                    List<string> directories = null;
                    if (node.Name == "Paradox Launcher")
                    {
                        rootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string launcherDirectory = rootDirectory + "\\Programs\\Paradox Interactive";
                        if (Directory.Exists(launcherDirectory))
                        {
                            directories = GetSteamApiDllDirectoriesFromGameDirectory(launcherDirectory);
                        }
                    }
                    else
                    {
                        rootDirectory = null;
                        directories = null;
                        foreach (string libraryDirectory in GameLibraryDirectories)
                        {
                            if (Program.Canceled) { return; }
                            rootDirectory = GetGameDirectoryFromLibraryDirectory(node.Name, libraryDirectory);
                            if (rootDirectory != null)
                            {
                                directories = GetSteamApiDllDirectoriesFromGameDirectory(rootDirectory);
                                break;
                            }
                        }
                    }
                    if (!(directories is null))
                    {
                        if (Program.Canceled) { return; }
                        flowLayoutPanel1.Invoke((MethodInvoker)delegate
                        {
                            if (Program.Canceled) { return; }
                        
                            ProgramSelection selection = new();
                            selection.ProgramName = node.Name;
                            selection.ProgramDirectory = rootDirectory;
                            selection.SteamApiDllDirectories = new();
                            selection.SteamApiDllDirectories.AddRange(directories);
                        
                            foreach (INode _node in fileNodes)
                            {
                                if (_node.Type == NodeType.File && _node.ParentId == node.Id)
                                {
                                    selection.DownloadNode = _node;
                                    break;
                                }
                            }
                        
                            CheckBox checkBox = new();
                            checkBoxes.Add(checkBox);
                            checkBox.AutoSize = true;
                            checkBox.Parent = flowLayoutPanel1;
                            checkBox.Text = node.Name;
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
                    }
                }
            }
            progress.Report(maxProgress);
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

            label2.Text = "Scanning for CreamAPI-applicable programs on your computer . . . ";
            int maxProgress = 0;
            Progress<int> progress = new();
            progress.ProgressChanged += (sender, _progress) =>
            {
                if (maxProgress == 0)
                {
                    maxProgress = _progress;
                }
                else
                {
                    int p = (int)((float)(_progress / (float)maxProgress) * 100);
                    label2.Text = "Scanning for CreamAPI-applicable programs on your computer . . . " + p + "% (" + _progress + "/" + maxProgress + ")";
                    progressBar1.Value = p;
                }
            };
            await Task.Run(() => GetCreamApiApplicablePrograms(progress));

            Program.ProgramSelections.ForEach(selection => selection.SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory)));
            Program.ProgramSelections.RemoveAll(selection => !Directory.Exists(selection.ProgramDirectory) || !selection.SteamApiDllDirectories.Any());
            foreach (CheckBox checkBox in checkBoxes)
            {
                if (!Program.ProgramSelections.Any(selection => selection.ProgramName == checkBox.Text))
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
                    int X = (installForm.Location.X + installForm.Size.Width / 2) - Size.Width / 2;
                    int Y = (installForm.Location.Y + installForm.Size.Height / 2) - Size.Height / 2;
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
            Program.Cleanup(logout: false);
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
