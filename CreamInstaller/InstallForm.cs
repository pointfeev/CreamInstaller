using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            if (value < userProgressBar.Value) return;
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

        private async Task OperateFor(ProgramSelection selection)
        {
            UpdateProgress(0);
            int count = selection.SteamApiDllDirectories.Count;
            int cur = 0;
            foreach (string directory in selection.SteamApiDllDirectories)
            {
                UpdateUser("Installing CreamAPI for " + selection.Name + $" in directory \"{directory}\" . . . ", LogColor.Operation);
                if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
                string api = directory + @"\steam_api.dll";
                string api_o = directory + @"\steam_api_o.dll";
                if (File.Exists(api) && !File.Exists(api_o))
                {
                    File.Move(api, api_o);
                    UpdateUser($"Renamed file: {api} -> steam_api_o.dll", LogColor.Resource);
                }
                if (File.Exists(api_o))
                {
                    Resources.WriteResourceToFile("steam_api.dll", api);
                    UpdateUser($"Wrote resource to file: {api}", LogColor.Resource);
                }
                string api64 = directory + @"\steam_api64.dll";
                string api64_o = directory + @"\steam_api64_o.dll";
                if (File.Exists(api64) && !File.Exists(api64_o))
                {
                    File.Move(api64, api64_o);
                    UpdateUser($"Renamed file: {api64} -> steam_api64_o.dll", LogColor.Resource);
                }
                if (File.Exists(api64_o))
                {
                    Resources.WriteResourceToFile("steam_api64.dll", api64);
                    UpdateUser($"Wrote resource to file: {api64}", LogColor.Resource);
                }
                string cApi = directory + @"\cream_api.ini";
                File.Create(cApi).Close();
                StreamWriter writer = File.AppendText(cApi);
                writer.WriteLine(";Created with CreamInstaller by pointfeev#4538");
                if (selection.SteamAppId > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("[steam]");
                    writer.WriteLine($"appid = {selection.SteamAppId}");
                    writer.WriteLine();
                    writer.WriteLine("[dlc]");
                    UpdateUser($"Added game to cream_api.ini with appid {selection.SteamAppId} ({selection.Name})", LogColor.Resource);
                    foreach (Tuple<int, string> dlcApp in selection.SelectedSteamDlc)
                    {
                        writer.WriteLine($"{dlcApp.Item1} = {dlcApp.Item2}");
                        UpdateUser($"Added DLC to cream_api.ini with appid {dlcApp.Item1} ({dlcApp.Item2})", LogColor.Resource);
                    }
                }
                foreach (Tuple<int, string, List<Tuple<int, string>>> extraAppDlc in selection.ExtraSteamAppIdDlc)
                {
                    writer.WriteLine();
                    writer.WriteLine("[steam]");
                    writer.WriteLine($"appid = {extraAppDlc.Item1}");
                    writer.WriteLine();
                    writer.WriteLine("[dlc]");
                    UpdateUser($"Added game to cream_api.ini with appid {extraAppDlc.Item1} ({extraAppDlc.Item2})", LogColor.Resource);
                    foreach (Tuple<int, string> dlcApp in extraAppDlc.Item3)
                    {
                        writer.WriteLine($"{dlcApp.Item1} = {dlcApp.Item2}");
                        UpdateUser($"Added DLC to cream_api.ini with appid {dlcApp.Item1} ({dlcApp.Item2})", LogColor.Resource);
                    }
                }
                writer.Flush();
                writer.Close();
                await Task.Run(() => Thread.Sleep(0)); // to keep the text box control from glitching
                UpdateProgress(++cur / count * 100);
            }
            UpdateProgress(100);
        }

        private async Task Operate()
        {
            OperationsCount = ProgramSelection.AllSafeEnabled.Count;
            CompleteOperationsCount = 0;
            foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            {
                if (!selection.Enabled) continue;
                if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
                try
                {
                    await OperateFor(selection);
                    UpdateUser($"Operation succeeded for {selection.Name}.", LogColor.Success);
                    selection.Enabled = false;
                }
                catch (Exception exception)
                {
                    UpdateUser($"Operation failed for {selection.Name}: " + exception.ToString(), LogColor.Error);
                }
                ++CompleteOperationsCount;
            }
            Program.Cleanup();
            List<ProgramSelection> FailedSelections = ProgramSelection.AllSafeEnabled;
            if (FailedSelections.Any())
            {
                if (FailedSelections.Count == 1)
                {
                    throw new CustomMessageException($"Operation failed for {FailedSelections.First().Name}.");
                }
                else
                {
                    throw new CustomMessageException($"Operation failed for {FailedSelections.Count} programs.");
                }
            }
        }

        private readonly int ProgramCount = ProgramSelection.AllSafeEnabled.Count;

        private async void Start()
        {
            acceptButton.Enabled = false;
            retryButton.Enabled = false;
            cancelButton.Enabled = true;
            reselectButton.Enabled = false;
            userProgressBar.Value = userProgressBar.Minimum;
            try
            {
                await Operate();
                UpdateUser("CreamAPI successfully installed for " + ProgramCount + " program(s).", LogColor.Success);
            }
            catch (Exception exception)
            {
                UpdateUser("CreamAPI installation failed: " + exception.ToString(), LogColor.Error);
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