using Il2CppInterop.Runtime.Attributes;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Capture : MonoBehaviour, ISNetExt_Manager
{
    public enum SNetExt_CaptureState { Idle, Capturing, Recalling }

    [HideFromIl2Cpp]
    public SNetExt_CaptureState State => _state;

    [HideFromIl2Cpp]
    public bool IsCapturing => _state == SNetExt_CaptureState.Capturing;

    [HideFromIl2Cpp]
    public bool IsRecalling => _state == SNetExt_CaptureState.Recalling;

    [HideFromIl2Cpp]
    public SNetExt_CaptureBuffer PrimedBuffer => IsCapturing ? _activeBuffer : null;

    [HideFromIl2Cpp]
    public SNetExt_BufferType PrimedBufferType => _activeBufferType;

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
        _activeBuffer = null;
        _state = SNetExt_CaptureState.Idle;
    }

    [HideFromIl2Cpp]
    public void OnValidateMasterData()
    {
    }

    [HideFromIl2Cpp]
    internal static void RegisterCaptureCallback(ICaptureCallbackObject syncInterface)
    {
        s_captureCallbackObjects.Add(syncInterface);
    }

    [HideFromIl2Cpp]
    internal static void UnRegisterForDropInCallback(ICaptureCallbackObject syncInterface)
    {
        s_captureCallbackObjects.Remove(syncInterface);
    }

    [HideFromIl2Cpp]
    internal static void UnregisterCaptureCallback(ICaptureCallbackObject syncInterface)
        => UnRegisterForDropInCallback(syncInterface);

    [HideFromIl2Cpp]
    internal static void CleanUpAllButManagersCaptureCallbacks()
    {
        int i = s_captureCallbackObjects.Count;
        while (i-- > 0)
        {
            var captureCallbackObject = s_captureCallbackObjects[i];
            if (captureCallbackObject == null || !captureCallbackObject.PersistAcrossSession)
                s_captureCallbackObjects.RemoveAt(i);
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
        if (!captureBuffer.isValid)
        {
            _logger.Warning($"IncreaseRecallCount on invalid buffer {bufferType}");
            return;
        }
        captureBuffer.data.recallCount++;
    }

    [HideFromIl2Cpp]
    public List<pBuffersSummary> GetBufferSummaries()
    {
        _summariesScratch.Clear();
        for (int i = 0; i < m_buffers.Length; i++)
        {
            var captureBuffer = m_buffers[i];
            if (captureBuffer.type != SNetExt_BufferType.JoinedHub && captureBuffer.isValid)
            {
                _summariesScratch.Add(new pBuffersSummary(captureBuffer));
            }
        }
        return _summariesScratch;
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
            if (!TryGetNewestMigrationBuffer(out bufferType))
            {
                _logger.Error("TrySendLatestMigrationBuffer: forceCapture failed to produce usable buffer");
                return false;
            }
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
        m_migrationPlayerScratch.Clear();
        var playersInSession = SNetwork.SNet.SessionHub.PlayersInSession;
        for (int i = 0; i < playersInSession.Count; i++)
        {
            var player = playersInSession[i];
            if (!player.IsLocal && !player.IsBot)
            {
                m_migrationPlayerScratch.Add(player);
            }
        }
        if (m_migrationPlayerScratch.Count == 0)
        {
            m_minTimeToSendNextBuffer = Clock.Time + MIGRATION_CAPTURE_INTERVAL_MIN_DELAY;
            return;
        }

        m_passiveMigrationBufferSend = new SNetExt_BufferSender(5, 0.5f, m_buffers[(int)oldestMigrationBuffer], m_migrationPlayerScratch, SNetwork.SNet_ChannelType.SessionMigration);
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
                if (_activeBuffer != null)
                    _activeBuffer.data.sendingPlayerLookup = SNetExt.Replication.LastSenderID;
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
        _activeBufferType = type;
        _activeBuffer = m_buffers[(int)type];
        _activeBuffer.Clear();
        _activeBuffer.data.bufferID = bufferID;
        _state = SNetExt_CaptureState.Capturing;
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
        if (_activeBuffer != null)
        {
            _activeBuffer.data.levelChecksum = SNetwork.SNet.LocalPlayer.Session.levelChecksum;
            _activeBuffer.data.progressionTime = Clock.ExpeditionProgressionTime;
        }
    }

    [HideFromIl2Cpp]
    internal void CaptureToBuffer(byte[] data, SNetExt_CapturePass captureDataType)
    {
        if (captureDataType == SNetExt_CapturePass.Skip)
            return;
        if (_activeBuffer == null)
            return;

        byte[] array = new byte[data.Length];
        Buffer.BlockCopy(data, 0, array, 0, data.Length);
        _activeBuffer.GetPass(captureDataType).Add(array);
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
        if (_state != SNetExt_CaptureState.Capturing || _activeBuffer == null)
            return;
        if (SNetExt.Replication.LastSenderID != _activeBuffer.data.sendingPlayerLookup)
            return;
        if (_activeBufferType == completion.type && _activeBuffer.data.bufferID == completion.data.bufferID)
        {
            _activeBuffer.isValid = true;
            _activeBuffer.data = completion.data;
            _activeBuffer = null;
            _state = SNetExt_CaptureState.Idle;
        }
    }

    [HideFromIl2Cpp]
    private void CloseBuffer(SNetExt_BufferType type)
    {
        if (_state == SNetExt_CaptureState.Capturing && _activeBufferType == type)
        {
            if (_activeBuffer != null)
                _activeBuffer.isValid = true;
            _activeBuffer = null;
            _state = SNetExt_CaptureState.Idle;
        }
    }

    [HideFromIl2Cpp]
    public void SendBuffer(SNetExt_BufferType bufferType, SNetwork.SNet_Player player = null, bool sendAsMigrationBuffer = false)
    {
        m_minTimeToSendNextBuffer = Clock.Time + MIGRATION_CAPTURE_INTERVAL_MIN_DELAY;
        var actualType = sendAsMigrationBuffer ? GetOldestMigrationBuffer() : bufferType;
        var captureBuffer = m_buffers[(int)actualType];
        var bufferCommand = new pBufferCommand
        {
            type = actualType,
            operation = SNetExt_BufferOperationType.StartReceive,
            bufferID = captureBuffer.data.bufferID
        };
        if (player != null)
            m_bufferCommandPacket.Send(bufferCommand, SNetwork.SNet_ChannelType.SessionOrderCritical, player);
        else
            m_bufferCommandPacket.Send(bufferCommand, SNetwork.SNet_ChannelType.SessionOrderCritical);

        var bufferCompletion = new pBufferCompletion
        {
            type = actualType,
            data = captureBuffer.data
        };
        byte[] array = new byte[3];
        if (player != null)
        {
            for (int i = 0; i < SNetExt_CaptureBuffer.PassCount; i++)
            {
                List<byte[]> list = captureBuffer.m_passes[i];
                SNetExt_ReplicatedPacketBufferBytes.WriteBufferDataBytes(
                    new SNetExt_ReplicatedPacketBufferBytes.BufferData(captureBuffer.data.bufferID, (byte)i),
                    array.AsSpan());
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
            SNetExt_ReplicatedPacketBufferBytes.WriteBufferDataBytes(
                new SNetExt_ReplicatedPacketBufferBytes.BufferData(captureBuffer.data.bufferID, (byte)k),
                array.AsSpan());
            int count2 = list2.Count;
            for (int l = 0; l < count2; l++)
            {
                m_bufferBytesPacket.Send(list2[l], array);
            }
        }
        m_bufferCompletionPacket.Send(bufferCompletion, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    [HideFromIl2Cpp]
    public void OnReceiveBufferBytes(ReadOnlySpan<byte> bytes, SNetExt_ReplicatedPacketBufferBytes.BufferData bufferData)
    {
        if (_state != SNetExt_CaptureState.Capturing) return;
        var buf = _activeBuffer;
        if (buf == null) return;
        if (buf.data.bufferID != bufferData.bufferID) return;
        if (buf.data.sendingPlayerLookup != SNetExt.Replication.LastSenderID) return;
        if (!SNetwork.SNet.MasterManagement.IsMigrating && !SNetExt.Replication.IsLastSenderMaster) return;
        if (bufferData.pass >= SNetExt_CaptureBuffer.PassCount) return;

        buf.m_passes[bufferData.pass].Add(bytes.ToArray());
    }

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
        _state = SNetExt_CaptureState.Recalling;
        _activeBufferType = bufferType;
        try
        {
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
        }
        finally
        {
            _state = SNetExt_CaptureState.Idle;
        }
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

    private SNetExt_CaptureState _state = SNetExt_CaptureState.Idle;
    private SNetExt_CaptureBuffer _activeBuffer;
    private SNetExt_BufferType _activeBufferType;

    private SNetExt_CaptureBuffer[] m_buffers;
    private SNetExt_BufferSender m_passiveMigrationBufferSend;
    private float m_migrationTimer;
    private const float MIGRATION_CAPTURE_INTERVAL = 60f;
    private const float MIGRATION_CAPTURE_INTERVAL_MIN_DELAY = 20f;
    private ushort m_highestBufferID = 1;
    private float m_minTimeToSendNextBuffer;
    private readonly List<SNetwork.SNet_Player> m_migrationPlayerScratch = new(8);
    private readonly List<pBuffersSummary> _summariesScratch = new(8);
    private static readonly List<ICaptureCallbackObject> s_captureCallbackObjects = new();
    private readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Capture));
}
