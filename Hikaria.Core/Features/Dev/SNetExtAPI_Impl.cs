using Hikaria.Core.SNetworkExt;
using SNetwork;
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

    #region SNet
    [ArchivePatch(typeof(SNet), nameof(SNet.ValidateMasterData))]
    private class SNet__ValidateMasterData__Patch
    {
        private static void Postfix()
        {
            SNetExt.ValidateMasterData();
        }
    }

    [ArchivePatch(typeof(SNet), nameof(SNet.Setup))]
    private class SNet__Setup__Patch
    {
        private static void Postfix()
        {
            SNetExt.Setup();
        }
    }

    [ArchivePatch(typeof(SNet), nameof(SNet.ResetSession))]
    private class SNet__ResetSession__Patch
    {
        private static void Postfix()
        {
            SNetExt.ResetSession();
        }
    }

    [ArchivePatch(typeof(SNet), nameof(SNet.DestroySelfManagedReplicatedObject))]
    private class SNet__DestroySelfManagedReplicatedObject__Patch
    {
        private static void Prefix(GameObject go)
        {
            SNetExt.DestroySelfManagedReplicatedObject(go);
        }
    }
    #endregion

    #region SNet_Capture
    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.SendBufferCommand))]
    private class SNet_Capture__SendBufferCommand__Patch
    {
        private static void Postfix(eBufferType buffer, eBufferOperationType operation, SNet_Player toPlayer = null, ushort bufferID = 0)
        {
            SNetExt.Capture.SendBufferCommand((SNetExt_BufferType)buffer, (SNetExt_BufferOperationType)operation, toPlayer, bufferID);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.CaptureGameState))]
    private class SNet_Capture__CaptureGameState__Patch
    {
        private static void Postfix(eBufferType bufferType)
        {
            SNetExt.Capture.CaptureGameState((SNetExt_BufferType)bufferType);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.RecallGameState))]
    private class SNet_Capture__RecallGameState__Patch
    {
        private static void Postfix(eBufferType bufferType)
        {
            SNetExt.Capture.RecallGameState((SNetExt_BufferType)bufferType);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.SendBuffer))]
    private class SNet_Capture__SendBuffer__Patch
    {
        private static void Postfix(eBufferType bufferType, SNet_Player player = null, bool sendAsMigrationBuffer = false)
        {
            SNetExt.Capture.SendBuffer((SNetExt_BufferType)bufferType, player, sendAsMigrationBuffer);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.InitCheckpointRecall))]
    private class SNet_Capture__InitCheckpointRecall__Patch
    {
        private static void Postfix()
        {
            SNetExt.Capture.InitCheckpointRecall();
        }
    }
    #endregion

    #region SNet_SyncManager
    [ArchivePatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.CleanUpAllButManagersCaptureCallbacks))]
    private class SNet_SyncManager__CleanUpAllButManagersCaptureCallbacks__Patch
    {
        private static void Postfix()
        {
            SNetExt_Capture.CleanUpAllButManagersCaptureCallbacks();
        }
    }
    #endregion
}
