using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace CreamInstaller
{
    public partial class InstallForm : Form
    {
        public bool Reselecting = false;

        public InstallForm(IWin32Window owner)
        {
            Owner = owner as Form;
            InitializeComponent();
            Program.InstallForm = this;
            Text = Program.ApplicationName;
            logTextBox.BackColor = LogColor.Background;
        }

        public void UpdateProgress(int progress)
        {
            Program.UpdateProgressInstantly(userProgressBar, progress);
        }

        public void UpdateUser(string text, Color color, bool log = true)
        {
            userInfoLabel.Text = text;
            if (log && !logTextBox.IsDisposed)
            {
                if (logTextBox.Text.Length > 0)
                    logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(userInfoLabel.Text, color);
            }
        }

        private async Task OperateFor(ProgramSelection selection)
        {
            UpdateProgress(0);
            UpdateUser("Downloading CreamAPI files for " + selection.ProgramName + " . . . ", LogColor.Operation);
            Program.OutputFile = selection.ProgramDirectory + "\\" + selection.DownloadNode.Name;
            if (File.Exists(Program.OutputFile))
            {
                try
                {
                    File.Delete(Program.OutputFile);
                }
                catch
                {
                    throw new Exception($"Unable to delete old archive file: {Program.OutputFile}");
                }
            }
            Progress<double> progress = new Progress<double>(delegate (double progress)
            {
                if (!Program.Canceled)
                {
                    UpdateUser($"Downloading CreamAPI files for {selection.ProgramName} . . . {(int)progress}%", LogColor.Operation, log: false);
                    UpdateProgress((int)progress);
                }
            });
            Program.CancellationTokenSource = new CancellationTokenSource();
            Program.OutputTask = Program.MegaApiClient.DownloadFileAsync(selection.DownloadNode, Program.OutputFile, progress, Program.CancellationTokenSource.Token);
            await Program.OutputTask;
            UpdateUser($"Downloaded file: {Program.OutputFile}", LogColor.Resource);
            UpdateProgress(100);

            UpdateProgress(0);
            UpdateUser("Searching for CreamAPI files in downloaded archive . . . ", LogColor.Operation);
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
                    UpdateUser("Got CreamAPI file path: " + resourcePath, LogColor.Resource);
                }
                UpdateProgress((currentEntryCount / (Program.OutputArchive.Entries.Count * 2)) * 100);
            }
            foreach (ZipArchiveEntry entry in Program.OutputArchive.Entries)
            {
                currentEntryCount++;
                if (!string.IsNullOrEmpty(entry.Name) && Path.GetDirectoryName(entry.FullName) == resourcePath)
                {
                    resources.Add(entry);
                    UpdateUser("Found CreamAPI file: " + entry.Name, LogColor.Resource);
                }
                UpdateProgress((currentEntryCount / (Program.OutputArchive.Entries.Count * 2)) * 100);
            }
            if (resources.Count < 1)
            {
                throw new Exception($"Unable to find CreamAPI files in downloaded archive: {Program.OutputFile}");
            }
            UpdateProgress(100);

            if (!Program.IsProgramRunningDialog(this, selection))
                throw new OperationCanceledException();

            UpdateProgress(0);
            UpdateUser("Installing CreamAPI files for " + selection.ProgramName + " . . . ", LogColor.Operation);
            int currentFileCount = 0;
            foreach (string directory in selection.SteamApiDllDirectories)
            {
                Dictionary<string, string> changesToRevert = new();
                foreach (ZipArchiveEntry entry in resources)
                {
                    currentFileCount++;
                    string file = directory + "\\" + entry.Name;
                    if (File.Exists(file))
                    {
                        string backup = file + Program.BackupFileExtension;
                        File.Copy(file, backup, true);
                        changesToRevert.Add(file, backup);
                    }
                    else
                    {
                        changesToRevert.Add(file, string.Empty);
                    }
                    try
                    {
                        entry.ExtractToFile(file, true);
                    }
                    catch
                    {
                        foreach (KeyValuePair<string, string> keyValuePair in changesToRevert)
                        {
                            file = keyValuePair.Key;
                            string backup = keyValuePair.Value;
                            if (string.IsNullOrEmpty(backup))
                            {
                                File.Delete(file);
                                UpdateUser("Deleted CreamAPI file: " + file, LogColor.Warning);
                            }
                            else if (file.IsFilePathLocked())
                            {
                                File.Delete(backup);
                            }
                            else
                            {
                                File.Move(backup, file, true);
                                UpdateUser("Reversed changes to Steam API file: " + file, LogColor.Warning);
                            }
                        }
                        throw new Exception($"Unable to overwrite Steam API file: {file}");
                    }
                    UpdateUser("Installed file: " + file, LogColor.Resource);
                    UpdateProgress((currentFileCount / (resources.Count * selection.SteamApiDllDirectories.Count)) * 100);
                }
                foreach (KeyValuePair<string, string> keyValuePair in changesToRevert)
                {
                    string file = keyValuePair.Key;
                    string backup = keyValuePair.Value;
                    if (!string.IsNullOrEmpty(backup))
                        File.Delete(backup);
                }
            }
            UpdateProgress(100);
        }

        private async Task Operate()
        {
            foreach (ProgramSelection selection in Program.ProgramSelections.ToList())
            {
                if (Program.Canceled)
                    break;

                Program.Cleanup(cancel: false, logout: false);

                if (!Program.IsProgramRunningDialog(this, selection))
                    throw new OperationCanceledException();

                try
                {
                    await OperateFor(selection);

                    UpdateUser($"Operation succeeded for {selection.ProgramName}.", LogColor.Success);
                    selection.Remove();
                }
                catch (Exception exception)
                {
                    UpdateUser($"Operation failed for {selection.ProgramName}: " + exception.Message, LogColor.Error);
                }
            }

            Program.Cleanup(logout: false);

            if (Program.ProgramSelections.Count == 1)
            {
                throw new Exception($"Operation failed for {Program.ProgramSelections.First().ProgramName}.");
            }
            else if (Program.ProgramSelections.Count > 1)
            {
                throw new Exception($"Operation failed for {Program.ProgramSelections.Count} programs.");
            }
        }

        private int ProgramCount = Program.ProgramSelections.Count;

        private async void Start()
        {
            Program.Canceled = false;
            acceptButton.Enabled = false;
            retryButton.Enabled = false;
            cancelButton.Enabled = true;
            reselectButton.Enabled = false;
            try
            {
                await Operate();
                UpdateUser("CreamAPI successfully downloaded and installed for " + ProgramCount + " program(s).", LogColor.Success);
            }
            catch (Exception exception)
            {
                UpdateUser("CreamAPI download and/or installation failed: " + exception.Message, LogColor.Error);
                retryButton.Enabled = true;
            }
            acceptButton.Enabled = true;
            cancelButton.Enabled = false;
            reselectButton.Enabled = true;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            userInfoLabel.Text = "Loading . . . ";
            logTextBox.Text = string.Empty;
            Start();
        }

        private void OnAccept(object sender, EventArgs e)
        {
            Program.Cleanup(logout: false);
            Close();
        }

        private void OnRetry(object sender, EventArgs e)
        {
            Program.Cleanup(logout: false);
            Start();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Program.Cleanup(logout: false);
        }

        private void OnReselect(object sender, EventArgs e)
        {
            Program.Cleanup(logout: false);
            Reselecting = true;
            Close();
        }
    }
}
