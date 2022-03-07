using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Components;
using CreamInstaller.Resources;
using CreamInstaller.Utility;

namespace CreamInstaller;

internal partial class InstallForm : CustomForm
{
    internal bool Reselecting = false;
    internal bool Uninstalling = false;

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

    internal static void WriteCreamConfiguration(StreamWriter writer, string appId, string name, SortedList<string, (DlcType type, string name, string icon)> dlc, InstallForm installForm = null)
    {
        writer.WriteLine($"; {name}");
        writer.WriteLine("[steam]");
        writer.WriteLine($"appid = {appId}");
        writer.WriteLine();
        writer.WriteLine("[dlc]");
        if (installForm is not null)
            installForm.UpdateUser($"Added game to cream_api.ini with appid {appId} ({name})", InstallationLog.Resource, info: false);
        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in dlc)
        {
            string dlcId = pair.Key;
            (_, string dlcName, _) = pair.Value;
            writer.WriteLine($"{dlcId} = {dlcName}");
            if (installForm is not null)
                installForm.UpdateUser($"Added DLC to cream_api.ini with appid {dlcId} ({dlcName})", InstallationLog.Resource, info: false);
        }
    }

    internal static async Task UninstallCreamAPI(string directory, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted file: {Path.GetFileName(config)}", InstallationLog.Resource, info: false);
        }
    });

    internal static async Task InstallCreamAPI(string directory, ProgramSelection selection, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.Steamworks32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.Steamworks64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
        }
        if (installForm is not null)
            installForm.UpdateUser("Generating CreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
        File.Create(config).Close();
        StreamWriter writer = new(config, true, Encoding.UTF8);
        if (selection.Id != "ParadoxLauncher")
            WriteCreamConfiguration(writer, selection.Id, selection.Name, selection.SelectedDlc, installForm);
        foreach (Tuple<string, string, SortedList<string, (DlcType type, string name, string icon)>> extraAppDlc in selection.ExtraDlc)
            WriteCreamConfiguration(writer, extraAppDlc.Item1, extraAppDlc.Item2, extraAppDlc.Item3, installForm);
        writer.Flush();
        writer.Close();
    });

    internal static void WriteScreamConfiguration(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> dlc, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"eos_logging\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"catalog_items\": {");
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> catalogItems = dlc.Where(pair => pair.Value.type == DlcType.CatalogItem);
        if (catalogItems.Any())
        {
            writer.WriteLine("    \"unlock_all\": false,");
            writer.WriteLine("    \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastCatalogItem = catalogItems.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in catalogItems)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastCatalogItem) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added catalog item to ScreamAPI.json with id {id} ({name})", InstallationLog.Resource, info: false);
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
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> entitlements = dlc.Where(pair => pair.Value.type == DlcType.Entitlement);
        if (entitlements.Any())
        {
            writer.WriteLine("    \"unlock_all\": false,");
            writer.WriteLine("    \"auto_inject\": false,");
            writer.WriteLine("    \"inject\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastEntitlement = entitlements.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in entitlements)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastEntitlement) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added entitlement to ScreamAPI.json with id {id} ({name})", InstallationLog.Resource, info: false);
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
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted file: {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted file: {Path.GetFileName(config)}", InstallationLog.Resource, info: false);
        }
    });

    internal static async Task InstallScreamAPI(string directory, ProgramSelection selection, InstallForm installForm = null) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.EpicOnlineServices32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk32)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed file: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Resource, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.EpicOnlineServices64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote resource to file: {Path.GetFileName(sdk64)}", InstallationLog.Resource, info: false);
        }
        if (installForm is not null)
            installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
        File.Create(config).Close();
        StreamWriter writer = new(config, true, Encoding.UTF8);
        if (selection.Id != "ParadoxLauncher")
            WriteScreamConfiguration(writer, selection.SelectedDlc, installForm);
        foreach (Tuple<string, string, SortedList<string, (DlcType type, string name, string icon)>> extraAppDlc in selection.ExtraDlc)
            WriteScreamConfiguration(writer, extraAppDlc.Item3, installForm);
        writer.Flush();
        writer.Close();
    });

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        int count = selection.DllDirectories.Count;
        int cur = 0;
        foreach (string directory in selection.DllDirectories)
        {
            UpdateUser($"{(Uninstalling ? "Uninstalling" : "Installing")} {(selection.IsSteam ? "CreamAPI" : "ScreamAPI")}" +
                $" {(Uninstalling ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
            if (!Program.IsProgramRunningDialog(this, selection)) throw new OperationCanceledException();
            if (selection.IsSteam)
            {
                if (Uninstalling)
                    await UninstallCreamAPI(directory, this);
                else
                    await InstallCreamAPI(directory, selection, this);
            }
            else
            {
                if (Uninstalling)
                    await UninstallScreamAPI(directory, this);
                else
                    await InstallScreamAPI(directory, selection, this);
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
            UpdateUser($"CreamAPI/ScreamAPI successfully {(Uninstalling ? "uninstalled" : "installed and generated")} for " + ProgramCount + " program(s).", InstallationLog.Success);
        }
        catch (Exception exception)
        {
            UpdateUser($"CreamAPI/ScreamAPI {(Uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception.ToString(), InstallationLog.Error);
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
