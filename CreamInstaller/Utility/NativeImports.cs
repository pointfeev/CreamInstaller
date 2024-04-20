using System.Runtime.InteropServices;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Utility;

internal static partial class NativeImports
{
    internal const short SWP_NOACTIVATE = 0x0010;
    internal const short SWP_SHOWWINDOW = 0x0040;
    internal const short SWP_NOMOVE = 0x0002;
    internal const short SWP_NOSIZE = 0x0001;

    internal static readonly nint HWND_NOTOPMOST = new(-2);
    internal static readonly nint HWND_TOPMOST = new(-1);

    [LibraryImport("kernel32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetBinaryType([MarshalAs(UnmanagedType.LPStr)] string lpApplicationName,
        out BinaryType lpBinaryType);

    [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial void SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);
}