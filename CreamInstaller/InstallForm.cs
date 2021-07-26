using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

namespace CreamInstaller
{
    public partial class InstallForm : Form
    {
        public InstallForm()
        {
            InitializeComponent();
            Program.InstallForm = this;
        }

        public void UpdateProgress(int progress)
        {
            userProgressBar.Value = progress;
        }

        public void UpdateUser(string text)
        {
            userInfoLabel.Text = text;
            if (logTextBox.IsDisposed == false)
            {
                logTextBox.AppendText(Environment.NewLine);
                logTextBox.AppendText(userInfoLabel.Text);
            }
        }

        private async Task Install()
        {
            foreach (ProgramSelection selection in Program.ProgramSelections)
            {
                if (Program.Canceled)
                    return;

                Program.Cleanup(false, false);

                UpdateProgress(0);
                UpdateUser("Downloading CreamAPI files for " + selection.ProgramName + " . . . ");
                Program.OutputFile = selection.ProgramDirectory + "\\" + selection.DownloadNode.Name;
                if (File.Exists(Program.OutputFile))
                {
                    try
                    {
                        File.Delete(Program.OutputFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new Exception("Unable to delete old CreamAPI archive file for " + selection.ProgramName + "!");
                    }
                }
                Progress<double> progress = new Progress<double>(delegate (double progress)
                {
                    if (!Program.Canceled)
                    {
                        UpdateUser($"Downloading CreamAPI files from MEGA . . . {(int)progress}%");
                        UpdateProgress((int)progress);
                    }
                });
                Program.CancellationTokenSource = new CancellationTokenSource();
                Program.OutputTask = Program.MegaApiClient.DownloadFileAsync(selection.DownloadNode, Program.OutputFile, progress, Program.CancellationTokenSource.Token);
                await Program.OutputTask;
                UpdateProgress(100);

                UpdateProgress(0);
                UpdateUser("Searching for CreamAPI files in downloaded archive . . . ");
                string resourcePath = null;
                List<ZipArchiveEntry> resources = new List<ZipArchiveEntry>();
                Program.OutputArchive = ZipFile.OpenRead(Program.OutputFile);
                int currentEntryCount = 0;
                foreach (ZipArchiveEntry entry in Program.OutputArchive.Entries)
                {
                    currentEntryCount++;
                    if (entry.Name == "steam_api64.dll")
                    {
                        resourcePath = Path.GetDirectoryName(entry.FullName);
                        UpdateUser("CreamAPI file path: " + resourcePath);
                    }
                    UpdateProgress((currentEntryCount / (Program.OutputArchive.Entries.Count * 2)) * 100);
                }
                foreach (ZipArchiveEntry entry in Program.OutputArchive.Entries)
                {
                    currentEntryCount++;
                    if (!string.IsNullOrEmpty(entry.Name) && Path.GetDirectoryName(entry.FullName) == resourcePath)
                    {
                        resources.Add(entry);
                        UpdateUser("Found CreamAPI file: " + entry.Name);
                    }
                    UpdateProgress((currentEntryCount / (Program.OutputArchive.Entries.Count * 2)) * 100);
                }
                if (resources.Count < 1)
                {
                    throw new Exception("Unable to find CreamAPI files in downloaded archive for " + selection.ProgramName + "!");
                }
                UpdateProgress(100);

                UpdateProgress(0);
                UpdateUser("Extracting CreamAPI files for " + selection.ProgramName + " . . . ");
                int currentFileCount = 0;
                foreach (string directory in selection.SteamApiDllDirectories)
                {
                    foreach (ZipArchiveEntry entry in resources)
                    {
                        currentFileCount++;
                        string file = directory + "\\" + entry.Name;
                        UpdateUser(file);
                        if (File.Exists(file))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                throw new Exception(selection.ProgramName + " is currently running!");
                            }
                        }
                        entry.ExtractToFile(file);
                        UpdateProgress((currentFileCount / (resources.Count * selection.SteamApiDllDirectories.Count)) * 100);
                    }
                }
                UpdateProgress(100);
            }
        }

        private async void Start()
        {
            Program.Canceled = false;
            acceptButton.Enabled = false;
            retryButton.Enabled = false;
            cancelButton.Enabled = true;
            userInfoLabel.Text = "Loading . . . ";
            logTextBox.Text = "Loading . . . ";
            string output;
            try
            {
                await Install();
                if (Program.ProgramSelections.Count > 1)
                    output = "CreamAPI successfully installed for " + Program.ProgramSelections.Count + " programs!";
                else
                    output = "CreamAPI successfully installed for " + Program.ProgramSelections.First().ProgramName + "!";
            }
            catch (Exception exception)
            {
                output = "Installation failed: " + exception.Message;
                retryButton.Enabled = true;
            }
            Program.Cleanup();
            UpdateUser(output);
            acceptButton.Enabled = true;
            cancelButton.Enabled = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Start();
        }

        private void OnAccept(object sender, EventArgs e)
        {
            Close();
        }

        private void OnRetry(object sender, EventArgs e)
        {
            Program.Cleanup(true, false);
            Start();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Program.Cleanup(true, false);
        }
    }
}
