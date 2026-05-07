// Polyfill for System.Runtime.CompilerServices.IsExternalInit on netstandard2.0
// Required by C# records and init-only properties.

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
