using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

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

        private int OperationsCount;
        private int CompleteOperationsCount;

        public void UpdateProgress(int progress)
        {
            int value = (int)((float)(CompleteOperationsCount / (float)OperationsCount) * 100) + (progress / OperationsCount);
            if (value < userProgressBar.Value) { return; }
            userProgressBar.Value = value;
        }

        public void UpdateUser(string text, Color color, bool log = true)
        {
            userInfoLabel.Text = text;
            if (log && !logTextBox.IsDisposed)
            {
                if (logTextBox.Text.Length > 0)
                {
                    logTextBox.AppendText(Environment.NewLine, color);
                }

                logTextBox.AppendText(userInfoLabel.Text, color);
            }
        }

        private void OperateFor(ProgramSelection selection)
        {
            UpdateProgress(0);
            UpdateUser("Downloading CreamAPI files for " + selection.DisplayName + " . . . ", LogColor.Operation);
            UpdateUser($"Downloaded archive: {Program.OutputFile}", LogColor.Resource);
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
                UpdateProgress(currentEntryCount / (Program.OutputArchive.Entries.Count * 2) * 100);
            }
            foreach (ZipArchiveEntry entry in Program.OutputArchive.Entries)
            {
                currentEntryCount++;
                if (!string.IsNullOrEmpty(entry.Name) && Path.GetDirectoryName(entry.FullName) == resourcePath)
                {
                    resources.Add(entry);
                    UpdateUser("Found CreamAPI file: " + entry.Name, LogColor.Resource);
                }
                UpdateProgress(currentEntryCount / (Program.OutputArchive.Entries.Count * 2) * 100);
            }
            if (resources.Count < 1)
            {
                throw new CustomMessageException($"Unable to find CreamAPI files in downloaded archive: {Program.OutputFile}");
            }
            if (!Program.IsProgramRunningDialog(this, selection))
            {
                throw new OperationCanceledException();
            }
            UpdateUser("Installing CreamAPI files for " + selection.DisplayName + " . . . ", LogColor.Operation);
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
                        throw new CustomMessageException($"Unable to overwrite Steam API file: {file}");
                    }
                    UpdateUser("Installed file: " + file, LogColor.Resource);
                    UpdateProgress(currentFileCount / (resources.Count * selection.SteamApiDllDirectories.Count) * 100);
                }
                foreach (KeyValuePair<string, string> keyValuePair in changesToRevert)
                {
                    string file = keyValuePair.Key;
                    string backup = keyValuePair.Value;
                    if (!string.IsNullOrEmpty(backup))
                    {
                        File.Delete(backup);
                    }
                }
            }
            UpdateProgress(100);
        }

        private void Operate()
        {
            OperationsCount = Program.ProgramSelections.FindAll(selection => selection.Enabled).Count;
            CompleteOperationsCount = 0;

            foreach (ProgramSelection selection in Program.ProgramSelections.ToList())
            {
                if (!selection.Enabled) { continue; }

                if (!Program.IsProgramRunningDialog(this, selection))
                {
                    throw new OperationCanceledException();
                }

                try
                {
                    OperateFor(selection);
                    UpdateUser($"Operation succeeded for {selection.DisplayName}.", LogColor.Success);
                    selection.Toggle(false);
                }
                catch (Exception exception)
                {
                    UpdateUser($"Operation failed for {selection.DisplayName}: " + exception.ToString(), LogColor.Error);
                }

                ++CompleteOperationsCount;
            }

            Program.Cleanup();

            List<ProgramSelection> FailedSelections = Program.ProgramSelections.FindAll(selection => selection.Enabled);
            if (FailedSelections.Any())
            {
                if (FailedSelections.Count == 1)
                {
                    throw new CustomMessageException($"Operation failed for {FailedSelections.First().DisplayName}.");
                }
                else
                {
                    throw new CustomMessageException($"Operation failed for {FailedSelections.Count} programs.");
                }
            }
        }

        private readonly int ProgramCount = Program.ProgramSelections.FindAll(selection => selection.Enabled).Count;

        private void Start()
        {
            acceptButton.Enabled = false;
            retryButton.Enabled = false;
            cancelButton.Enabled = true;
            reselectButton.Enabled = false;
            userProgressBar.Value = userProgressBar.Minimum;
            try
            {
                Operate();
                UpdateUser("CreamAPI successfully downloaded and installed for " + ProgramCount + " program(s).", LogColor.Success);
            }
            catch (Exception exception)
            {
                UpdateUser("CreamAPI download and/or installation failed: " + exception.ToString(), LogColor.Error);
                retryButton.Enabled = true;
            }
            userProgressBar.Value = userProgressBar.Maximum;
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
            Program.Cleanup();
            Close();
        }

        private void OnRetry(object sender, EventArgs e)
        {
            Program.Cleanup();
            Start();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Program.Cleanup();
        }

        private void OnReselect(object sender, EventArgs e)
        {
            Program.Cleanup();
            Reselecting = true;
            Close();
        }
    }
}