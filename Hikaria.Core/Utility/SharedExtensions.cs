using SNetwork;

namespace Hikaria.Core;

public static class SharedExtensions
{
    public static string[] SplitInChunks(this string str, int length)
    {
        return Enumerable.Range(0, (int)Math.Ceiling((double)str.Length / length))
            .Select(i => str.Substring(i * length, Math.Min(length, str.Length - i * length)))
            .ToArray();
    }

    private static UFloat24 s_UFloat24 = new();
    public static float GetFromLowResVector3(this ref LowResVector3 vector, float maxValue)
    {
        s_UFloat24.internalValue1 = vector.vector.x.internalValue;
        s_UFloat24.internalValue2 = vector.vector.y.internalValue;
        s_UFloat24.internalValue3 = vector.vector.z.internalValue;
        return s_UFloat24.Get(maxValue);
    }

    public static void SetToLowResVector3(this ref LowResVector3 vector, float v, float maxValue)
    {
        s_UFloat24.Set(v, maxValue);
        vector.vector = new LowResVector3_Normalized
        {
            x = new UFloat8 { internalValue = s_UFloat24.internalValue1 },
            y = new UFloat8 { internalValue = s_UFloat24.internalValue2 },
            z = new UFloat8 { internalValue = s_UFloat24.internalValue3 }
        };
    }
}
