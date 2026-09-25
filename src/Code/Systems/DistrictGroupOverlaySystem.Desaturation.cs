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
            if (percent != m_AppliedDesaturationPercent)
            {
                m_ColorAdjustments.saturation.Override(-percent);
                m_AppliedDesaturationPercent = percent;
            }

            if (!m_DesaturationActive)
            {
                m_DesaturationActive = true;
                m_DesaturationVolume.gameObject.SetActive(true);
                Mod.log.Debug("Group overlay desaturation toggled; active:True");
            }
        }

        private void DisableDesaturation()
        {
            m_DesaturationActive = false;
            if (m_DesaturationVolume != null)
            {
                m_DesaturationVolume.gameObject.SetActive(false);
            }
            Mod.log.Debug("Group overlay desaturation toggled; active:False");
        }

        private void EnsureDesaturationVolume()
        {
            if (m_DesaturationVolume != null)
            {
                return;
            }

            bool debugLogging = Mod.log.isDebugEnabled;
            System.Diagnostics.Stopwatch stopwatch = debugLogging ? System.Diagnostics.Stopwatch.StartNew() : null;

            m_DesaturationVolume = VolumeHelper.CreateVolume("DistrictGroupsDesaturationVolume", VolumeHelper.kOverrideVolumePriority);
            m_DesaturationVolume.isGlobal = true;
            VolumeHelper.GetOrCreateVolumeComponent(m_DesaturationVolume, ref m_ColorAdjustments);
            m_ColorAdjustments.active = true;
            m_AppliedDesaturationPercent = int.MinValue; // fresh component, force the first write

            if (debugLogging)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Group overlay desaturation volume created; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3}");
            }
        }

        private void DestroyDesaturationVolume()
        {
            bool debugLogging = Mod.log.isDebugEnabled;
            System.Diagnostics.Stopwatch stopwatch = debugLogging ? System.Diagnostics.Stopwatch.StartNew() : null;

            VolumeHelper.DestroyVolume(m_DesaturationVolume);
            m_DesaturationVolume = null;
            m_ColorAdjustments = null;

            if (debugLogging)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Group overlay desaturation volume destroyed; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3}");
            }
        }
    }
}
