namespace Hikaria.Core;

public static class SharedExtensions
{
    public static string[] SplitInChunks(this string str, int length)
    {
        return Enumerable.Range(0, (int)Math.Ceiling((double)str.Length / length))
            .Select(i => str.Substring(i * length, Math.Min(length, str.Length - i * length)))
            .ToArray();
    }
}
