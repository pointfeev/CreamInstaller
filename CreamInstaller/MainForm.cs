using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
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
            new SelectForm().ShowDialog();
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
            updateManager = new UpdateManager(resolver, extractor);

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
                progressBar1.Value = (int)_progress;
                label1.Text = $"Updating . . . {(int)_progress}%";
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
                label1.Text = "Updating . . . 100%";
                progressBar1.Value = 100;
                updateManager.LaunchUpdater(latestVersion);
                updateManager.Dispose();
                Close();
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
