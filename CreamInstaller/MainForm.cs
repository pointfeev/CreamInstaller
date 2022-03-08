using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

using CreamInstaller.Components;
using CreamInstaller.Utility;

using HtmlAgilityPack;

using Onova;
using Onova.Models;
using Onova.Services;

namespace CreamInstaller;

internal partial class MainForm : CustomForm
{
    internal MainForm() : base()
    {
        InitializeComponent();
        Text = Program.ApplicationNameShort;
    }

    private static CancellationTokenSource cancellationTokenSource;

    private void StartProgram()
    {
        if (cancellationTokenSource is not null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
        Hide();
        using SelectForm form = new(this);
        form.ShowDialog();
        Close();
    }

    private static UpdateManager updateManager;
    private static Version latestVersion;
    private static IReadOnlyList<Version> versions;

    private async void OnLoad()
    {
        Size = new(420, 85);
        progressBar1.Visible = false;
        ignoreButton.Visible = true;
        updateButton.Text = "Update";
        updateButton.Click -= OnUpdateCancel;
        label1.Text = "Checking for updates . . .";
        changelogTreeView.Visible = false;
        changelogTreeView.Location = new(12, 41);
        changelogTreeView.Size = new(380, 208);

        GithubPackageResolver resolver = new("pointfeev", "CreamInstaller", "CreamInstaller.zip");
        ZipPackageExtractor extractor = new();
        updateManager = new(AssemblyMetadata.FromAssembly(Program.EntryAssembly, Program.CurrentProcessFilePath), resolver, extractor);
        if (latestVersion is null)
        {
            CheckForUpdatesResult checkForUpdatesResult = null;
            cancellationTokenSource = new();
            try
            {
                checkForUpdatesResult = await updateManager.CheckForUpdatesAsync(cancellationTokenSource.Token);
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                if (checkForUpdatesResult.CanUpdate)
                {
                    latestVersion = checkForUpdatesResult.LastVersion;
                    versions = checkForUpdatesResult.Versions;
                }
            }
            catch { }
        }
        if (latestVersion is null)
        {
            updateManager.Dispose();
            updateManager = null;
            StartProgram();
        }
        else
        {
            Size = new(420, 300);
            label1.Text = $"An update is available: v{latestVersion}";
            ignoreButton.Enabled = true;
            updateButton.Enabled = true;
            updateButton.Click += new(OnUpdate);
            changelogTreeView.Visible = true;
            Version currentVersion = new(Application.ProductVersion);
            foreach (Version version in versions)
                if (version > currentVersion && !changelogTreeView.Nodes.ContainsKey(version.ToString()))
                {
                    TreeNode root = new($"v{version}");
                    root.Name = root.Text;
                    changelogTreeView.Nodes.Add(root);
                    _ = Task.Run(async () =>
                    {
                        HtmlNodeCollection nodes = await HttpClientManager.GetDocumentNodes(
                            $"https://github.com/pointfeev/CreamInstaller/releases/tag/v{version}",
                            "//div[@data-test-selector='body-content']/ul/li");
                        if (nodes is null) changelogTreeView.Nodes.Remove(root);
                        else foreach (HtmlNode node in nodes)
                            {
                                Program.Invoke(changelogTreeView, delegate
                                {
                                    TreeNode change = new();
                                    change.Text = HttpUtility.HtmlDecode(node.InnerText);
                                    root.Nodes.Add(change);
                                    root.Expand();
                                });
                            }
                    });
                }
            versions = null;
        }
    }

    private void OnLoad(object sender, EventArgs _)
    {
    retry:
        try
        {
            string FileName = Path.GetFileName(Program.CurrentProcessFilePath);
            if (FileName != "CreamInstaller.exe")
            {
                using DialogForm form = new(this);
                if (form.Show(SystemIcons.Warning,
                    "WARNING: CreamInstaller.exe was renamed!" +
                    "\n\nThis will cause unwanted behavior when updating the program!",
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
            if (ExceptionHandler.OutputException(e)) goto retry;
            Close();
        }
    }

    private void OnIgnore(object sender, EventArgs e) => StartProgram();

    private async void OnUpdate(object sender, EventArgs e)
    {
        progressBar1.Visible = true;
        ignoreButton.Visible = false;
        updateButton.Text = "Cancel";
        updateButton.Click -= OnUpdate;
        updateButton.Click += new(OnUpdateCancel);
        changelogTreeView.Location = new(12, 70);
        changelogTreeView.Size = new(380, 179);

        Progress<double> progress = new();
        progress.ProgressChanged += new(delegate (object sender, double _progress)
        {
            label1.Text = $"Updating . . . {(int)_progress}%";
            progressBar1.Value = (int)_progress;
        });

        label1.Text = "Updating . . . ";
        cancellationTokenSource = new();
        try
        {
            await updateManager.PrepareUpdateAsync(latestVersion, progress, cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
        catch { }

        if (updateManager is not null && updateManager.IsUpdatePrepared(latestVersion))
        {
            updateManager.LaunchUpdater(latestVersion);
            Application.Exit();
            return;
        }
        else OnLoad();
    }

    private void OnUpdateCancel(object sender, EventArgs e)
    {
        cancellationTokenSource.Cancel();
        updateManager.Dispose();
        updateManager = null;
    }
}
