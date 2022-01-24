using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace CreamInstaller;

internal static class ValveDataFile
{
    internal static bool TryDeserialize(string value, out VProperty result)
    {
        result = null;
        try
        {
            result = VdfConvert.Deserialize(value);
            return true;
        }
        catch { }
        return false;
    }

    internal static VToken TryGet(this VToken token, string index)
    {
        try
        {
            return token[index];
        }
        catch { }
        return null;
    }
}