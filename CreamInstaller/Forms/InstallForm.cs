using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Platforms.Paradox.ParadoxLauncher;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Forms;

internal sealed partial class InstallForm : CustomForm
{
    private readonly List<ProgramSelection> disabledSelections = new();

    private readonly int programCount = ProgramSelection.AllEnabled.Count;
    private readonly bool uninstalling;
    private int completeOperationsCount;

    private int operationsCount;
    internal bool Reselecting;

    internal InstallForm(bool uninstall = false)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
        logTextBox.BackColor = LogTextBox.Background;
        uninstalling = uninstall;
    }

    private void UpdateProgress(int progress)
    {
        if (!userProgressBar.Disposing && !userProgressBar.IsDisposed)
            userProgressBar.Invoke(() =>
            {
                int value = (int)((float)completeOperationsCount / operationsCount * 100) + progress / operationsCount;
                if (value < userProgressBar.Value)
                    return;
                userProgressBar.Value = value;
            });
    }

    internal void UpdateUser(string text, Color color, bool info = true, bool log = true)
    {
        if (info)
            _ = userInfoLabel.Invoke(() => userInfoLabel.Text = text);
        if (log && !logTextBox.Disposing && !logTextBox.IsDisposed)
            logTextBox.Invoke(() =>
            {
                if (logTextBox.Text.Length > 0)
                    logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(text, color);
                logTextBox.Invalidate();
            });
    }

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        if (selection.Id == "PL")
        {
            UpdateUser("Repairing Paradox Launcher . . . ", LogTextBox.Operation);
            _ = await Repair(this, selection);
        }
        UpdateUser(
            $"{(uninstalling ? "Uninstalling" : "Installing")}" + $" {(uninstalling ? "from" : "for")} " + selection.Name
          + $" with root directory \"{selection.RootDirectory}\" . . . ", LogTextBox.Operation);
        IEnumerable<string> invalidDirectories = (await selection.RootDirectory.GetExecutables())
                                               ?.Where(d => selection.ExecutableDirectories.All(s => s.directory != Path.GetDirectoryName(d.path)))
                                                .Select(d => Path.GetDirectoryName(d.path));
        if (selection.ExecutableDirectories.All(s => s.directory != selection.RootDirectory))
            invalidDirectories = invalidDirectories?.Append(selection.RootDirectory);
        invalidDirectories = invalidDirectories?.Distinct();
        if (invalidDirectories is not null)
            foreach (string directory in invalidDirectories)
            {
                if (Program.Canceled)
                    throw new CustomMessageException("The operation was canceled.");
                directory.GetKoaloaderComponents(out string old_config, out string config);
                if (directory.GetKoaloaderProxies().Any(proxy => File.Exists(proxy) && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                 || directory != selection.RootDirectory && Koaloader.AutoLoadDLLs.Any(pair => File.Exists(directory + @"\" + pair.dll))
                 || File.Exists(old_config) || File.Exists(config))
                {
                    UpdateUser("Uninstalling Koaloader from " + selection.Name + $" in incorrect directory \"{directory}\" . . . ", LogTextBox.Operation);
                    await Koaloader.Uninstall(directory, selection.RootDirectory, this);
                }
                Thread.Sleep(1);
            }
        if (uninstalling || !selection.Koaloader)
            foreach ((string directory, BinaryType _) in selection.ExecutableDirectories)
            {
                if (Program.Canceled)
                    throw new CustomMessageException("The operation was canceled.");
                directory.GetKoaloaderComponents(out string old_config, out string config);
                if (directory.GetKoaloaderProxies().Any(proxy => File.Exists(proxy) && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                 || Koaloader.AutoLoadDLLs.Any(pair => File.Exists(directory + @"\" + pair.dll)) || File.Exists(old_config) || File.Exists(config))
                {
                    UpdateUser("Uninstalling Koaloader from " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                    await Koaloader.Uninstall(directory, selection.RootDirectory, this);
                }
                Thread.Sleep(1);
            }
        bool uninstallProxy = uninstalling || selection.Koaloader;
        int count = selection.DllDirectories.Count, cur = 0;
        foreach (string directory in selection.DllDirectories)
        {
            if (Program.Canceled)
                throw new CustomMessageException("The operation was canceled.");
            if (selection.Platform is Platform.Steam or Platform.Paradox)
            {
                directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                    out string config, out string old_log, out string log, out string cache);
                if (uninstallProxy
                        ? File.Exists(api32_o) || File.Exists(api64_o) || File.Exists(old_config) || File.Exists(config) || File.Exists(old_log)
                       || File.Exists(log) || File.Exists(cache)
                        : File.Exists(api32) || File.Exists(api64))
                {
                    UpdateUser(
                        $"{(uninstallProxy ? "Uninstalling" : "Installing")} SmokeAPI" + $" {(uninstallProxy ? "from" : "for")} " + selection.Name
                      + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                    if (uninstallProxy)
                        await SmokeAPI.Uninstall(directory, this);
                    else
                        await SmokeAPI.Install(directory, selection, this);
                }
            }
            if (selection.Platform is Platform.Epic or Platform.Paradox)
            {
                directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string log);
                if (uninstallProxy
                        ? File.Exists(api32_o) || File.Exists(api64_o) || File.Exists(config) || File.Exists(log)
                        : File.Exists(api32) || File.Exists(api64))
                {
                    UpdateUser(
                        $"{(uninstallProxy ? "Uninstalling" : "Installing")} ScreamAPI" + $" {(uninstallProxy ? "from" : "for")} " + selection.Name
                      + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                    if (uninstallProxy)
                        await ScreamAPI.Uninstall(directory, this);
                    else
                        await ScreamAPI.Install(directory, selection, this);
                }
            }
            if (selection.Platform is Platform.Ubisoft)
            {
                directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string log);
                if (uninstallProxy
                        ? File.Exists(api32_o) || File.Exists(api64_o) || File.Exists(config) || File.Exists(log)
                        : File.Exists(api32) || File.Exists(api64))
                {
                    UpdateUser(
                        $"{(uninstallProxy ? "Uninstalling" : "Installing")} Uplay R1 Unlocker" + $" {(uninstallProxy ? "from" : "for")} " + selection.Name
                      + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                    if (uninstallProxy)
                        await UplayR1.Uninstall(directory, this);
                    else
                        await UplayR1.Install(directory, selection, this);
                }
                directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config, out log);
                if (uninstallProxy
                        ? File.Exists(api32_o) || File.Exists(api64_o) || File.Exists(config) || File.Exists(log)
                        : File.Exists(old_api32) || File.Exists(old_api64) || File.Exists(api32) || File.Exists(api64))
                {
                    UpdateUser(
                        $"{(uninstallProxy ? "Uninstalling" : "Installing")} Uplay R2 Unlocker" + $" {(uninstallProxy ? "from" : "for")} " + selection.Name
                      + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                    if (uninstallProxy)
                        await UplayR2.Uninstall(directory, this);
                    else
                        await UplayR2.Install(directory, selection, this);
                }
            }
            UpdateProgress(++cur / count * 100);
            Thread.Sleep(1);
        }
        if (selection.Koaloader && !uninstalling)
            foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories)
            {
                if (Program.Canceled)
                    throw new CustomMessageException("The operation was canceled.");
                UpdateUser("Installing Koaloader to " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);
                await Koaloader.Install(directory, binaryType, selection, selection.RootDirectory, this);
                Thread.Sleep(1);
            }
        UpdateProgress(100);
    }

    private async Task Operate()
    {
        List<ProgramSelection> programSelections = ProgramSelection.AllEnabled;
        operationsCount = programSelections.Count;
        completeOperationsCount = 0;
        foreach (ProgramSelection selection in programSelections)
        {
            if (Program.Canceled || !Program.IsProgramRunningDialog(this, selection))
                throw new CustomMessageException("The operation was canceled.");
            try
            {
                await OperateFor(selection);
                UpdateUser($"Operation succeeded for {selection.Name}.", LogTextBox.Success);
                selection.Enabled = false;
                disabledSelections.Add(selection);
            }
            catch (Exception exception)
            {
                UpdateUser($"Operation failed for {selection.Name}: " + exception, LogTextBox.Error);
            }
            ++completeOperationsCount;
        }
        Program.Cleanup();
        List<ProgramSelection> failedSelections = ProgramSelection.AllEnabled;
        if (failedSelections.Any())
            if (failedSelections.Count == 1)
                throw new CustomMessageException($"Operation failed for {failedSelections.First().Name}.");
            else
                throw new CustomMessageException($"Operation failed for {failedSelections.Count} programs.");
        foreach (ProgramSelection selection in disabledSelections)
            selection.Enabled = true;
        disabledSelections.Clear();
    }

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
            UpdateUser($"DLC unlocker(s) successfully {(uninstalling ? "uninstalled" : "installed and generated")} for " + programCount + " program(s).",
                LogTextBox.Success);
        }
        catch (Exception exception)
        {
            UpdateUser($"DLC unlocker {(uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception, LogTextBox.Error);
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
            if (e.HandleException(this))
                goto retry;
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
        foreach (ProgramSelection selection in disabledSelections)
            selection.Enabled = true;
        disabledSelections.Clear();
        Close();
    }
}