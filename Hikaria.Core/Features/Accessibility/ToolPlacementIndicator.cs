using AssetShards;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using UnityEngine;

namespace Hikaria.Core.Features.Accessibility;

[EnableFeatureByDefault]
public class ToolPlacementIndicator : Feature
{
    public override string Name => "工具摆放指示器";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Accessibility");

    public static ToolPlacementIndicator Instance { get; set; }

    public override void Init()
    {
        AssetShardManager.add_OnSharedAsssetLoaded((Action)LoadAssets);
    }

    private static void LoadAssets()
    {
        var explosive = AssetShardManager.GetLoadedAsset(EXPLOSIVE_MINE_PREFAB);
        var glue = AssetShardManager.GetLoadedAsset(GLUE_MINE_PREFAB);
        if (explosive == null || glue == null)
        {
            Instance.RequestDisable("Missing Prefab");
            return;
        }

        explosiveLineRenderPrefab = explosive.Cast<GameObject>().GetComponent<MineDeployerInstance_Detect_Laser>().m_lineRenderer;
        glueLineRenderPrefab = glue.Cast<GameObject>().GetComponent<MineDeployerInstance_Detect_Laser>().m_lineRenderer;
    }

    private static LineRenderer GetLineRenderer(MineDeployerFirstPerson deployer)
    {
        var line = GameObject.Instantiate<LineRenderer>(deployer.m_mineIdToSpawn == 126U || deployer.m_mineIdToSpawn == 144U ? glueLineRenderPrefab : explosiveLineRenderPrefab);
        GameObject.DontDestroyOnLoad(line);
        return line;
    }

    private const string EXPLOSIVE_MINE_PREFAB = "ASSETS/ASSETPREFABS/ITEMS/CONSUMABLES/TRIPMINE/CONSUMABLE_TRIPMINE_EXPLOSIVE_INSTANCE.PREFAB";
    private const string GLUE_MINE_PREFAB = "ASSETS/ASSETPREFABS/ITEMS/CONSUMABLES/TRIPMINE/CONSUMABLE_TRIPMINE_GLUE_INSTANCE.PREFAB";

    private static LineRenderer explosiveLineRenderPrefab;
    private static LineRenderer glueLineRenderPrefab;

    [ArchivePatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.Update))]
    private static class MineDeployerFirstPerson__Update__Patch
    {
        private static void Postfix(MineDeployerFirstPerson __instance)
        {
            if (CurrentLineRenderer == null)
                return;
            if (__instance.m_hasRayHit && __instance.m_lastCanPlace && __instance.CanWield)
            {
                if (!CurrentLineRenderer.gameObject.active)
                    CurrentLineRenderer.gameObject.SetActive(true);

                CurrentLineRenderer.SetPosition(0, __instance.m_lastRayHit.point);
                if (Physics.Raycast(__instance.m_lastRayHit.point, __instance.m_lastRayHit.normal, out var raycastHit, 20f, LayerManager.MASK_STICKY_MINE_TARGETS))
                    CurrentLineRenderer.SetPosition(1, raycastHit.point);
                else
                    CurrentLineRenderer.SetPosition(1, __instance.m_lastRayHit.point + __instance.m_lastRayHit.normal * 20f);
            }
            else if (CurrentLineRenderer.gameObject.active)
            {
                CurrentLineRenderer.gameObject.SetActive(false);
            }
        }
    }

    [ArchivePatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnWield))]
    private static class MineDeployerFirstPerson__OnWield__Patch
    {
        private static void Postfix(MineDeployerFirstPerson __instance)
        {
            if (CurrentLineRenderer == null)
            {
                CurrentLineRenderer = GetLineRenderer(__instance);
            }
        }
    }

    [ArchivePatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnUnWield))]
    private static class MineDeployerFirstPerson__OnUnWield__Patch
    {
        private static void Prefix()
        {
            if (CurrentLineRenderer == null)
                return;
            UnityEngine.Object.Destroy(CurrentLineRenderer);
            CurrentLineRenderer = null;
        }
    }

    private static LineRenderer CurrentLineRenderer;
}
