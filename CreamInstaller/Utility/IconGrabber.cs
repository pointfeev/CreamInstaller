using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace CreamInstaller.Utility;

internal static class IconGrabber
{
    internal static Icon ToIcon(this Image image)
    {
        Bitmap dialogIconBitmap = new(image, new Size(image.Width, image.Height));
        return Icon.FromHandle(dialogIconBitmap.GetHicon());
    }

    private static readonly string SteamAppImagesPath = "https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/";
    internal static async Task<Image> GetSteamIcon(string steamAppId, string iconStaticId) => await HttpClientManager.GetImageFromUrl(SteamAppImagesPath + $"/{steamAppId}/{iconStaticId}.jpg");
    internal static async Task<Image> GetSteamClientIcon(string steamAppId, string clientIconStaticId) => await HttpClientManager.GetImageFromUrl(SteamAppImagesPath + $"/{steamAppId}/{clientIconStaticId}.ico");

    internal static Image GetFileIconImage(string path) => File.Exists(path) ? Icon.ExtractAssociatedIcon(path).ToBitmap() : null;

    internal static Image GetNotepadImage() => GetFileIconImage(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\notepad.exe");

    internal static Image GetCommandPromptImage() => GetFileIconImage(Environment.SystemDirectory + @"\cmd.exe");

    internal static Image GetFileExplorerImage() => GetFileIconImage(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\explorer.exe");
}
