using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public static class SNetExt
{
    internal static readonly IArchiveLogger Logger = LoaderWrapper.CreateArSubLoggerInstance("SNetExt");

    private static readonly Dictionary<(ulong PlayerLookup, Type DataType), DataWrapper> s_dataWrappersLookup = new(16);
    private static readonly List<ISNetExt_Manager> s_subManagers = new();

    public static SNetExt_Replication Replication { get; private set; }

    public static SNetExt_Replicator SubManagerReplicator { get; private set; }

    public static SNetExt_Capture Capture { get; private set; }

    public static SNetExt_PrefabReplicationManager PrefabReplication { get; private set; }

    internal static GameObject RootObject { get; private set; }

    internal static void Setup()
    {
        if (RootObject != null)
            return;

        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<SNetExt_StateReplicatorProviderWrapper>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<SNetExt_ReplicatorSupplierWrapper>();

        RootObject = new GameObject("SNetExt");
        UnityEngine.Object.DontDestroyOnLoad(RootObject);
        Capture = CreateSubManager<SNetExt_Capture>(true, "Capture");
        Replication = CreateSubManager<SNetExt_Replication>(true, "Replication");
        PrefabReplication = CreateSubManager<SNetExt_PrefabReplicationManager>(false, "PrefabReplication");
        SubManagerReplicator = SNetExt_Replication.AddManagerReplicator("SNetExt_SubManager") as SNetExt_Replicator;

        SetupReplication();
    }

    public static T CreateSubManager<T>(bool createSubGo = false, string name = "") where T : MonoBehaviour, ISNetExt_Manager
    {
        GameObject gameObject;
        if (createSubGo)
        {
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(RootObject.transform, false);
        }
        else
        {
            gameObject = RootObject;
        }
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<T>();
        T t = gameObject.AddComponent<T>();
        t.Setup();
        s_subManagers.Add(t);
        return t;
    }

    public static void SendAllCustomData(SNetwork.SNet_Player sourcePlayer, SNetwork.SNet_Player toPlayer = null)
    {
        foreach (var kvp in s_dataWrappersLookup)
        {
            if (kvp.Key.PlayerLookup == sourcePlayer.Lookup)
            {
                kvp.Value.Send(sourcePlayer, toPlayer);
            }
        }
    }

    public static void SetupCustomData<A>(string eventName, Action<SNetwork.SNet_Player, A> callback) where A : struct, ISNetExt_ReplicatedPlayerData
    {
        SNetExt_ReplicatedPlayerData<A>.Setup(eventName, callback);
    }

    public static void SetLocalCustomData<A>(A data) where A : struct
    {
        if (SNetwork.SNet.LocalPlayer != null)
        {
            StoreCustomData(SNetwork.SNet.LocalPlayer, data);
        }
    }

    public static void SendCustomData<A>(SNetwork.SNet_Player toPlayer = null) where A : struct
    {
        if (toPlayer != null && toPlayer.IsBot) return;
        if (SNetwork.SNet.LocalPlayer == null) return;

        SNetExt_ReplicatedPlayerData<A>.SendData(SNetwork.SNet.LocalPlayer, GetLocalCustomData<A>(), toPlayer);

        if (!SNetwork.SNet.IsMaster) return;

        var allBots = SNetwork.SNet.Core.GetAllBots(true);
        for (int i = 0; i < allBots.Count; i++)
        {
            var bot = allBots[i];
            if (bot == null || !bot.IsBot) continue;
            SNetExt_ReplicatedPlayerData<A>.SendData(bot, LoadCustomData<A>(bot), toPlayer);
        }
    }

    public static A GetLocalCustomData<A>() where A : struct
    {
        if (SNetwork.SNet.LocalPlayer != null)
            return SNetwork.SNet.LocalPlayer.LoadCustomData<A>();

        return new();
    }

    public static A LoadCustomData<A>(this SNetwork.SNet_Player player) where A : struct
    {
        var key = (player.Lookup, typeof(A));
        if (!s_dataWrappersLookup.TryGetValue(key, out var dataWrapper))
        {
            var dataWrapper2 = new DataWrapper<A>();
            s_dataWrappersLookup.Add(key, dataWrapper2);
            return dataWrapper2.Load();
        }
        return ((DataWrapper<A>)dataWrapper).Load();
    }

    public static void StoreCustomData<A>(this SNetwork.SNet_Player player, A data) where A : struct
    {
        var key = (player.Lookup, typeof(A));
        DataWrapper<A> typed;
        if (!s_dataWrappersLookup.TryGetValue(key, out var dataWrapper))
        {
            typed = new DataWrapper<A>();
            s_dataWrappersLookup.Add(key, typed);
        }
        else
        {
            typed = (DataWrapper<A>)dataWrapper;
        }
        typed.Store(player, data);
    }

    public static void StoreCustomDataLocal<A>(this SNetwork.SNet_Player player, A data) where A : struct
    {
        var key = (player.Lookup, typeof(A));
        DataWrapper<A> typed;
        if (!s_dataWrappersLookup.TryGetValue(key, out var dataWrapper))
        {
            typed = new DataWrapper<A>();
            s_dataWrappersLookup.Add(key, typed);
        }
        else
        {
            typed = (DataWrapper<A>)dataWrapper;
        }
        typed.StoreLocal(data);
    }

    private static void SetupReplication()
    {
        for (int i = 0; i < s_subManagers.Count; i++)
        {
            s_subManagers[i].SetupReplication();
        }
    }

    internal static void ResetSession()
    {
        for (int i = 0; i < s_subManagers.Count; i++)
        {
            s_subManagers[i].OnResetSession();
        }
    }

    internal static void ValidateMasterData()
    {
        for (int i = 0; i < s_subManagers.Count; i++)
        {
            s_subManagers[i].OnValidateMasterData();
        }
    }

    internal static void DestroySelfManagedReplicatedObject(GameObject go)
    {
        var components = go.GetComponentsInChildren<SNetExt_StateReplicatorProviderWrapper>();
        for (int i = 0; i < components.Length; i++)
        {
            var stateReplicator = components[i].Provider.GetStateReplicator();
            if (stateReplicator != null)
            {
                var replicator = stateReplicator.Replicator;
                if (stateReplicator is ICaptureCallbackObject captureCallbackObject)
                    SNetExt_Capture.UnRegisterForDropInCallback(captureCallbackObject);
                SNetExt_Replication.DeallocateReplicator(replicator);
            }
        }
    }
}
