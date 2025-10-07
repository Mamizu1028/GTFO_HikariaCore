using GTFO.API;
using Il2CppInterop.Runtime.Attributes;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replication : MonoBehaviour, ISNetExt_Manager
{
    public const string NETWORK_EVENT_NAME = nameof(SNetExt_Replication);

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replication));

    [HideFromIl2Cpp]
    public void Setup()
    {
        NetworkAPI.RegisterFreeSizedEvent(NETWORK_EVENT_NAME, ReceiveBytes);
    }

    [HideFromIl2Cpp]
    public void SetupReplication()
    {
        m_badPacketQuestion = SNetExt.SubManagerReplicator.CreatePacket<pBadPacketQuestion>(typeof(pBadPacketQuestion).FullName, ReceiveBadPacketQuestion);
        m_badPacketAnswer = SNetExt.SubManagerReplicator.CreatePacket<pBadPacketAnswer>(typeof(pBadPacketAnswer).FullName, ReceiveBadPacketAnswer);
    }

    [HideFromIl2Cpp]
    private void ReceiveBadPacketQuestion(pBadPacketQuestion question)
    {
    }

    [HideFromIl2Cpp]
    private void ReceiveBadPacketAnswer(pBadPacketAnswer answer)
    {
    }

    [HideFromIl2Cpp]
    internal void OnRecall()
    {
    }

    [HideFromIl2Cpp]
    internal void OnPostRecall()
    {
    }

    [HideFromIl2Cpp]
    public void OnResetSession()
    {
        CleanupReplicators();
    }

    [HideFromIl2Cpp]
    public void OnValidateMasterData()
    {
        if (m_replicationManagers != null)
        {
            for (int i = 0; i < m_replicationManagers.Count; i++)
            {
                m_replicationManagers[i].OnValidateMasterData();
            }
        }
    }

    [HideFromIl2Cpp]
    public void CleanupReplicators()
    {
        for (int i = m_assignedReplicators.Count - 1; i > -1; i--)
        {
            var replicator = m_assignedReplicators[i];
            if (replicator != null)
            {
                if (replicator.Type == SNetExt_ReplicatorType.Dynamic)
                {
                    replicator.Despawn();
                }
                else if (replicator.Type == SNetExt_ReplicatorType.SelfManaged)
                {
                    m_assignedReplicators.RemoveAt(i);
                }
            }
            else
            {
                m_assignedReplicators.RemoveAt(i);
            }
        }
    }

    [HideFromIl2Cpp]
    public List<SNetExt_ReplicationManager> ReplicationManagers => m_replicationManagers;

    [HideFromIl2Cpp]
    public void RegisterReplicationManager(SNetExt_ReplicationManager manager)
    {
        m_replicationManagers.Add(manager);
    }

    [HideFromIl2Cpp]
    public void UnregisterReplicationManager(SNetExt_ReplicationManager manager)
    {
        m_replicationManagers.Remove(manager);
    }

    [HideFromIl2Cpp]
    public bool TryGetLastSender(out SNetwork.SNet_Player sender, bool allowCreate = false)
    {
        return SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out sender, allowCreate);
    }

    [HideFromIl2Cpp]
    public bool IsLastSenderMaster => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsMaster;

    [HideFromIl2Cpp]
    public bool IsLastSenderInSessionHub => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsInSessionHub;

    [HideFromIl2Cpp]
    public bool IsLastSenderInSlot => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsInSessionHub && sender.IsInSlot;

    [HideFromIl2Cpp]
    public void RecallBytes(byte[] bytes)
    {
        LastSenderID = 0UL;
        if (TryGetReplicator(bytes, out m_tempReplicator, out m_tempPacketKey, out m_tempPacketIndex))
            m_tempReplicator.ReceiveBytes(m_tempPacketKey, m_tempPacketIndex, bytes);
    }

    [HideFromIl2Cpp]
    private void ReceiveBytes(ulong senderId, byte[] bytes)
    {
        LastSenderID = senderId;
        if (TryGetReplicator(bytes, out m_tempReplicator, out m_tempPacketKey, out m_tempPacketIndex))
            m_tempReplicator.ReceiveBytes(m_tempPacketKey, m_tempPacketIndex, bytes);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicator(string keyHash, out IReplicator replicator)
    {
        if (!s_replicators.TryGetValue(keyHash, out replicator))
            return false;
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicationSupplier<A>(string keyHash, out A supplier) where A : class
    {
        if (TryGetReplicator(keyHash, out var replicator) && replicator.ReplicatorSupplier != null)
        {
            supplier = replicator.ReplicatorSupplier as A;
            if (supplier != null)
                return true;
        }
        supplier = default;
        return false;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicator(pReplicationData data, out IReplicator replicator)
    {
        if (!s_replicators.TryGetValue(data.ReplicatorKeyHash, out replicator))
            return false;
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetReplicator(byte[] bytes, out IReplicator replicator, out string packetKey, out ushort packetIndex)
    {
        if (!s_replicators.TryGetValue(Convert.ToHexString(bytes, 0, 16), out replicator))
        {
            packetKey = string.Empty;
            packetIndex = ushort.MaxValue;
            return false;
        }
        packetKey = Convert.ToHexString(bytes, 16, 16);
        packetIndex = BytesToPacketIndex(bytes.AsSpan(32, 2));
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReplicatorKeyToBytes(string key)
    {
        using MD5 md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplicatorKeyToHash(string key)
    {
        using MD5 md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] PacketIndexToBytes(ushort index)
    {
        return new byte[2]
        {
            (byte)(index & 0xFF),
            (byte)((index >> 8) & 0xFF)
        };
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort BytesToPacketIndex(ReadOnlySpan<byte> bytes)
    {
        return (ushort)((bytes[1] << 8) | bytes[0]);
    }

    [HideFromIl2Cpp]
    private void AddToAssingedList(IReplicator replicator)
    {
        if (!m_assignedReplicators.Contains(replicator))
        {
            m_assignedReplicators.Add(replicator);
        }
    }

    [HideFromIl2Cpp]
    private void RemoveFromAssignedList(IReplicator replicator)
    {
        m_assignedReplicators.Remove(replicator);
    }

    [HideFromIl2Cpp]
    private void ClearReplicatorKey(string keyHash, IReplicator newReplicator)
    {
        if (!s_replicators.TryGetValue(keyHash, out var replicator))
            return;
        if (replicator != null)
        {
            if (replicator == newReplicator)
            {
                _logger.Error("ClearReplicatorKey > This happened!");
                return;
            }

            RemoveFromAssignedList(replicator);
            replicator.Despawn();
        }
    }

    [HideFromIl2Cpp]
    public static void AllocateReplicator(IReplicator replicator)
    {
        if (s_replicators.ContainsKey(replicator.KeyHash))
        {
            _logger.Error($"AllocateReplicator, ID conflict: '{replicator.KeyHash}' ({replicator.Key})");
            return;
        }
        s_replicators[replicator.KeyHash] = replicator;
        s_replicatorKeyToHash[replicator.Key] = replicator.KeyHash;
        s_replicatorHashToKey[replicator.KeyHash] = replicator.Key;
        SNetExt.Replication.AddToAssingedList(replicator);
    }

    [HideFromIl2Cpp]
    public static void AssignReplicatorKey(IReplicator replicator, string key, bool isRecall = false)
    {
        SNetExt.Replication.ClearReplicatorKey(replicator.KeyHash, replicator);
        replicator.Key = key;
        s_replicators[replicator.KeyHash] = replicator;
        s_replicatorKeyToHash[replicator.Key] = replicator.KeyHash;
        s_replicatorHashToKey[replicator.KeyHash] = replicator.Key;
        SNetExt.Replication.AddToAssingedList(replicator);
    }

    [HideFromIl2Cpp]
    public static void DeallocateReplicator(IReplicator replicator)
    {
        var existReplicator = s_replicators[replicator.KeyHash];
        if (existReplicator == replicator)
        {
            s_replicators.Remove(replicator.KeyHash);
            s_replicatorKeyToHash.Remove(replicator.Key);
            s_replicatorHashToKey.Remove(replicator.KeyHash);
        }
        SNetExt.Replication.RemoveFromAssignedList(replicator);
    }

    [HideFromIl2Cpp]
    public static IReplicator AddManagerReplicator(string key)
    {
        var manager = new SNetExt_Replicator_Manager
        {
            Key = key
        };
        AllocateReplicator(manager);
        return manager;
    }

    [HideFromIl2Cpp]
    public static IReplicator AddManagerReplicator(IReplicatorSupplier supplier)
    {
        var manager = new SNetExt_Replicator_Manager
        {
            ReplicatorSupplier = supplier
        };
        AllocateReplicator(manager);
        return manager;
    }

    [HideFromIl2Cpp]
    public static IReplicator AddSelfManagedReplicator(IReplicatorSupplier supplier)
    {
        var replicator = new SNetExt_Replicator_SelfManaged
        {
            ReplicatorSupplier = supplier
        };
        AllocateReplicator(replicator);
        return replicator;
    }

    private static readonly Dictionary<string, IReplicator> s_replicators = new();
    private static readonly Dictionary<string, string> s_replicatorKeyToHash = new();
    private static readonly Dictionary<string, string> s_replicatorHashToKey = new();

    private SNetExt_ReplicatedPacket<pBadPacketAnswer> m_badPacketAnswer;
    private SNetExt_ReplicatedPacket<pBadPacketQuestion> m_badPacketQuestion;

    private readonly List<IReplicator> m_assignedReplicators = new();
    private readonly List<SNetExt_ReplicationManager> m_replicationManagers = new();
    private IReplicator m_tempReplicator;
    private string m_tempPacketKey;
    private ushort m_tempPacketIndex;

    [HideFromIl2Cpp]
    public ulong LastSenderID { get; private set; }
}
