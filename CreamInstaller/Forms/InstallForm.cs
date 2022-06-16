using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Components;
using CreamInstaller.Paradox;
using CreamInstaller.Resources;
using CreamInstaller.Utility;

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
        if (info) userInfoLabel.Invoke(() => userInfoLabel.Text = text);
        if (log && !logTextBox.Disposing && !logTextBox.IsDisposed)
        {
            logTextBox.Invoke(() =>
            {
                if (logTextBox.Text.Length > 0) logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(text, color);
            });
            Thread.Sleep(0);
        }
    }

    internal static void WriteSmokeConfiguration(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideDlc, SortedList<string, (DlcType type, string name, string icon)> allDlc, InstallForm installForm = null)
    {
        Thread.Sleep(0);
        writer.WriteLine("{");
        writer.WriteLine("  \"$version\": 1,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"hook_steamclient\": true,");
        writer.WriteLine("  \"unlock_all\": true,");
        if (overrideDlc.Count > 0)
        {
            writer.WriteLine("  \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideDlc = overrideDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideDlc)
            {
                Thread.Sleep(0);
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastOverrideDlc) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added override DLC to SmokeAPI.json with appid {dlcId} ({dlcName})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"override\": [],");
        if (allDlc.Count > 0)
        {
            writer.WriteLine("  \"dlc_ids\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastAllDlc = allDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in allDlc)
            {
                Thread.Sleep(0);
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastAllDlc) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added DLC to SmokeAPI.json with appid {dlcId} ({dlcName})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"dlc_ids\": [],");
        writer.WriteLine("  \"auto_inject_inventory\": true,");
        writer.WriteLine("  \"inventory_items\": []");
        writer.WriteLine("}");
    }

    internal static async Task UninstallSmokeAPI(string directory, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted file: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task InstallSmokeAPI(string directory, ProgramSelection selection, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
        if (File.Exists(oldConfig))
        {
            File.Delete(oldConfig);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted old config: {Path.GetFileName(oldConfig)}", InstallationLog.Action, info: false);
        }
        directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.Steamworks32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.Steamworks64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (installForm is not null)
            installForm.UpdateUser("Generating SmokeAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
        File.Create(config).Close();
        StreamWriter writer = new(config, true, Encoding.UTF8);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> allDlc = selection.AllDlc.AsEnumerable();
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraDlc)
            allDlc = allDlc.Concat(extraDlc);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideDlc = allDlc.Except(selection.SelectedDlc);
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
            overrideDlc = overrideDlc.Except(extraDlc);
        WriteSmokeConfiguration(writer,
            new(overrideDlc.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
            new(allDlc.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
            installForm);
        writer.Flush();
        writer.Close();
    });

    internal static void WriteScreamConfiguration(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> dlc, List<(string id, string name, SortedList<string, (DlcType type, string name, string icon)> dlc)> extraDlc, InstallForm installForm = null)
    {
        Thread.Sleep(0);
        writer.WriteLine("{");
        writer.WriteLine("  \"version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"eos_logging\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"catalog_items\": {");
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> catalogItems = dlc.Where(pair => pair.Value.type == DlcType.EpicCatalogItem);
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> _dlc) in extraDlc)
            catalogItems = catalogItems.Concat(_dlc.Where(pair => pair.Value.type == DlcType.EpicCatalogItem));
        if (catalogItems.Any())
        {
            writer.WriteLine("    \"unlock_all\": false,");
            writer.WriteLine("    \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastCatalogItem = catalogItems.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in catalogItems)
            {
                Thread.Sleep(0);
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastCatalogItem) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added catalog item to ScreamAPI.json with id {id} ({name})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("    ]");
        }
        else
        {
            writer.WriteLine("    \"unlock_all\": true,");
            writer.WriteLine("    \"override\": []");
        }
        writer.WriteLine("  },");
        writer.WriteLine("  \"entitlements\": {");
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> entitlements = dlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement);
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> _dlc) in extraDlc)
            entitlements = entitlements.Concat(_dlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement));
        if (entitlements.Any())
        {
            writer.WriteLine("    \"unlock_all\": false,");
            writer.WriteLine("    \"auto_inject\": false,");
            writer.WriteLine("    \"inject\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastEntitlement = entitlements.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in entitlements)
            {
                Thread.Sleep(0);
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastEntitlement) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added entitlement to ScreamAPI.json with id {id} ({name})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("    ]");
        }
        else
        {
            writer.WriteLine("    \"unlock_all\": true,");
            writer.WriteLine("    \"auto_inject\": true,");
            writer.WriteLine("    \"inject\": []");
        }
        writer.WriteLine("  }");
        writer.WriteLine("}");
    }

    internal static async Task UninstallScreamAPI(string directory, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted file: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task InstallScreamAPI(string directory, ProgramSelection selection, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.EpicOnlineServices32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.EpicOnlineServices64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (installForm is not null)
            installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
        File.Create(config).Close();
        StreamWriter writer = new(config, true, Encoding.UTF8);
        WriteScreamConfiguration(writer, selection.SelectedDlc, selection.ExtraSelectedDlc, installForm);
        writer.Flush();
        writer.Close();
    });

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.DllDirectories.Count;
        int cur = 0;
        int code = 0;
        if (selection.Id == "ParadoxLauncher")
        {
            UpdateUser($"Repairing Paradox Launcher . . . ", InstallationLog.Operation);
            code = await ParadoxLauncher.Repair(this, selection);
            switch (code)
            {
                case -2:
                    throw new CustomMessageException("Repair failed! The Paradox Launcher is currently running!");
                case -1:
                    throw new CustomMessageException("Repair failed! " +
                        "An original Steamworks/Epic Online Services SDK file could not be found. " +
                        "You must reinstall Paradox Launcher to fix this issue.");
                case 0:
                    UpdateUser("Paradox Launcher does not need to be repaired.", InstallationLog.Action);
                    break;
                case 1:
                    UpdateUser("Paradox Launcher successfully repaired!", InstallationLog.Success);
                    break;
            }
        }
        if (code < 0) throw new CustomMessageException("Repair failed!");
        foreach (string directory in selection.DllDirectories)
        {
            Thread.Sleep(0);
            if (selection.IsSteam && selection.SelectedDlc.Any(d => d.Value.type is DlcType.Steam)
                || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.Steam)))
            {
                directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config))
                {
                    UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} SmokeAPI" +
                        $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (Uninstalling)
                        await UninstallSmokeAPI(directory, this);
                    else
                        await InstallSmokeAPI(directory, selection, this);
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
                        await UninstallScreamAPI(directory, this);
                    else
                        await InstallScreamAPI(directory, selection, this);
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
            Thread.Sleep(0);
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
