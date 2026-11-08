#if NETSTANDARD2_0
// Allows `init` accessors and records when compiling for netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
