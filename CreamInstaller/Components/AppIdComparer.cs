using System.Collections.Generic;

namespace CreamInstaller.Components;

internal class AppIdComparer : IComparer<string>
{
    private static AppIdComparer comparer;
    public static AppIdComparer Comparer => comparer ??= new AppIdComparer();

    public int Compare(string a, string b) =>
        a == "ParadoxLauncher" ? -1
      : b == "ParadoxLauncher" ? 1
      : !int.TryParse(a, out _) && !int.TryParse(b, out _) ? string.Compare(a, b)
      : !int.TryParse(a, out int A) ? 1
      : !int.TryParse(b, out int B) ? -1
      : A > B ? 1
      : A < B ? -1
      : 0;
}
