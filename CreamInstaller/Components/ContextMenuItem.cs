using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Paradox;
using CreamInstaller.Utility;

namespace CreamInstaller.Components;

internal class ContextMenuItem : ToolStripMenuItem
{
    private static readonly ConcurrentDictionary<string, Image> images = new();

    private static async Task TryImageIdentifier(ContextMenuItem item, string imageIdentifier) => await Task.Run(async () =>
    {
        if (images.TryGetValue(imageIdentifier, out Image image) && image is not null) item.Image = image;
        else
        {
            switch (imageIdentifier)
            {
                case "Paradox Launcher":
                    if (Directory.Exists(ParadoxLauncher.InstallPath))
                        foreach (string file in Directory.GetFiles(ParadoxLauncher.InstallPath, "*.exe"))
                        {
                            image = IconGrabber.GetFileIconImage(file);
                            break;
                        }
                    break;
                case "Notepad":
                    image = IconGrabber.GetNotepadImage();
                    break;
                case "Command Prompt":
                    image = IconGrabber.GetCommandPromptImage();
                    break;
                case "File Explorer":
                    image = IconGrabber.GetFileExplorerImage();
                    break;
                case "SteamDB":
                    image = await HttpClientManager.GetImageFromUrl("https://steamdb.info/favicon.ico");
                    break;
                case "Steam Store":
                    image = await HttpClientManager.GetImageFromUrl("https://store.steampowered.com/favicon.ico");
                    break;
                case "Steam Community":
                    image = await HttpClientManager.GetImageFromUrl("https://steamcommunity.com/favicon.ico");
                    break;
                case "ScreamDB":
                    image = await HttpClientManager.GetImageFromUrl("https://scream-db.web.app/favicon.ico");
                    break;
                case "Epic Games":
                    image = await HttpClientManager.GetImageFromUrl("https://www.epicgames.com/favicon.ico");
                    break;
                default:
                    return;
            }
            if (image is not null)
            {
                images[imageIdentifier] = image;
                item.Image = image;
            }
        }
    }).ConfigureAwait(false);

    private static async Task TryImageIdentifierInfo(ContextMenuItem item, (string id, string iconUrl, bool sub) imageIdentifierInfo, Action onFail = null) => await Task.Run(async () =>
    {
        (string id, string iconUrl, bool sub) = imageIdentifierInfo;
        string imageIdentifier = sub ? "SubIcon_" + id : "Icon_" + id;
        if (images.TryGetValue(imageIdentifier, out Image image) && image is not null) item.Image = image;
        else
        {
            image = await HttpClientManager.GetImageFromUrl(iconUrl);
            if (image is not null)
            {
                images[imageIdentifier] = image;
                item.Image = image;
            }
            else if (onFail is not null)
                onFail();
        }
    }).ConfigureAwait(false);

    private readonly EventHandler OnClickEvent;
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (OnClickEvent is null) return;
        OnClickEvent.Invoke(this, e);
    }

    internal ContextMenuItem(string text, EventHandler onClick = null)
    {
        Text = text;
        OnClickEvent = onClick;
    }

    internal ContextMenuItem(string text, string imageIdentifier, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifier(this, imageIdentifier);

    internal ContextMenuItem(string text, (string id, string iconUrl, bool sub) imageIdentifierInfo, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo);

    internal ContextMenuItem(string text, (string id, string iconUrl, bool sub) imageIdentifierInfo, string imageIdentifierFallback, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo, async () => await TryImageIdentifier(this, imageIdentifierFallback));

    internal ContextMenuItem(string text, (string id, string iconUrl, bool sub) imageIdentifierInfo, (string id, string iconUrl, bool sub) imageIdentifierInfoFallback, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo, async () => await TryImageIdentifierInfo(this, imageIdentifierInfoFallback));
}