using Hikaria.Core.Interfaces;
using System.Reflection;

namespace Hikaria.Core.Managers;

public sealed class InitializeManager
{
    internal static void OnPluginLoaded()
    {
        if (Instance == null)
            Instance = new InitializeManager();

        Assembly assembly = Assembly.GetExecutingAssembly();

        List<Type> initializableTypes = assembly.GetTypes()
            .Where(t => typeof(IInitializable).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();
        foreach (Type type in initializableTypes)
            InitializableTypes.Add(type);
        ExecuteInit();
    }

    private static void ExecuteInit()
    {
        foreach (Type type in InitializableTypes)
        {
            try
            {
                IInitializable instance = (IInitializable)Activator.CreateInstance(type);
                instance.Init();
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.Message);
            }
        }
    }

    public static void RegisterInitializableType<T>() where T : IInitializable
    {
        if (IsPublicOrInternalClass<T>())
        {
            InitializableTypes.Add(typeof(T));
        }
    }

    private static bool IsPublicOrInternalClass<T>()
    {
        Type type = typeof(T);
        if (type.IsPublic)
        {
            return true;
        }
        if (type.IsNotPublic && type.Assembly == Assembly.GetExecutingAssembly())
        {
            return true;
        }
        return false;
    }

    public static InitializeManager Instance { get; private set; }

    private static readonly HashSet<Type> InitializableTypes = new();
}
