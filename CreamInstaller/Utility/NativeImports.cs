using System.Runtime.InteropServices;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Utility;

internal static partial class NativeImports
{
    [LibraryImport("kernel32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetBinaryType([MarshalAs(UnmanagedType.LPStr)] string lpApplicationName, out BinaryType lpBinaryType);
}