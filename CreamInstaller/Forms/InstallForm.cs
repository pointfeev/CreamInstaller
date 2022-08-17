using CreamInstaller.Components;
using CreamInstaller.Resources;
using CreamInstaller.Utility;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using static CreamInstaller.Paradox.ParadoxLauncher;

namespace CreamInstaller;

internal partial class InstallForm : CustomForm
{
    internal bool Reselecting;
    internal readonly bool Uninstalling;

    internal InstallForm(IWin32Window owner, bool uninstall = false) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
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
                int value = (int)((float)CompleteOperationsCount / OperationsCount * 100) + progress / OperationsCount;
                if (value < userProgressBar.Value) return;
                userProgressBar.Value = value;
            });
    }

    internal void UpdateUser(string text, Color color, bool info = true, bool log = true)
    {
        if (info) _ = userInfoLabel.Invoke(() => userInfoLabel.Text = text);
        if (log && !logTextBox.Disposing && !logTextBox.IsDisposed)
        {
            logTextBox.Invoke(() =>
            {
                if (logTextBox.Text.Length > 0) logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(text, color);
                logTextBox.Refresh();
            });
        }
    }

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.DllDirectories.Count;
        int cur = 0;
        if (selection.Id == "ParadoxLauncher")
        {
            UpdateUser($"Repairing Paradox Launcher . . . ", InstallationLog.Operation);
            _ = await Repair(this, selection);
        }
        foreach (string directory in selection.DllDirectories)
        {
            if (selection.IsSteam && selection.SelectedDlc.Any(d => d.Value.type is DlcType.Steam or DlcType.SteamHidden)
                || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.Steam or DlcType.SteamHidden)))
            {
                directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out string cache);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config) || File.Exists(cache))
                {
                    UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} SmokeAPI" +
                        $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (Uninstalling)
                        await SmokeAPI.Uninstall(directory, this);
                    else
                        await SmokeAPI.Install(directory, selection, this);
                }
            }
            if (selection.IsEpic && selection.SelectedDlc.Any(d => d.Value.type is DlcType.EpicCatalogItem or DlcType.EpicEntitlement)
                || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.EpicCatalogItem or DlcType.EpicEntitlement)))
            {
                directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config))
                {
                    UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} ScreamAPI" +
                        $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (Uninstalling)
                        await ScreamAPI.Uninstall(directory, this);
                    else
                        await ScreamAPI.Install(directory, selection, this);
                }
            }
            if (selection.IsUbisoft)
            {
                directory.GetUplayR1Components(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config))
                {
                    UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} Uplay R1 Unlocker" +
                        $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (Uninstalling)
                        await UplayR1.Uninstall(directory, this);
                    else
                        await UplayR1.Install(directory, selection, this);
                }
                directory.GetUplayR2Components(out string old_sdk32, out string old_sdk64, out sdk32, out sdk32_o, out sdk64, out sdk64_o, out config);
                if (File.Exists(old_sdk32) || File.Exists(old_sdk64) || File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config))
                {
                    UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} Uplay R2 Unlocker" +
                        $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (Uninstalling)
                        await UplayR2.Uninstall(directory, this);
                    else
                        await UplayR2.Install(directory, selection, this);
                }
            }
            UpdateProgress(++cur / count * 100);
        }
        UpdateProgress(100);
    }

    private async Task Operate()
    {
        List<ProgramSelection> programSelections = ProgramSelection.AllEnabled;
        OperationsCount = programSelections.Count;
        CompleteOperationsCount = 0;
        List<ProgramSelection> disabledSelections = new();
        foreach (ProgramSelection selection in programSelections)
        {
            if (Program.Canceled || !Program.IsProgramRunningDialog(this, selection)) throw new CustomMessageException("The operation was canceled.");
            try
            {
                await OperateFor(selection);
                UpdateUser($"Operation succeeded for {selection.Name}.", InstallationLog.Success);
                selection.Enabled = false;
                disabledSelections.Add(selection);
            }
            catch (Exception exception)
            {
                UpdateUser($"Operation failed for {selection.Name}: " + exception, InstallationLog.Error);
            }
            ++CompleteOperationsCount;
        }
        Program.Cleanup();
        List<ProgramSelection> FailedSelections = ProgramSelection.AllEnabled;
        if (FailedSelections.Any())
            if (FailedSelections.Count == 1) throw new CustomMessageException($"Operation failed for {FailedSelections.First().Name}.");
            else throw new CustomMessageException($"Operation failed for {FailedSelections.Count} programs.");
        foreach (ProgramSelection selection in disabledSelections) selection.Enabled = true;
    }

    private readonly int ProgramCount = ProgramSelection.AllEnabled.Count;

    private async void Start()
    {
        Program.Canceled = false;
        acceptButton.Enabled = false;
        retryButton.Enabled = false;
        cancelButton.Enabled = true;
        reselectButton.Enabled = false;
        userProgressBar.Value = userProgressBar.Minimum;
        try
        {
            await Operate();
            UpdateUser($"SmokeAPI/ScreamAPI successfully {(Uninstalling ? "uninstalled" : "installed and generated")} for " + ProgramCount + " program(s).", InstallationLog.Success);
        }
        catch (Exception exception)
        {
            UpdateUser($"SmokeAPI/ScreamAPI {(Uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception, InstallationLog.Error);
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
            if (e.HandleException(form: this)) goto retry;
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
