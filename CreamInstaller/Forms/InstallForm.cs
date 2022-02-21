using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Classes;
using CreamInstaller.Forms.Components;
using CreamInstaller.Resources;

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
        if (!userProgressBar.Disposing && !userProgressBar.IsDisposed)
            userProgressBar.Invoke(() =>
            {
                int value = (int)((float)(CompleteOperationsCount / (float)OperationsCount) * 100) + progress / OperationsCount;
                if (value < userProgressBar.Value) return;
                userProgressBar.Value = value;
            });
    }

    internal void UpdateUser(string text, Color color, bool info = true, bool log = true)
    {
        if (info) userInfoLabel.Invoke(() => userInfoLabel.Text = text);
        if (log && !logTextBox.Disposing && !logTextBox.IsDisposed)
        {
            logTextBox.Invoke(() =>
            {
                if (logTextBox.Text.Length > 0) logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(text, color);
            });
        }
    }

    internal static void WriteConfiguration(StreamWriter writer, int steamAppId, string name, SortedList<int, (string name, string iconStaticId)> steamDlcApps, InstallForm installForm = null)
    {
        writer.WriteLine();
        writer.WriteLine($"; {name}");
        writer.WriteLine("[steam]");
        writer.WriteLine($"appid = {steamAppId}");
        writer.WriteLine();
        writer.WriteLine("[dlc]");
        if (installForm is not null)
            installForm.UpdateUser($"Added game to cream_api.ini with appid {steamAppId} ({name})", InstallationLog.Resource, info: false);
        foreach (KeyValuePair<int, (string name, string iconStaticId)> pair in steamDlcApps)
        {
            int appId = pair.Key;
            (string name, string iconStaticId) dlcApp = pair.Value;
            writer.WriteLine($"{appId} = {dlcApp.name}");
            if (installForm is not null)
                installForm.UpdateUser($"Added DLC to cream_api.ini with appid {appId} ({dlcApp.name})", InstallationLog.Resource, info: false);
        }
    }

    internal static async Task UninstallCreamAPI(string directory, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
        if (File.Exists(api_o))
        {
            if (File.Exists(api))
            {
                File.Delete(api);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
            }
            File.Move(api_o, api);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(api_o)} -> {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(api64_o))
        {
            if (File.Exists(api64))
            {
                File.Delete(api64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
            }
            File.Move(api64_o, api64);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(cApi))
        {
            File.Delete(cApi);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted file: {Path.GetFileName(cApi)}", InstallationLog.Resource, info: false);
        }
    });

    internal static async Task InstallCreamAPI(string directory, ProgramSelection selection, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
        if (File.Exists(api) && !File.Exists(api_o))
        {
            File.Move(api, api_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(api)} -> {Path.GetFileName(api_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(api_o))
        {
            Properties.Resources.API.Write(api);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(api)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(api64) && !File.Exists(api64_o))
        {
            File.Move(api64, api64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(api64_o))
        {
            Properties.Resources.API64.Write(api64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(api64)}", InstallationLog.Resource, info: false);
        }
        if (installForm is not null)
            installForm.UpdateUser("Generating CreamAPI for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
        File.Create(cApi).Close();
        StreamWriter writer = new(cApi, true, Encoding.UTF8);
        writer.WriteLine("; " + Application.CompanyName + " v" + Application.ProductVersion);
        if (selection.SteamAppId > 0)
            WriteConfiguration(writer, selection.SteamAppId, selection.Name, selection.SelectedSteamDlc, installForm);
        foreach (Tuple<int, string, SortedList<int, (string name, string iconStaticId)>> extraAppDlc in selection.ExtraSteamAppIdDlc)
            WriteConfiguration(writer, extraAppDlc.Item1, extraAppDlc.Item2, extraAppDlc.Item3, installForm);
        writer.Flush();
        writer.Close();
    });

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.SteamApiDllDirectories.Count;
        int cur = 0;
        foreach (string directory in selection.SteamApiDllDirectories)
        {
            UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} CreamAPI for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
            if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
            if (Uninstalling)
                await UninstallCreamAPI(directory, this);
            else
                await InstallCreamAPI(directory, selection, this);
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
                UpdateUser($"Operation succeeded for {selection.Name}.", InstallationLog.Success);
                selection.Enabled = false;
                disabledSelections.Add(selection);
            }
            catch (Exception exception)
            {
                UpdateUser($"Operation failed for {selection.Name}: " + exception.ToString(), InstallationLog.Error);
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
            UpdateUser($"CreamAPI successfully {(Uninstalling ? "uninstalled" : "installed and generated")} for " + ProgramCount + " program(s).", InstallationLog.Success);
        }
        catch (Exception exception)
        {
            UpdateUser($"CreamAPI {(Uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception.ToString(), InstallationLog.Error);
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
