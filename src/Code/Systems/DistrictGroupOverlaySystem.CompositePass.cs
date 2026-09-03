using Game.Prefabs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        // One shared CustomPassVolume with a single DrawRenderersCustomPass for the labels
        private void EnsureOverlayCompositePass()
        {
            if (m_OverlayPassVolume != null)
            {
                return;
            }

            m_OverlayPassVolumeObject = new GameObject("DistrictGroupsOverlayPassVolume");
            m_OverlayPassVolume = m_OverlayPassVolumeObject.AddComponent<CustomPassVolume>();
            m_OverlayPassVolume.isGlobal = true;
            m_OverlayPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;

            m_LabelCustomPass = (DrawRenderersCustomPass)m_OverlayPassVolume.AddPassOfType<DrawRenderersCustomPass>();
            m_LabelCustomPass.name = "DistrictGroupsLabelCustomPass";

            // renderQueueType stays All so this doesn't depend on knowing which specific
            // AfterPostProcessOpaque/Transparent sub-queue HDMaterial.SetRenderingPass routed the label
            // material into - the layer is what scopes this pass to just the label renderers.
            m_LabelCustomPass.layerMask = 1 << kOverlayLabelLayer;
            m_LabelCustomPass.renderQueueType = CustomPass.RenderQueueType.All;

            // No depth test and no depth write - the labels are a pure screen overlay: whatever this
            // pass draws should sit on top of the already-composited frame, never be discarded against
            // scene depth, and never occlude anything itself.
            m_LabelCustomPass.overrideDepthState = true;
            m_LabelCustomPass.depthCompareFunction = CompareFunction.Always;
            m_LabelCustomPass.depthWrite = false;

            RefreshOverlayCompositePassPriority();

            Mod.log.Info($"Group overlay composite pass ready; label_layer:{kOverlayLabelLayer} " +
                $"label_layer_name:{LayerMask.LayerToName(kOverlayLabelLayer)} priority:{m_OverlayPassVolume.priority}");
        }

        // The vanilla border/fill kept drawing over the labels because the game itself registers a
        // CustomPassVolume at this same AfterPostProcess injection point
        //
        // This calls FindObjectsOfType against the whole scene, so never call this OnUpdate.
        private void RefreshOverlayCompositePassPriority()
        {
            /*
            Volumes sharing an injection point execute in DESCENDING priority order, so
            whichever volume sorts last paints on top
            this volume has to sort after every other AfterPostProcess volume for labels to stay visible.
            */
            float lowestOtherPriority = 0f;
            foreach (CustomPassVolume volume in Object.FindObjectsOfType<CustomPassVolume>())
            {
                if (volume == m_OverlayPassVolume || volume.injectionPoint != CustomPassInjectionPoint.AfterPostProcess)
                {
                    continue;
                }
                lowestOtherPriority = Mathf.Min(lowestOtherPriority, volume.priority);
                Mod.log.Info($"Found existing after-post-process custom pass volume; name:{volume.gameObject.name} " +
                    $"priority:{volume.priority} global:{volume.isGlobal} pass_count:{volume.customPasses.Count}");
            }
            m_OverlayPassVolume.priority = Mathf.Min(lowestOtherPriority, -100f) - 10f;
            m_CompositePassPriorityRefreshed = true;
        }

        private void DestroyOverlayCompositePass()
        {
            if (m_OverlayPassVolumeObject != null)
            {
                Object.Destroy(m_OverlayPassVolumeObject);
                m_OverlayPassVolumeObject = null;
                m_OverlayPassVolume = null;
                m_LabelCustomPass = null;
            }
        }
    }
}
