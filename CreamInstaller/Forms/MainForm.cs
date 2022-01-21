using HtmlAgilityPack;
using Onova;
using Onova.Models;
using Onova.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace CreamInstaller
{
    internal partial class MainForm : CustomForm
    {
        internal MainForm() : base()
        {
            InitializeComponent();
            Text = Program.ApplicationName;
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
            new SelectForm(this).ShowDialog();
            Close();
        }

        private static readonly HttpClient httpClient = new();
        private static UpdateManager updateManager = null;
        private static Version latestVersion = null;
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
                {
                    if (version > currentVersion && !changelogTreeView.Nodes.ContainsKey(version.ToString()))
                    {
                        TreeNode root = new($"v{version}");
                        root.Name = version.ToString();
                        changelogTreeView.Nodes.Add(root);
                        new Task(async () =>
                        {
                            try
                            {
                                string url = $"https://github.com/pointfeev/CreamInstaller/releases/tag/v{version}";
                                using HttpRequestMessage request = new(HttpMethod.Get, url);
                                using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                                response.EnsureSuccessStatusCode();
                                using Stream stream = await response.Content.ReadAsStreamAsync();
                                using StreamReader reader = new(stream, Encoding.UTF8);
                                HtmlAgilityPack.HtmlDocument document = new();
                                document.LoadHtml(reader.ReadToEnd());
                                foreach (HtmlNode node in document.DocumentNode.SelectNodes("//div[@data-test-selector='body-content']/ul/li"))
                                {
                                    changelogTreeView.Invoke((MethodInvoker)delegate
                                    {
                                        TreeNode change = new();
                                        change.Text = $"{HttpUtility.HtmlDecode(node.InnerText)}";
                                        root.Nodes.Add(change);
                                        root.Expand();
                                    });
                                }
                            }
                            catch
                            {
                                changelogTreeView.Nodes.Remove(root);
                            }
                        }).Start();
                    }
                }
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
                    if (new DialogForm(this).Show(Program.ApplicationName, SystemIcons.Warning,
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
                if (ExceptionHandler.OutputException(e))
                {
                    goto retry;
                }

                Close();
            }
        }

        private void OnIgnore(object sender, EventArgs e)
        {
            StartProgram();
        }

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
            else
            {
                OnLoad();
            }
        }

        private void OnUpdateCancel(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            updateManager.Dispose();
            updateManager = null;
        }
    }
}