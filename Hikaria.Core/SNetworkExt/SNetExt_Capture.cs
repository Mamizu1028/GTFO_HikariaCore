using Il2CppInterop.Runtime.Attributes;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Capture : MonoBehaviour, ISNetExt_Manager
{
    [HideFromIl2Cpp]
    public SNetExt_CaptureBuffer PrimedBuffer
    {
        get => m_primedBuffer;
        set
        {
            m_primedBuffer = value;
            IsCapturing = m_primedBuffer != null;
        }
    }

    [HideFromIl2Cpp]
    public bool IsCapturing { get; set; }

    [HideFromIl2Cpp]
    public bool IsRecalling { get; set; }

    [HideFromIl2Cpp]
    public void Setup()
    {
    }

    [HideFromIl2Cpp]
    public void SetupReplication()
    {
        m_bufferCommandPacket = SNetExt.SubManagerReplicator.CreatePacket<pBufferCommand>(typeof(pBufferCommand).FullName, OnReceiveBufferCommand);
        m_bufferCompletionPacket = SNetExt.SubManagerReplicator.CreatePacket<pBufferCompletion>(typeof(pBufferCompletion).FullName, OnReceiveBufferCompletion);
        m_bufferBytesPacket = SNetExt.SubManagerReplicator.CreatePacketBufferBytes($"{typeof(SNetExt_Capture).FullName}.BufferBytes", OnReceiveBufferBytes);
        int length = Enum.GetValues(typeof(SNetExt_BufferType)).Length;
        m_buffers = new SNetExt_CaptureBuffer[length];
        for (int i = 0; i < length; i++)
        {
            m_buffers[i] = new SNetExt_CaptureBuffer((SNetExt_BufferType)i);
        }
    }

    [HideFromIl2Cpp]
    public void OnResetSession()
    {
        for (int i = 0; i < m_buffers.Length; i++)
        {
            m_buffers[i].Clear();
        }
        m_highestBufferID = 1;
        m_passiveMigrationBufferSend = null;
        IsCapturing = false;
    }

    [HideFromIl2Cpp]
    public void OnValidateMasterData()
    {
    }

    internal static void RegisterCaptureCallback(ICaptureCallbackObject syncInterface)
    {
        s_captureCallbackObjects.Add(syncInterface);
    }

    internal static void UnRegisterForDropInCallback(ICaptureCallbackObject syncInterface)
    {
        s_captureCallbackObjects.Remove(syncInterface);
    }

    internal static void CleanUpAllButManagersCaptureCallbacks()
    {
        int i = s_captureCallbackObjects.Count;
        while (i-- > 0)
        {
            var captureCallbackObject = s_captureCallbackObjects[i];
            if (captureCallbackObject == null)
            {
                s_captureCallbackObjects.RemoveAt(i);
                continue;
            }
            var replicator = captureCallbackObject.GetReplicator();
            if (replicator == null || replicator.Type != SNetExt_ReplicatorType.Manager)
            {
                s_captureCallbackObjects.RemoveAt(i);
                continue;
            }
        }
    }

    [HideFromIl2Cpp]
    internal void InitCheckpointRecall()
    {
        IncreaseRecallCount(SNetExt_BufferType.Checkpoint);
    }

    [HideFromIl2Cpp]
    internal void IncreaseRecallCount(SNetExt_BufferType bufferType)
    {
        var captureBuffer = m_buffers[(int)bufferType];
        captureBuffer.data.recallCount++;
    }

    [HideFromIl2Cpp]
    public List<pBuffersSummary> GetBufferSummaries()
    {
        var list = new List<pBuffersSummary>();
        for (int i = 0; i < m_buffers.Length; i++)
        {
            var captureBuffer = m_buffers[i];
            if (captureBuffer.type != SNetExt_BufferType.JoinedHub && captureBuffer.isValid)
            {
                list.Add(new pBuffersSummary(captureBuffer));
            }
        }
        return list;
    }

    [HideFromIl2Cpp]
    public bool HasCorrectMigrationBuffer(ref pBuffersSummary bufferSummary)
    {
        for (int i = 4; i <= 5; i++)
        {
            var buffersSummary = new pBuffersSummary(m_buffers[i]);
            if (buffersSummary.IsSame(ref bufferSummary))
                return true;
        }
        return false;
    }

    [HideFromIl2Cpp]
    public bool TrySendMigrationBuffer(pBuffersSummary bufferSummary, SNetwork.SNet_Player toPlayer)
    {
        var captureBuffer = m_buffers[(int)bufferSummary.bufferType];
        var buffersSummary = new pBuffersSummary(captureBuffer);
        if (!buffersSummary.IsSame(ref bufferSummary))
            return false;
        SendBuffer(captureBuffer.type, toPlayer, true);
        return true;
    }

    [HideFromIl2Cpp]
    public bool TrySendLatestMigrationBuffer(SNetwork.SNet_Player toPlayer, bool forceCapture = false)
    {
        if (SNetwork.SNet.Sync.IsBitSet(SNetwork.SNet.Sync.MASK_IN_SESSION_HUB, (int)SNetwork.SNet.LocalPlayer.Session.mode))
            return false;
        if (!TryGetNewestMigrationBuffer(out var bufferType))
        {
            if (!forceCapture || !SNetwork.SNet.Sync.IsBitSet(SNetwork.SNet.Sync.MASK_MASTER_CAN_RECALL_GAMESTATE, (int)SNetwork.SNet.LocalPlayer.Session.mode))
                return false;

            SNetExt.Capture.CaptureGameState(GetOldestMigrationBuffer());
        }
        SendBuffer(bufferType, toPlayer, false);
        return true;
    }

    [HideFromIl2Cpp]
    public bool GotBuffer(SNetExt_BufferType type)
    {
        var captureBuffer = m_buffers[(int)type];
        return captureBuffer.isValid && SNetwork.SNet.LocalPlayer.Session.levelChecksum == captureBuffer.data.levelChecksum;
    }

    [HideFromIl2Cpp]
    private SNetExt_BufferType GetOldestMigrationBuffer()
    {
        SNetExt_CaptureBuffer captureBuffer = null;
        for (int i = 4; i <= 5; i++)
        {
            var existBuffer = m_buffers[i];
            if (!existBuffer.isValid || SNetwork.SNet.LocalPlayer.Session.levelChecksum != existBuffer.data.levelChecksum)
            {
                captureBuffer = existBuffer;
            }
            else if (captureBuffer == null || existBuffer.data.progressionTime < captureBuffer.data.progressionTime)
            {
                captureBuffer = existBuffer;
            }
        }
        return captureBuffer.type;
    }

    [HideFromIl2Cpp]
    public bool TryGetNewestMigrationBuffer(out SNetExt_BufferType type)
    {
        SNetExt_CaptureBuffer captureBuffer = null;
        for (int i = 4; i <= 5; i++)
        {
            SNetExt_CaptureBuffer existBuffer = m_buffers[i];
            if (existBuffer.isValid && SNetwork.SNet.LocalPlayer.Session.levelChecksum == existBuffer.data.levelChecksum && (captureBuffer == null || existBuffer.data.progressionTime > captureBuffer.data.progressionTime))
            {
                captureBuffer = existBuffer;
            }
        }
        if (captureBuffer != null)
        {
            type = captureBuffer.type;
            return true;
        }
        type = SNetExt_BufferType.JoinedHub;
        return false;
    }

    private void Update()
    {
        if (SNetwork.SNet.IsMaster)
        {
            UpdateMigrationSend();
        }
    }

    [HideFromIl2Cpp]
    private void UpdateMigrationSend()
    {
        if (SNetwork.SNet.MasterManagement.IsMigrating || !SNetwork.SNet.Sync.IsBitSet(SNetwork.SNet.Sync.MASK_PLAYER_READY_TO_START_PLAYING, (int)SNetwork.SNet.LocalPlayer.Session.mode))
        {
            m_passiveMigrationBufferSend = null;
            return;
        }
        if (m_passiveMigrationBufferSend != null)
        {
            if (m_passiveMigrationBufferSend.Update())
            {
                m_passiveMigrationBufferSend = null;
                m_minTimeToSendNextBuffer = Clock.Time + MIGRATION_CAPTURE_INTERVAL_MIN_DELAY;
            }
            return;
        }
        if (m_minTimeToSendNextBuffer > Clock.Time)
            return;
        if (m_migrationTimer >= Clock.Time)
            return;
        var oldestMigrationBuffer = GetOldestMigrationBuffer();
        CaptureGameState(oldestMigrationBuffer);
        var list = new List<SNetwork.SNet_Player>();
        var playersInSession = SNetwork.SNet.SessionHub.PlayersInSession;
        for (int i = 0; i < playersInSession.Count; i++)
        {
            var player = playersInSession[i];
            if (!player.IsLocal && !player.IsBot)
            {
                list.Add(player);
            }
        }
        if (list.Count == 0)
        {
            m_minTimeToSendNextBuffer = Clock.Time + MIGRATION_CAPTURE_INTERVAL_MIN_DELAY;
            return;
        }
        _logger.Error("UpdateMigrationSend, Sending " + oldestMigrationBuffer);
        m_passiveMigrationBufferSend = new SNetExt_BufferSender(5, 0.5f, m_buffers[(int)oldestMigrationBuffer], list, SNetwork.SNet_ChannelType.SessionMigration);
        m_migrationTimer = Clock.Time + MIGRATION_CAPTURE_INTERVAL;
    }

    [HideFromIl2Cpp]
    internal void CaptureGameState(SNetExt_BufferType buffer)
    {
        var bufferCommand = new pBufferCommand
        {
            type = buffer,
            operation = SNetExt_BufferOperationType.StoreGameState
        };
        OnBufferCommand(bufferCommand);
    }

    [HideFromIl2Cpp]
    internal void RecallGameState(SNetExt_BufferType buffer)
    {
        var bufferCommand = new pBufferCommand
        {
            type = buffer,
            operation = SNetExt_BufferOperationType.RecallGameState
        };
        OnBufferCommand(bufferCommand);
    }

    [HideFromIl2Cpp]
    internal void SendBufferCommand(SNetExt_BufferType buffer, SNetExt_BufferOperationType operation, SNetwork.SNet_Player toPlayer = null, ushort bufferID = 0)
    {
        if (toPlayer != null)
        {
            m_bufferCommandPacket.Send(new pBufferCommand
            {
                type = buffer,
                operation = operation,
                bufferID = bufferID
            }, SNetwork.SNet_ChannelType.SessionOrderCritical, toPlayer);
            return;
        }
        m_bufferCommandPacket.Send(new pBufferCommand
        {
            type = buffer,
            operation = operation,
            bufferID = bufferID
        }, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    [HideFromIl2Cpp]
    private void OnReceiveBufferCommand(pBufferCommand command)
    {
        if (SNetwork.SNet.MasterManagement.IsMigrating)
        {
            OnBufferCommand(command);
            return;
        }
        if (SNetExt.Replication.IsLastSenderMaster)
        {
            OnBufferCommand(command);
            return;
        }
    }

    [HideFromIl2Cpp]
    private void OnBufferCommand(pBufferCommand command)
    {
        switch (command.operation)
        {
            case SNetExt_BufferOperationType.StartReceive:
                OpenBuffer(command.type, command.bufferID);
                PrimedBuffer.data.sendingPlayerLookup = SNetExt.Replication.LastSenderID;
                return;
            case SNetExt_BufferOperationType.StoreGameState:
                OpenBuffer(command.type, 0);
                TriggerCapture();
                CloseBuffer(command.type);
                return;
            case SNetExt_BufferOperationType.RecallGameState:
                RecallBuffer(command.type);
                return;
            default:
                return;
        }
    }

    [HideFromIl2Cpp]
    private void OpenBuffer(SNetExt_BufferType type, ushort bufferID = 0)
    {
        if (bufferID == 0)
        {
            m_highestBufferID += 1;
            if (m_highestBufferID == ushort.MaxValue)
            {
                m_highestBufferID = 1;
            }
            bufferID = m_highestBufferID;
        }
        else if (bufferID == 1)
        {
            m_highestBufferID = 1;
        }
        else if (m_highestBufferID < bufferID)
        {
            m_highestBufferID = bufferID;
        }
        PrimedBufferType = type;
        PrimedBuffer = m_buffers[(int)type];
        PrimedBuffer.Clear();
        PrimedBuffer.data.bufferID = bufferID;
    }

    [HideFromIl2Cpp]
    private void TriggerCapture()
    {
        for (int i = 0; i < SNetExt.Replication.ReplicationManagers.Count; i++)
        {
            SNetExt.Replication.ReplicationManagers[i].OnStateCapture();
        }
        for (int j = 0; j < s_captureCallbackObjects.Count; j++)
        {
            s_captureCallbackObjects[j].OnStateCapture();
        }
        PrimedBuffer.data.levelChecksum = SNetwork.SNet.LocalPlayer.Session.levelChecksum;
        PrimedBuffer.data.progressionTime = Clock.ExpeditionProgressionTime;
    }

    [HideFromIl2Cpp]
    internal void CaptureToBuffer(byte[] data, SNetExt_CapturePass captureDataType)
    {
        if (captureDataType == SNetExt_CapturePass.Skip)
            return;

        byte[] array = new byte[data.Length];
        Buffer.BlockCopy(data, 0, array, 0, data.Length);
        PrimedBuffer.GetPass(captureDataType).Add(array);
    }

    [HideFromIl2Cpp]
    private bool IsValidBufferData(SNetwork.SNet_Player sender)
    {
        return SNetwork.SNet.MasterManagement.IsMigrating || SNetExt.Replication.IsLastSenderMaster;
    }

    [HideFromIl2Cpp]
    private void OnReceiveBufferCompletion(pBufferCompletion completion)
    {
        if (!SNetExt.Replication.TryGetLastSender(out var sender, false) || !sender.IsInSessionHub)
            return;
        if (!IsCapturing || PrimedBuffer == null)
            return;
        if (SNetExt.Replication.LastSenderID != PrimedBuffer.data.sendingPlayerLookup)
            return;
        if (PrimedBuffer.type == completion.type && PrimedBuffer.data.bufferID == completion.data.bufferID)
        {
            PrimedBuffer.isValid = true;
            PrimedBuffer.data = completion.data;
            PrimedBuffer = null;
        }
    }

    [HideFromIl2Cpp]
    private void CloseBuffer(SNetExt_BufferType type)
    {
        if (PrimedBuffer != null && PrimedBuffer.type == type)
        {
            PrimedBuffer.isValid = true;
            PrimedBuffer = null;
        }
    }

    [HideFromIl2Cpp]
    public void SendBuffer(SNetExt_BufferType bufferType, SNetwork.SNet_Player player = null, bool sendAsMigrationBuffer = false)
    {
        m_minTimeToSendNextBuffer = Clock.Time + MIGRATION_CAPTURE_INTERVAL_MIN_DELAY;
        var captureBuffer = m_buffers[(int)bufferType];
        if (sendAsMigrationBuffer)
        {
            bufferType = GetOldestMigrationBuffer();
        }
        var bufferCommand = new pBufferCommand
        {
            type = bufferType,
            operation = SNetExt_BufferOperationType.StartReceive,
            bufferID = captureBuffer.data.bufferID
        };
        if (player != null)
        {
            m_bufferCommandPacket.Send(bufferCommand, SNetwork.SNet_ChannelType.SessionOrderCritical, player);
        }
        else
        {
            m_bufferCommandPacket.Send(bufferCommand, SNetwork.SNet_ChannelType.SessionOrderCritical);
        }
        var bufferCompletion = new pBufferCompletion
        {
            type = bufferType,
            data = captureBuffer.data
        };
        byte[] array;
        if (player != null)
        {
            for (int i = 0; i < SNetwork.SNet_CaptureBuffer.PassCount; i++)
            {
                List<byte[]> list = captureBuffer.m_passes[i];
                array = SNetExt_ReplicatedPacketBufferBytes.GetBufferDataBytes(new SNetExt_ReplicatedPacketBufferBytes.BufferData(captureBuffer.data.bufferID, 0));
                int count = list.Count;
                for (int j = 0; j < count; j++)
                {
                    m_bufferBytesPacket.Send(list[j], array, player);
                }
            }
            m_bufferCompletionPacket.Send(bufferCompletion, SNetwork.SNet_ChannelType.SessionOrderCritical, player);
            return;
        }
        for (int k = 0; k < SNetExt_CaptureBuffer.PassCount; k++)
        {
            List<byte[]> list2 = captureBuffer.m_passes[k];
            array = SNetExt_ReplicatedPacketBufferBytes.GetBufferDataBytes(new SNetExt_ReplicatedPacketBufferBytes.BufferData(captureBuffer.data.bufferID, 0));
            int count2 = list2.Count;
            for (int l = 0; l < count2; l++)
            {
                m_bufferBytesPacket.Send(list2[l], array);
            }
        }
        m_bufferCompletionPacket.Send(bufferCompletion, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    [HideFromIl2Cpp]
    public void OnReceiveBufferBytes(byte[] bytes, SNetExt_ReplicatedPacketBufferBytes.BufferData bufferData)
    {
        if (!IsCapturing)
            return;
        if (PrimedBuffer.data.bufferID != bufferData.bufferID)
            return;
        if (PrimedBuffer.data.sendingPlayerLookup != SNetExt.Replication.LastSenderID)
            return;
        if (!SNetwork.SNet.MasterManagement.IsMigrating && !SNetExt.Replication.IsLastSenderMaster)
            return;
        if (bufferData.pass >= SNetExt_CaptureBuffer.PassCount)
            return;
        byte[] array = new byte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
        PrimedBuffer.m_passes[bufferData.pass].Add(array);
    }

    [HideFromIl2Cpp]
    public SNetExt_BufferType PrimedBufferType { get; set; }

    [HideFromIl2Cpp]
    public ulong GetRecallCount(SNetExt_BufferType type)
    {
        var captureBuffer = m_buffers[(int)type];
        if (!captureBuffer.isValid)
        {
            return 0UL;
        }
        return captureBuffer.data.recallCount;
    }

    [HideFromIl2Cpp]
    private void RecallBuffer(SNetExt_BufferType bufferType)
    {
        var captureBuffer = m_buffers[(int)bufferType];
        IsRecalling = true;
        PrimedBufferType = bufferType;
        if (!captureBuffer.isValid)
            return;
        var replicationManagers = SNetExt.Replication.ReplicationManagers;
        for (int i = 0; i < replicationManagers.Count; i++)
        {
            replicationManagers[i].ClearAllLocal();
        }
        SNetExt.Replication.OnRecall();
        for (int j = 0; j < SNetExt_CaptureBuffer.PassCount; j++)
        {
            List<byte[]> list = captureBuffer.m_passes[j];
            int count = list.Count;
            for (int k = 0; k < count; k++)
            {
                byte[] bytes = list[k];
                SNetExt.Replication.RecallBytes(bytes);
            }
        }
        SNetExt.Replication.OnPostRecall();
        IsRecalling = false;
    }

    [HideFromIl2Cpp]
    public static bool TryGetInterface<A, B>(A obj, out B intObj) where A : class where B : UnityEngine.Object
    {
        intObj = obj as B;
        return intObj != null;
    }

    internal SNetExt_ReplicatedPacket<pBufferCommand> m_bufferCommandPacket;
    internal SNetExt_ReplicatedPacket<pBufferCompletion> m_bufferCompletionPacket;
    internal SNetExt_ReplicatedPacketBufferBytes m_bufferBytesPacket;

    private SNetExt_CaptureBuffer[] m_buffers;
    private SNetExt_CaptureBuffer m_primedBuffer;
    private SNetExt_BufferSender m_passiveMigrationBufferSend;
    private float m_migrationTimer;
    private const float MIGRATION_CAPTURE_INTERVAL = 60f;
    private const float MIGRATION_CAPTURE_INTERVAL_MIN_DELAY = 20f;
    private ushort m_highestBufferID = 1;
    private float m_minTimeToSendNextBuffer;
    private static readonly List<ICaptureCallbackObject> s_captureCallbackObjects = new();
    private readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Capture));
}
