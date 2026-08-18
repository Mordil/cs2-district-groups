using Game.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        private void UpdateDesaturation()
        {
            EnsureDesaturationVolume();
            int percent = Mod.Settings?.OverlayDesaturationPercent ?? Setting.kDefaultOverlayDesaturationPercent;
            m_ColorAdjustments.saturation.Override(-percent);

            if (!m_DesaturationActive)
            {
                m_DesaturationActive = true;
                m_DesaturationVolume.gameObject.SetActive(true);
                Mod.log.Info("Group overlay desaturation toggled; active:True");
            }
        }

        private void DisableDesaturation()
        {
            m_DesaturationActive = false;
            if (m_DesaturationVolume != null)
            {
                m_DesaturationVolume.gameObject.SetActive(false);
            }
            Mod.log.Info("Group overlay desaturation toggled; active:False");
        }

        private void EnsureDesaturationVolume()
        {
            if (m_DesaturationVolume != null)
            {
                return;
            }

            m_DesaturationVolume = VolumeHelper.CreateVolume("DistrictGroupsDesaturationVolume", VolumeHelper.kOverrideVolumePriority);
            m_DesaturationVolume.isGlobal = true;
            VolumeHelper.GetOrCreateVolumeComponent(m_DesaturationVolume, ref m_ColorAdjustments);
            m_ColorAdjustments.active = true;
            Mod.log.Info("Group overlay desaturation volume created");
        }

        private void DestroyDesaturationVolume()
        {
            if (m_DesaturationVolume != null)
            {
                VolumeHelper.DestroyVolume(m_DesaturationVolume);
                m_DesaturationVolume = null;
                m_ColorAdjustments = null;
                Mod.log.Info("Group overlay desaturation volume destroyed");
            }
            else
            {
                Mod.log.Warn($"{nameof(DestroyDesaturationVolume)} was called, but the volume does not exist.");
            }
        }
    }
}
