using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreamInstaller;

internal partial class InstallForm : CustomForm
{
    internal bool Reselecting = false;
    internal bool Uninstalling = false;

    internal InstallForm(IWin32Window owner, bool uninstall = false) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
        Program.InstallForm = this;
        logTextBox.BackColor = InstallationLog.Background;
        Uninstalling = uninstall;
    }

    private int OperationsCount;
    private int CompleteOperationsCount;

    internal void UpdateProgress(int progress)
    {
        int value = (int)((float)(CompleteOperationsCount / (float)OperationsCount) * 100) + (progress / OperationsCount);
        if (value < userProgressBar.Value) return;
        userProgressBar.Value = value;
    }

    internal async Task UpdateUser(string text, Color color, bool info = true, bool log = true)
    {
        if (info) userInfoLabel.Text = text;
        if (log && !logTextBox.IsDisposed)
        {
            if (logTextBox.Text.Length > 0) logTextBox.AppendText(Environment.NewLine, color);
            logTextBox.AppendText(text, color);
        }
        await Task.Run(() => Thread.Sleep(0)); // to keep the text box control from glitching
    }

    internal async Task WriteConfiguration(StreamWriter writer, int steamAppId, string name, SortedList<int, string> steamDlcApps)
    {
        writer.WriteLine();
        writer.WriteLine($"; {name}");
        writer.WriteLine("[steam]");
        writer.WriteLine($"appid = {steamAppId}");
        writer.WriteLine();
        writer.WriteLine("[dlc]");
        await UpdateUser($"Added game to cream_api.ini with appid {steamAppId} ({name})", InstallationLog.Resource, info: false);
        foreach (KeyValuePair<int, string> dlcApp in steamDlcApps)
        {
            writer.WriteLine($"{dlcApp.Key} = {dlcApp.Value}");
            await UpdateUser($"Added DLC to cream_api.ini with appid {dlcApp.Key} ({dlcApp.Value})", InstallationLog.Resource, info: false);
        }
    }

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.SteamApiDllDirectories.Count;
        int cur = 0;
        foreach (string directory in selection.SteamApiDllDirectories)
        {
            await UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} CreamAPI for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
            if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
            string api = directory + @"\steam_api.dll";
            string api_o = directory + @"\steam_api_o.dll";
            string api64 = directory + @"\steam_api64.dll";
            string api64_o = directory + @"\steam_api64_o.dll";
            string cApi = directory + @"\cream_api.ini";
            if (Uninstalling)
            {
                if (File.Exists(api_o))
                {
                    if (File.Exists(api))
                    {
                        File.Delete(api);
                        await UpdateUser($"Deleted file: {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
                    }
                    File.Move(api_o, api);
                    await UpdateUser($"Renamed file: {Path.GetFileName(api_o)} -> {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
                }
                if (File.Exists(api64_o))
                {
                    if (File.Exists(api64))
                    {
                        File.Delete(api64);
                        await UpdateUser($"Deleted file: {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
                    }
                    File.Move(api64_o, api64);
                    await UpdateUser($"Renamed file: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
                }
                if (File.Exists(cApi))
                {
                    File.Delete(cApi);
                    await UpdateUser($"Deleted file: {Path.GetFileName(cApi)}", InstallationLog.Resource, info: false);
                }
            }
            else
            {
                if (File.Exists(api) && !File.Exists(api_o))
                {
                    File.Move(api, api_o);
                    await UpdateUser($"Renamed file: {Path.GetFileName(api)} -> {Path.GetFileName(api_o)}", InstallationLog.Resource, info: false);
                }
                if (File.Exists(api_o))
                {
                    Properties.Resources.API.Write(api);
                    await UpdateUser($"Wrote resource to file: {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
                }
                if (File.Exists(api64) && !File.Exists(api64_o))
                {
                    File.Move(api64, api64_o);
                    await UpdateUser($"Renamed file: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", InstallationLog.Resource, info: false);
                }
                if (File.Exists(api64_o))
                {
                    Properties.Resources.API64.Write(api64);
                    await UpdateUser($"Wrote resource to file: {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
                }
                await UpdateUser("Generating CreamAPI for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(cApi).Close();
                StreamWriter writer = new(cApi, true, Encoding.UTF8);
                writer.WriteLine("; " + Application.CompanyName + " v" + Application.ProductVersion);
                if (selection.SteamAppId > 0) await WriteConfiguration(writer, selection.SteamAppId, selection.Name, selection.SelectedSteamDlc);
                foreach (Tuple<int, string, SortedList<int, string>> extraAppDlc in selection.ExtraSteamAppIdDlc)
                    await WriteConfiguration(writer, extraAppDlc.Item1, extraAppDlc.Item2, extraAppDlc.Item3);
                writer.Flush();
                writer.Close();
            }
            UpdateProgress(++cur / count * 100);
        }
        UpdateProgress(100);
    }

    private async Task Operate()
    {
        List<ProgramSelection> programSelections = ProgramSelection.AllUsableEnabled;
        OperationsCount = programSelections.Count;
        CompleteOperationsCount = 0;
        List<ProgramSelection> disabledSelections = new();
        foreach (ProgramSelection selection in programSelections)
        {
            if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
            try
            {
                await OperateFor(selection);
                await UpdateUser($"Operation succeeded for {selection.Name}.", InstallationLog.Success);
                selection.Enabled = false;
                disabledSelections.Add(selection);
            }
            catch (Exception exception)
            {
                await UpdateUser($"Operation failed for {selection.Name}: " + exception.ToString(), InstallationLog.Error);
            }
            ++CompleteOperationsCount;
        }
        Program.Cleanup();
        List<ProgramSelection> FailedSelections = ProgramSelection.AllUsableEnabled;
        if (FailedSelections.Any())
            if (FailedSelections.Count == 1) throw new CustomMessageException($"Operation failed for {FailedSelections.First().Name}.");
            else throw new CustomMessageException($"Operation failed for {FailedSelections.Count} programs.");
        foreach (ProgramSelection selection in disabledSelections) selection.Enabled = true;
    }

    private readonly int ProgramCount = ProgramSelection.AllUsableEnabled.Count;

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
            await UpdateUser($"CreamAPI successfully {(Uninstalling ? "uninstalled" : "installed and generated")} for " + ProgramCount + " program(s).", InstallationLog.Success);
        }
        catch (Exception exception)
        {
            await UpdateUser($"CreamAPI {(Uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception.ToString(), InstallationLog.Error);
            retryButton.Enabled = true;
        }
        userProgressBar.Value = userProgressBar.Maximum;
        acceptButton.Enabled = true;
        cancelButton.Enabled = false;
        reselectButton.Enabled = true;
    }

    private void OnLoad(object sender, EventArgs _)
    {
    retry:
        try
        {
            userInfoLabel.Text = "Loading . . . ";
            logTextBox.Text = string.Empty;
            Start();
        }
        catch (Exception e)
        {
            if (ExceptionHandler.OutputException(e)) goto retry;
            Close();
        }
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

    private void OnCancel(object sender, EventArgs e) => Program.Cleanup();

    private void OnReselect(object sender, EventArgs e)
    {
        Program.Cleanup();
        Reselecting = true;
        Close();
    }
}
