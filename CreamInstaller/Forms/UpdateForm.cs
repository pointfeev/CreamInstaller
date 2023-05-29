using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Forms;

internal sealed partial class UpdateForm : CustomForm
{
    private static readonly string PackagePath = ProgramData.DirectoryPath + @"\" + Program.RepositoryPackage;
    private static readonly string ExecutablePath = ProgramData.DirectoryPath + @"\" + Program.RepositoryExecutable;
    private static readonly string UpdaterPath = ProgramData.DirectoryPath + @"\updater.cmd";

    private CancellationTokenSource cancellation;
    private ProgramRelease latestRelease;

    internal UpdateForm()
    {
        InitializeComponent();
        Text = Program.ApplicationNameShort;
    }

    private void StartProgram()
    {
        SelectForm form = SelectForm.Current;
        form.InheritLocation(this);
        form.FormClosing += (_, _) => Close();
        form.Show();
        Hide();
#if DEBUG
        DebugForm.Current.Attach(form);
#endif
    }

    private async void OnLoad()
    {
        progressBar.Visible = false;
        ignoreButton.Visible = true;
        updateButton.Text = "Update";
        updateButton.Click -= OnUpdateCancel;
        progressLabel.Text = "Checking for updates . . .";
        changelogTreeView.Visible = false;
        changelogTreeView.Location = progressLabel.Location with { Y = progressLabel.Location.Y + progressLabel.Size.Height + 13 };
        Refresh();
#if !DEBUG
        Version currentVersion = new(Program.Version);
#endif
        List<ProgramRelease> releases = null;
        string response = await HttpClientManager.EnsureGet($"https://api.github.com/repos/{Program.RepositoryOwner}/{Program.RepositoryName}/releases");
        if (response is not null)
            releases = JsonConvert.DeserializeObject<List<ProgramRelease>>(response)
              ?.Where(release => !release.Draft && !release.Prerelease && release.Asset is not null).ToList();
        latestRelease = releases?.FirstOrDefault();
#if DEBUG
        if (latestRelease?.Version is not { } latestVersion)
#else
        if (latestRelease?.Version is not { } latestVersion || latestVersion <= currentVersion)
#endif
            StartProgram();
        else
        {
            progressLabel.Text = $"An update is available: v{latestVersion}";
            ignoreButton.Enabled = true;
            updateButton.Enabled = true;
            updateButton.Click += OnUpdate;
            changelogTreeView.Visible = true;
            for (int r = releases!.Count - 1; r >= 0; r--)
            {
                ProgramRelease release = releases[r];
#if !DEBUG
                if (release.Version <= currentVersion)
                    continue;
#endif
                TreeNode root = new(release.Name) { Name = release.Name };
                changelogTreeView.Nodes.Add(root);
                if (changelogTreeView.Nodes.Count > 0)
                    changelogTreeView.Nodes[0].EnsureVisible();
                for (int i = release.Changes.Length - 1; i >= 0; i--)
                    changelogTreeView.Invoke(delegate
                    {
                        string change = release.Changes[i];
                        TreeNode changeNode = new() { Text = change };
                        root.Nodes.Add(changeNode);
                        root.Expand();
                        if (changelogTreeView.Nodes.Count > 0)
                            changelogTreeView.Nodes[0].EnsureVisible();
                    });
            }
        }
    }

    private void OnLoad(object sender, EventArgs _)
    {
    retry:
        try
        {
            UpdaterPath.DeleteFile();
            OnLoad();
        }
        catch (Exception e)
        {
            if (e.HandleException(this))
                goto retry;
            Close();
        }
    }

    private void OnIgnore(object sender, EventArgs e) => StartProgram();

    private async void OnUpdate(object sender, EventArgs e)
    {
        progressBar.Value = 0;
        progressBar.Visible = true;
        ignoreButton.Visible = false;
        updateButton.Text = "Cancel";
        updateButton.Click -= OnUpdate;
        updateButton.Click += OnUpdateCancel;
        changelogTreeView.Location = progressBar.Location with { Y = progressBar.Location.Y + progressBar.Size.Height + 6 };
        Refresh();
        Progress<int> progress = new();
        IProgress<int> iProgress = progress;
        progress.ProgressChanged += delegate(object _, int _progress)
        {
            progressLabel.Text = $"Updating . . . {_progress}%";
            progressBar.Value = _progress;
        };
        progressLabel.Text = "Updating . . . ";
        cancellation = new();
        bool success = true;
        PackagePath.DeleteFile(true);
        await using Stream update = PackagePath.CreateFile(true);
        bool retry = true;
        try
        {
            if (cancellation is null || Program.Canceled)
                throw new TaskCanceledException();
            using HttpResponseMessage response = await HttpClientManager.HttpClient.GetAsync(latestRelease.Asset.BrowserDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
            _ = response.EnsureSuccessStatusCode();
            if (cancellation is null || Program.Canceled)
                throw new TaskCanceledException();
            await using Stream download = await response.Content.ReadAsStreamAsync(cancellation.Token);
            double bytes = latestRelease.Asset.Size;
            byte[] buffer = new byte[16384];
            long bytesRead = 0;
            int newBytes;
            while (cancellation is not null && !Program.Canceled
                                            && (newBytes = await download.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellation.Token)) != 0)
            {
                if (cancellation is null || Program.Canceled)
                    throw new TaskCanceledException();
                await update.WriteAsync(buffer.AsMemory(0, newBytes), cancellation.Token);
                bytesRead += newBytes;
                int report = (int)(bytesRead / bytes * 100);
                if (report <= progressBar.Value)
                    continue;
                iProgress.Report(report);
            }
            iProgress.Report((int)(bytesRead / bytes * 100));
            if (cancellation is null || Program.Canceled)
                throw new TaskCanceledException();
        }
        catch (TaskCanceledException)
        {
            success = false;
        }
        catch (Exception ex)
        {
            retry = ex.HandleException(this, Program.Name + " encountered an exception while updating");
            success = false;
        }
        cancellation?.Dispose();
        cancellation = null;
        await update.DisposeAsync();
        bool canContinue = success && !Program.Canceled;
        if (canContinue)
            updateButton.Enabled = false;
        ExecutablePath.DeleteFile(canContinue);
        if (canContinue)
            await Task.Run(() => PackagePath.ExtractZip(ProgramData.DirectoryPath, true, this));
        PackagePath.DeleteFile(canContinue);
        if (canContinue)
        {
            string currentPath = Program.CurrentProcessFilePath;
            string currentDirectory = Path.GetDirectoryName(currentPath);
            string properExecutable = Path.GetFileName(ExecutablePath);
            string properExecutablePath = Path.Combine(currentDirectory!, properExecutable!);
            StringBuilder commands = new();
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"\nTASKKILL /F /T /PID {Program.CurrentProcessId}");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $":LOOP");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"TASKLIST | FIND \" {Program.CurrentProcessId}\" ");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"IF NOT ERRORLEVEL 1 (");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"   TIMEOUT /T 1");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"   GOTO LOOP");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $")");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"DEL /F /Q \"{currentPath}\"");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"DEL /F /Q \"{properExecutablePath}\"");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"MOVE /Y \"{ExecutablePath}\" \"{properExecutablePath}\"");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"START \"\" /D \"{currentDirectory}\" \"{properExecutable}\"");
            _ = commands.AppendLine(CultureInfo.InvariantCulture, $"EXIT");
            UpdaterPath.WriteFile(commands.ToString(), true, this);
            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                WorkingDirectory = ProgramData.DirectoryPath, FileName = "cmd.exe", Arguments = $"/C START \"UPDATER\" /B {Path.GetFileName(UpdaterPath)}",
                CreateNoWindow = true
            };
            process.StartInfo = startInfo;
            _ = process.Start();
            return;
        }
        if (!retry)
            StartProgram();
        else
            OnLoad();
    }

    private void OnUpdateCancel(object sender, EventArgs e)
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
        OnUpdateCancel(null, null);
    }
}