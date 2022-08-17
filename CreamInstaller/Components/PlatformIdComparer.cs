using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CreamInstaller.Components;

internal static class PlatformIdComparer
{
    private static PlatformIdStringComparer stringComparer;
    internal static PlatformIdStringComparer Strings => stringComparer ??= new();

    private static PlatformIdNodeComparer nodeComparer;
    internal static PlatformIdNodeComparer TreeNodes => nodeComparer ??= new();
}

internal class PlatformIdStringComparer : IComparer<string>
{
    public int Compare(string a, string b) =>
        !int.TryParse(a, out _) && !int.TryParse(b, out _) ? string.Compare(a, b, StringComparison.Ordinal)
      : !int.TryParse(a, out int A) ? 1 : !int.TryParse(b, out int B) ? -1
      : A > B ? 1 : A < B ? -1 : 0;
}

internal class PlatformIdNodeComparer : IComparer
{
    public int Compare(object a, object b) =>
        a is not TreeNode A ? 1 : b is not TreeNode B ? -1
      : A.Tag is not Platform pA ? 1 : B.Tag is not Platform pB ? -1
      : pA > pB ? 1 : pA < pB ? -1
      : PlatformIdComparer.Strings.Compare(A.Name, B.Name);
    
}