using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;

namespace Hikaria.Core.Features.Fixes;

[HideInModSettings]
[DisallowInGameToggle]
[DoNotSaveToConfig]
[EnableFeatureByDefault]
public class EnemyDamageSync : Feature
{
    public override string Name => "敌人血量同步";

    public override string Description => "将敌人的整体与部位的血量与其他客户端同步\n\n" +
        "该功能应始终保持启用状态！！！";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    public new static IArchiveLogger FeatureLogger { get; set; }

    private static readonly VersionRange versionRange = "1.x.x";

    public static readonly int PatchPriority = 5000;

    public override void OnEnable()
    {
        GameEventAPI.OnSessionMemberChanged += OnSessionMemberChanged;
        CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;
    }

    public override void OnDisable()
    {
        GameEventAPI.OnSessionMemberChanged -= OnSessionMemberChanged;
        CoreAPI.OnPlayerModsSynced -= OnPlayerModsSynced;
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    private static class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        private static bool s_willDamageKill;

        private static int Priority() => PatchPriority;

        private static void Prefix(Dam_EnemyDamageBase __instance, float damage, int limbID)
        {
            if (!SNet.IsMaster) return;

            if (limbID >= 0)
            {
                s_willDamageKill = __instance.WillDamageKill(damage);
            }
        }

        private static void Postfix(Dam_EnemyDamageBase __instance, float damage, int limbID, bool __result)
        {
            if (!SNet.IsMaster || __result) return;

            if (limbID >= 0)
            {
                var limb = __instance.DamageLimbs[limbID];
                if (!limb.IsDestroyed)
                    SendLimbHealth(limb);
            }

            SendEnemyHealth(__instance);
        }
    }

    private static void SendEnemyHealth(Dam_EnemyDamageBase dam)
    {
        s_data.health.Set(dam.Health, dam.HealthMax);
        dam.m_setHealthPacket.Send(s_data, SNet_ChannelType.GameReceiveCritical);
    }

    private static void SendLimbHealth(Dam_EnemyDamageLimb limb)
    {
        if (s_Il2Cpp_players.Count == 0)
            return;

        s_pDestroyLimbData.limbID = (byte)(byte.MaxValue - limb.m_limbID);
        s_pDestroyLimbData.destructionEventData.atPos_Local.SetToLowResVector3(limb.m_health, limb.m_healthMax);
        limb.m_base.m_destroyLimbPacket.Send(s_pDestroyLimbData, SNet_ChannelType.GameReceiveCritical, s_Il2Cpp_players);
    }

    private static pSetHealthData s_data = new();
    private static Dam_EnemyDamageBase.pDestroyLimbData s_pDestroyLimbData = new();
    private static List<SNet_Player> s_players = new();
    private static Il2CppSystem.Collections.Generic.List<SNet_Player> s_Il2Cpp_players = new();

    public void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods)
    {
        if (CoreAPI.IsPlayerInstalledCore(player, versionRange))
        {
            if (!s_players.Any(p => p.Lookup == player.Lookup))
            {
                s_players.Add(player);
                s_Il2Cpp_players.Add(player);
            }
        }
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            if (player.IsLocal)
            {
                s_players.Clear();
                s_Il2Cpp_players.Clear();
            }
            else
            {
                var index = s_players.FindIndex(p => p.Lookup == player.Lookup);
                if (index >= 0)
                {
                    s_players.RemoveAt(index);
                    s_Il2Cpp_players.RemoveAt(index);
                }
            }
        }
    }
}
