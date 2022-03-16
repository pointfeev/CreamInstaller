using System;
using System.Drawing;
using System.IO;

namespace CreamInstaller.Utility;

internal static class IconGrabber
{
    internal static Icon ToIcon(this Image image)
    {
        using Bitmap dialogIconBitmap = new(image, new Size(image.Width, image.Height));
        return Icon.FromHandle(dialogIconBitmap.GetHicon());
    }

    internal const string SteamAppImagesPath = "https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/";

    internal static Image GetFileIconImage(string path) => File.Exists(path) ? Icon.ExtractAssociatedIcon(path).ToBitmap() : null;

    internal static Image GetNotepadImage() => GetFileIconImage(Diagnostics.GetNotepadPath());

    internal static Image GetCommandPromptImage() => GetFileIconImage(Environment.SystemDirectory + @"\cmd.exe");

    internal static Image GetFileExplorerImage() => GetFileIconImage(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\explorer.exe");
}
