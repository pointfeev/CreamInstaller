using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CreamInstaller.Components;

internal static class PlatformIdComparer
{
    private static StringComparer stringComparer;

    private static NodeComparer nodeComparer;

    private static NodeNameComparer nodeNameComparer;

    private static NodeTextComparer nodeTextComparer;
    internal static StringComparer String => stringComparer ??= new StringComparer();
    internal static NodeComparer Node => nodeComparer ??= new NodeComparer();
    internal static NodeNameComparer NodeName => nodeNameComparer ??= new NodeNameComparer();
    internal static NodeTextComparer NodeText => nodeTextComparer ??= new NodeTextComparer();
}

internal class StringComparer : IComparer<string>
{
    public int Compare(string a, string b) =>
        !int.TryParse(a, out _) && !int.TryParse(b, out _)
            ? string.Compare(a, b, StringComparison.Ordinal)
            : !int.TryParse(a, out int A)
                ? 1
                : !int.TryParse(b, out int B)
                    ? -1
                    : A > B
                        ? 1
                        : A < B
                            ? -1
                            : 0;
}

internal class NodeComparer : IComparer<TreeNode>
{
    public int Compare(TreeNode a, TreeNode b) =>
        a.Tag is not Platform A
            ? 1
            : b.Tag is not Platform B
                ? -1
                : A > B
                    ? 1
                    : A < B
                        ? -1
                        : 0;
}

internal class NodeNameComparer : IComparer
{
    public int Compare(object a, object b) =>
        a is not TreeNode A
            ? 1
            : b is not TreeNode B
                ? -1
                : PlatformIdComparer.Node.Compare(A, B) is int c && c != 0
                    ? c
                    : PlatformIdComparer.String.Compare(A.Name, B.Name);
}

internal class NodeTextComparer : IComparer
{
    public int Compare(object a, object b) =>
        a is not TreeNode A
            ? 1
            : b is not TreeNode B
                ? -1
                : PlatformIdComparer.Node.Compare(A, B) is int c && c != 0
                    ? c
                    : PlatformIdComparer.String.Compare(A.Text, B.Text);
}