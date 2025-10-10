using Hikaria.Core.SNetworkExt;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;
using UnityEngine;

namespace Hikaria.Core.Features.Dev;

[DoNotSaveToConfig]
[HideInModSettings]
[DisallowInGameToggle]
[EnableFeatureByDefault]
internal class SNetExtAPI_Impl : Feature
{
    public override string Name => "SNetExtAPI Impl";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public new static IArchiveLogger FeatureLogger { get; set; }

    public static bool TryGetVanillaReplicatorWrapper(ushort key, out ISNetExt_Replicator wrapper)
    {
        return SNetExt_Replication.TryGetReplicatorByKeyHash(SNetExt_Replication.ReplicatorKeyToHash($"VanillaWrapper_{key}"), out wrapper);
    }

    #region SNet_Replication
    [ArchivePatch(typeof(SNetwork.SNet_Replication), nameof(SNetwork.SNet_Replication.AssignReplicatorKey))]
    private class SNet_Replication__AssignReplicatorKey__Patch
    {
        private static void Postfix(SNetwork.IReplicator replicator)
        {
            if (!SNetExt_Replication.TryGetReplicatorByKeyHash(SNetExt_Replication.ReplicatorKeyToHash($"VanillaWrapper_{replicator.Key}"), out _))
            {
                SNetExt_Replication.AddVanillaReplicatorWrapper(replicator);
                return;
            }
            FeatureLogger.Error($"Duplicated Vanilla Replicator, Key: {replicator.Key}");
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Replication), nameof(SNetwork.SNet_Replication.ClearReplicatorKey))]
    private class SNet_Replication__ClearReplicatorKey__Patch
    {
        private static void Prefix(ushort key)
        {
            if (SNetExt_Replication.TryGetReplicatorByKeyHash(SNetExt_Replication.ReplicatorKeyToHash($"VanillaWrapper_{key}"), out var wrapper))
            {
                wrapper.Despawn();
            }
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Replication), nameof(SNetwork.SNet_Replication.DeallocateReplicator))]
    private class SNet_Replication__DeallocateReplicator__Patch
    {
        private static void Prefix(SNetwork.IReplicator replicator)
        {
            if (SNetExt_Replication.TryGetReplicatorByKeyHash(SNetExt_Replication.ReplicatorKeyToHash($"VanillaWrapper_{replicator.Key}"), out var wrapper))
            {
                wrapper.Despawn();
            }
        }
    }

    [ArchivePatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
    private class GS_AfterLevel__CleanupAfterExpedition__Patch
    {
        private static void Postfix()
        {
            SNetExt.Replication.CleanupReplicators();
        }
    }
    #endregion

    #region SNet
    [ArchivePatch(typeof(SNetwork.SNet), nameof(SNetwork.SNet.ValidateMasterData))]
    private class SNet__ValidateMasterData__Patch
    {
        private static void Postfix()
        {
            SNetExt.ValidateMasterData();
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet), nameof(SNetwork.SNet.Setup))]
    private class SNet__Setup__Patch
    {
        private static void Prefix()
        {
            SNetExt.Setup();
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet), nameof(SNetwork.SNet.ResetSession))]
    private class SNet__ResetSession__Patch
    {
        private static void Postfix()
        {
            SNetExt.ResetSession();
        }
    }

    //[ArchivePatch(typeof(SNet), nameof(SNet.DestroySelfManagedReplicatedObject))]
    private class SNet__DestroySelfManagedReplicatedObject__Patch
    {
        private static void Prefix(GameObject go)
        {
            SNetExt.DestroySelfManagedReplicatedObject(go);
        }
    }
    #endregion

    #region SNet_Capture
    [ArchivePatch(typeof(SNetwork.SNet_Capture), nameof(SNetwork.SNet_Capture.SendBufferCommand))]
    private class SNet_Capture__SendBufferCommand__Patch
    {
        private static void Postfix(SNetwork.eBufferType buffer, SNetwork.eBufferOperationType operation, SNetwork.SNet_Player toPlayer = null, ushort bufferID = 0)
        {
            SNetExt.Capture.SendBufferCommand((SNetExt_BufferType)buffer, (SNetExt_BufferOperationType)operation, toPlayer, bufferID);
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Capture), nameof(SNetwork.SNet_Capture.CaptureGameState))]
    private class SNet_Capture__CaptureGameState__Patch
    {
        private static void Postfix(SNetwork.eBufferType buffer)
        {
            SNetExt.Capture.CaptureGameState((SNetExt_BufferType)buffer);
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Capture), nameof(SNetwork.SNet_Capture.RecallGameState))]
    private class SNet_Capture__RecallGameState__Patch
    {
        private static void Postfix(SNetwork.eBufferType buffer)
        {
            SNetExt.Capture.RecallGameState((SNetExt_BufferType)buffer);
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Capture), nameof(SNetwork.SNet_Capture.SendBuffer))]
    private class SNet_Capture__SendBuffer__Patch
    {
        private static void Postfix(SNetwork.eBufferType bufferType, SNetwork.SNet_Player player = null, bool sendAsMigrationBuffer = false)
        {
            SNetExt.Capture.SendBuffer((SNetExt_BufferType)bufferType, player, sendAsMigrationBuffer);
        }
    }

    [ArchivePatch(typeof(SNetwork.SNet_Capture), nameof(SNetwork.SNet_Capture.InitCheckpointRecall))]
    private class SNet_Capture__InitCheckpointRecall__Patch
    {
        private static void Postfix()
        {
            SNetExt.Capture.InitCheckpointRecall();
        }
    }
    #endregion

    #region SNet_SyncManager
    [ArchivePatch(typeof(SNetwork.SNet_SyncManager), nameof(SNetwork.SNet_SyncManager.CleanUpAllButManagersCaptureCallbacks))]
    private class SNet_SyncManager__CleanUpAllButManagersCaptureCallbacks__Patch
    {
        private static void Postfix()
        {
            SNetExt_Capture.CleanUpAllButManagersCaptureCallbacks();
        }
    }
    #endregion
}
