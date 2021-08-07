using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CG.Web.MegaApiClient;
using Onova;
using Onova.Models;
using Onova.Services;

namespace CreamInstaller
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Text = Program.ApplicationName;
        }

        private static CancellationTokenSource cancellationTokenSource;

        private void StartProgram()
        {
            if (!(cancellationTokenSource is null))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            
            Hide();
            new SelectForm(this).ShowDialog();
            Close();
        }

        private static UpdateManager updateManager = null;
        private static Version latestVersion = null;

        private async void OnLoad()
        {
            Size = new Size(420, 85);
            progressBar1.Visible = false;
            ignoreButton.Visible = true;
            updateButton.Text = "Update";
            updateButton.Click -= OnUpdateCancel;
            label1.Text = "Checking for updates . . .";

            GithubPackageResolver resolver = new GithubPackageResolver("pointfeev", "CreamInstaller", "CreamInstaller.zip");
            ZipPackageExtractor extractor = new ZipPackageExtractor();
            updateManager = new UpdateManager(AssemblyMetadata.FromAssembly(Program.EntryAssembly, Program.CurrentProcessFilePath), resolver, extractor);

            if (latestVersion is null)
            {
                CheckForUpdatesResult checkForUpdatesResult = null;
                cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    checkForUpdatesResult = await updateManager.CheckForUpdatesAsync(cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                    if (checkForUpdatesResult.CanUpdate)
                        latestVersion = checkForUpdatesResult.LastVersion;
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
                label1.Text = $"An update is available: v{latestVersion}";
                ignoreButton.Enabled = true;
                updateButton.Enabled = true;
                updateButton.Click += new EventHandler(OnUpdate);
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            string FileName = Path.GetFileName(Program.CurrentProcessFilePath);
            if (FileName != "CreamInstaller.exe")
            {
                if (new DialogForm(this).Show(Program.ApplicationName, SystemIcons.Warning,
                    "WARNING: CreamInstaller.exe was renamed!" +
                    "\n\nThis will cause unwanted behavior when updating the program!",
                    "Ignore", "Abort") == DialogResult.Cancel)
                {
                    Environment.Exit(0);
                }
            }

            Program.MegaApiClient = new MegaApiClient();
            void Login()
            {
                try
                {
                    Program.MegaApiClient.Login();
                }
                catch (ApiException)
                {
                    if (new DialogForm(this).Show(Program.ApplicationName, SystemIcons.Error,
                        $"ERROR: Failed logging into MEGA!" +
                        "\n\nMEGA is likely offline, please try again later. . .",
                        "Retry", "Cancel") == DialogResult.OK)
                    {
                        Login();
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
            Login();

            OnLoad();
        }

        private void OnIgnore(object sender, EventArgs e)
        {
            StartProgram();
        }

        private async void OnUpdate(object sender, EventArgs e)
        {
            Size = new Size(420, 115);
            progressBar1.Visible = true;
            ignoreButton.Visible = false;
            updateButton.Text = "Cancel";
            updateButton.Click -= OnUpdate;
            updateButton.Click += new EventHandler(OnUpdateCancel);

            Progress<double> progress = new Progress<double>();
            progress.ProgressChanged += new EventHandler<double>(delegate (object sender, double _progress)
            {
                label1.Text = $"Updating . . . {(int)_progress}%";
                Program.UpdateProgressInstantly(progressBar1, (int)_progress);
            });

            label1.Text = "Updating . . . ";
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await updateManager.PrepareUpdateAsync(latestVersion, progress, cancellationTokenSource.Token);
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            catch { }

            if (!(updateManager is null) && updateManager.IsUpdatePrepared(latestVersion))
            {
                updateManager.LaunchUpdater(latestVersion);
                Application.Exit();
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
