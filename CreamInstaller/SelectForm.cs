using System;
using System.Windows.Forms;
using CG.Web.MegaApiClient;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Threading.Tasks;
using System.Drawing;

namespace CreamInstaller
{
    public partial class SelectForm : Form
    {
        public SelectForm()
        {
            InitializeComponent();
            Program.SelectForm = this;
            Text = Program.ApplicationName;
        }

        private List<string> gameLibraryDirectories;
        private List<string> GameLibraryDirectories
        {
            get
            {
                if (gameLibraryDirectories != null)
                {
                    return gameLibraryDirectories;
                }
                List<string> gameDirectories = new List<string>();
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
                gameLibraryDirectories = gameDirectories;
                return gameDirectories;
            }
        }

        private List<string> GetSteamApiDllDirectoriesFromGameDirectory(string gameDirectory, List<string> steamApiDllDirectories = null)
        {
            if (steamApiDllDirectories is null)
                steamApiDllDirectories = new();
            string file = gameDirectory + "\\steam_api64.dll";
            if (File.Exists(file) && !file.IsFilePathLocked())
            {
                steamApiDllDirectories.Add(gameDirectory);
            }
            foreach (string _directory in Directory.GetDirectories(gameDirectory))
            {
                GetSteamApiDllDirectoriesFromGameDirectory(_directory, steamApiDllDirectories);
            }
            return steamApiDllDirectories;
        }

        private string GetGameDirectoryFromLibraryDirectory(string gameName, string libraryDirectory)
        {
            if (Path.GetFileName(libraryDirectory) == gameName)
            {
                return libraryDirectory;
            }
            try
            {
                foreach (string _directory in Directory.GetDirectories(libraryDirectory))
                {
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

        private List<CheckBox> checkBoxes = new();
        private void GetCreamApiApplicablePrograms(IProgress<int> progress)
        {
            int maxProgress = 0;
            IEnumerable<INode> fileNodes = Program.MegaApiClient.GetNodesFromLink(new Uri("https://mega.nz/folder/45YBwIxZ#fsZNZZu9twY2PVLgrB86fA"));
            foreach (INode node in fileNodes)
            {
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
                if (node.Type == NodeType.Directory && node.Name != "CreamAPI" && node.Name != "Outdated")
                {
                    progress.Report(++curProgress);
                    string rootDirectory;
                    List<string> directories;
                    if (node.Name == "Paradox Launcher")
                    {
                        rootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        directories = GetSteamApiDllDirectoriesFromGameDirectory(rootDirectory + "\\Programs\\Paradox Interactive");
                    }
                    else
                    {
                        rootDirectory = null;
                        directories = null;
                        foreach (string libraryDirectory in GameLibraryDirectories)
                        {
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
                        flowLayoutPanel1.Invoke((MethodInvoker) delegate
                        {
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

                            checkBox.CheckedChanged += (sender, e) =>
                            {
                                if (checkBox.Checked)
                                {
                                    selection.Add();
                                }
                                else
                                {
                                    selection.Remove();
                                }

                                acceptButton.Enabled = Program.ProgramSelections.Count > 0;
                                if (acceptButton.Enabled)
                                    acceptButton.Focus();
                                else
                                    cancelButton.Focus();

                                allCheckBox.Checked = checkBoxes.TrueForAll(checkBox => checkBox.Checked);
                            };
                        });
                    }
                }
            }
            progress.Report(maxProgress);
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            label2.Text = "Finding CreamAPI-applicable programs . . . 0%";
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
                    int p = (int)((float)((float)_progress / (float)maxProgress) * 100);
                    label2.Text = "Finding CreamAPI-applicable programs . . . " + p + "% (" + _progress + "/" + maxProgress + ")";
                    progressBar1.Value = p;
                }
            };
            await Task.Run(() => GetCreamApiApplicablePrograms(progress));

            groupBox1.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height + 44);

            label2.Hide();
            progressBar1.Hide();

            allCheckBox.Enabled = true;
            foreach (CheckBox checkBox in checkBoxes)
                checkBox.Enabled = true;

            acceptButton.Enabled = true;
            acceptButton.Focus();
        }

        private void OnAccept(object sender, EventArgs e)
        {
            if (Program.ProgramSelections.Count > 0)
            {
                Hide();
                new InstallForm().ShowDialog();
                Close();
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Close();
        }

        private bool allCheckBoxChecked = true;
        private void OnAllCheckBoxMouseClick(object sender, EventArgs e)
        {
            allCheckBoxChecked = !allCheckBoxChecked;
            allCheckBox.Checked = allCheckBoxChecked;
            foreach (CheckBox checkBox in checkBoxes)
                checkBox.Checked = allCheckBoxChecked;
        }
    }
}
