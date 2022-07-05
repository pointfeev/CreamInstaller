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
using CreamInstaller.Resources;
using CreamInstaller.Utility;

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

    internal static void WriteSmokeConfiguration(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideDlc, SortedList<string, (DlcType type, string name, string icon)> injectDlc, InstallForm installForm = null)
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
        if (injectDlc.Count > 0)
        {
            writer.WriteLine("  \"dlc_ids\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastInjectDlc = injectDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in injectDlc)
            {
                Thread.Sleep(0);
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastInjectDlc) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added inject DLC to SmokeAPI.json with appid {dlcId} ({dlcName})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"dlc_ids\": [],");
        writer.WriteLine("  \"auto_inject_inventory\": true,");
        writer.WriteLine("  \"inventory_items\": []");
        writer.WriteLine("}");
    }

    internal static async Task UninstallSmokeAPI(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out string cache);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Steamworks: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Steamworks: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(cache))
        {
            File.Delete(cache);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted cache: {Path.GetFileName(cache)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task InstallSmokeAPI(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
        if (File.Exists(oldConfig))
        {
            File.Delete(oldConfig);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", InstallationLog.Action, info: false);
        }
        directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out _);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Steamworks: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.Steamworks32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Steamworks: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.Steamworks64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (generateConfig)
        {
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideDlc = selection.AllDlc.Except(selection.SelectedDlc);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                overrideDlc = overrideDlc.Except(extraDlc);
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> injectDlc = new List<KeyValuePair<string, (DlcType type, string name, string icon)>>();
            if (selection.AllDlc.Count > 64 || selection.ExtraDlc.Any(e => e.dlc.Count > 64))
            {
                injectDlc = injectDlc.Concat(selection.SelectedDlc.Where(pair => pair.Value.type is DlcType.SteamHidden));
                foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                    if (selection.ExtraDlc.Where(e => e.id == id).Single().dlc.Count > 64)
                        injectDlc = injectDlc.Concat(extraDlc.Where(pair => pair.Value.type is DlcType.SteamHidden));
            }
            if (overrideDlc.Any() || injectDlc.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating SmokeAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(config).Close();
                StreamWriter writer = new(config, true, Encoding.UTF8);
                WriteSmokeConfiguration(writer,
                    new(overrideDlc.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
                    new(injectDlc.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
                    installForm);
                writer.Flush();
                writer.Close();
            }
            else if (File.Exists(config))
            {
                File.Delete(config);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
            }
        }
    });

    internal static void WriteScreamConfiguration(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideCatalogItems, SortedList<string, (DlcType type, string name, string icon)> entitlements, InstallForm installForm = null)
    {
        Thread.Sleep(0);
        writer.WriteLine("{");
        writer.WriteLine("  \"version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"eos_logging\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"catalog_items\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        if (overrideCatalogItems.Any())
        {
            writer.WriteLine("    \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideCatalogItem = overrideCatalogItems.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideCatalogItems)
            {
                Thread.Sleep(0);
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastOverrideCatalogItem) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added override catalog item to ScreamAPI.json with id {id} ({name})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("    ]");
        }
        else
            writer.WriteLine("    \"override\": []");
        writer.WriteLine("  },");
        writer.WriteLine("  \"entitlements\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        writer.WriteLine("    \"auto_inject\": true,");
        if (entitlements.Any())
        {
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
            writer.WriteLine("    \"inject\": []");
        writer.WriteLine("  }");
        writer.WriteLine("}");
    }

    internal static async Task UninstallScreamAPI(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out string cache);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Epic Online Services: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Epic Online Services: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(cache))
        {
            File.Delete(cache);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted cache: {Path.GetFileName(cache)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task InstallScreamAPI(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out _);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Epic Online Services: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.EpicOnlineServices32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Epic Online Services: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.EpicOnlineServices64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (generateConfig)
        {
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideCatalogItems = selection.AllDlc.Where(pair => pair.Value.type is DlcType.EpicCatalogItem).Except(selection.SelectedDlc);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                overrideCatalogItems = overrideCatalogItems.Except(extraDlc);
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> entitlements = selection.SelectedDlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> _dlc) in selection.ExtraSelectedDlc)
                entitlements = entitlements.Concat(_dlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement));
            if (overrideCatalogItems.Any() || entitlements.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(config).Close();
                StreamWriter writer = new(config, true, Encoding.UTF8);
                WriteScreamConfiguration(writer,
                    new(overrideCatalogItems.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
                    new(entitlements.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer),
                    installForm);
                writer.Flush();
                writer.Close();
            }
            else if (File.Exists(config))
            {
                File.Delete(config);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
            }
        }
    });

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.DllDirectories.Count;
        int cur = 0;
        if (selection.Id == "ParadoxLauncher")
        {
            UpdateUser($"Repairing Paradox Launcher . . . ", InstallationLog.Operation);
            await Repair(this, selection);
        }
        foreach (string directory in selection.DllDirectories)
        {
            Thread.Sleep(0);
            if (selection.IsSteam && selection.SelectedDlc.Any(d => d.Value.type is DlcType.Steam or DlcType.SteamHidden)
                || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.Steam or DlcType.SteamHidden)))
            {
                directory.GetSmokeApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out string cache);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config) || File.Exists(cache))
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
                directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config, out string cache);
                if (File.Exists(sdk32) || File.Exists(sdk32_o) || File.Exists(sdk64) || File.Exists(sdk64_o) || File.Exists(config) || File.Exists(cache))
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
