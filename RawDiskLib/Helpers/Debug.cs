#if NETCORE
// ReSharper disable once CheckNamespace
namespace System.Diagnostics
{
    public static class Debug
    {
        public static void WriteLine(string message)
        {
            // Do nothing
        }

        public static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception("Assertion failure");
        }
    }
}
#endif