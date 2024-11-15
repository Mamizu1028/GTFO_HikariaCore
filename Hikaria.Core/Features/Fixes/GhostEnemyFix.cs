using Enemies;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features.Fixes
{
    [EnableFeatureByDefault]
    public class GhostEnemyFix : Feature
    {
        public override string Name => "幽灵尸体修复";
        public override string Description => "避免出现卡敌人尸体";

        public override FeatureGroup Group => EntryPoint.Groups.Fixes;

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Alive), null, ArchivePatch.PatchMethodType.Setter)]
        private class EnemyAgent__set_Alive__Patch
        {
            private static void Postfix(EnemyAgent __instance, bool value)
            {
                if (value)
                    EnemyUpdateManager.Current.Register(__instance, __instance.CourseNode.m_enemyUpdateMode);
            }
        }
    }
}
