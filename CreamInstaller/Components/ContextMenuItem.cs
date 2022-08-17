using CreamInstaller.Paradox;
using CreamInstaller.Utility;

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("steamdb.info"));
                    break;
                case "Steam Store":
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("store.steampowered.com"));
                    break;
                case "Steam Community":
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("steamcommunity.com"));
                    break;
                case "ScreamDB":
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("scream-db.web.app"));
                    break;
                case "Epic Games":
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("epicgames.com"));
                    break;
                case "Ubisoft Store":
                    image = await HttpClientManager.GetImageFromUrl(IconGrabber.GetDomainFaviconUrl("store.ubi.com"));
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

    private static async Task TryImageIdentifierInfo(ContextMenuItem item, (string id, string iconUrl) imageIdentifierInfo, Action onFail = null) => await Task.Run(async () =>
    {
        (string id, string iconUrl) = imageIdentifierInfo;
        string imageIdentifier = "Icon_" + id;
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

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo);

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo, string imageIdentifierFallback, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo, async () => await TryImageIdentifier(this, imageIdentifierFallback));

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo, (string id, string iconUrl) imageIdentifierInfoFallback, EventHandler onClick = null)
        : this(text, onClick) => _ = TryImageIdentifierInfo(this, imageIdentifierInfo, async () => await TryImageIdentifierInfo(this, imageIdentifierInfoFallback));
}