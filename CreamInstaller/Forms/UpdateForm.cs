using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Forms;

internal sealed partial class UpdateForm : CustomForm
{
    private Release latestRelease;

    internal UpdateForm()
    {
        InitializeComponent();
        Text = Program.ApplicationNameShort;
    }

    private void StartProgram()
    {
        SelectForm form = new();
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
        List<Release> releases = null;
        string response = await HttpClientManager.EnsureGet($"https://api.github.com/repos/{Program.RepositoryOwner}/{Program.RepositoryName}/releases");
        if (response is not null)
            releases = JsonConvert.DeserializeObject<List<Release>>(response)
              ?.Where(release => !release.Draft && !release.Prerelease && release.Assets.Count > 0).ToList();
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
                Release release = releases[r];
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
            string fileName = Path.GetFileName(Program.CurrentProcessFilePath);
            if (fileName != Program.ApplicationExecutable)
            {
                using DialogForm form = new(this);
                if (form.Show(SystemIcons.Warning,
                        "WARNING: " + Program.ApplicationExecutable + " was renamed!" + "\n\nThis will cause undesirable behavior when updating the program!",
                        "Ignore", "Abort") == DialogResult.Cancel)
                {
                    Application.Exit();
                    return;
                }
            }
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

    private void OnUpdate(object sender, EventArgs e)
    {
        progressBar.Visible = true;
        ignoreButton.Visible = false;
        updateButton.Text = "Cancel";
        updateButton.Click -= OnUpdate;
        updateButton.Click += OnUpdateCancel;
        changelogTreeView.Location = progressBar.Location with { Y = progressBar.Location.Y + progressBar.Size.Height + 6 };
        Refresh();
        Progress<double> progress = new();
        progress.ProgressChanged += delegate(object _, double _progress)
        {
            progressLabel.Text = $"Updating . . . {(int)_progress}%";
            progressBar.Value = (int)_progress;
        };
        progressLabel.Text = "Updating . . . ";
        // do update
        OnLoad();
    }

    private void OnUpdateCancel(object sender, EventArgs e)
    {
        // cancel update
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }
}