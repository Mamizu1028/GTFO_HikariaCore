namespace Hikaria.Core.Extensions
{
    public static class Il2CppExt
    {
        public static List<T> ToSystemList<T>(this Il2CppSystem.Collections.Generic.List<T> Il2CppList)
        {
            List<T> list = new List<T>();
            foreach (var item in Il2CppList)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
