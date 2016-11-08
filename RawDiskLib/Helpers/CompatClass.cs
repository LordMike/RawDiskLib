#if NET20 || NET35
using System.Text;

namespace RawDiskLib.Helpers
{
    internal static class CompatClass
    {
        public static void Clear(this StringBuilder sb)
        {
            // https://stackoverflow.com/questions/3227264/how-to-make-stringbuilder-empty-again-in-net-3-5
            sb.Length = 0;
        }
    }
}
#endif