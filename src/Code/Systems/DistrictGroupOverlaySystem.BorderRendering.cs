using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        private void DrawGroupOverlays()
        {
            bool shouldSample = UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;

            // Fully transparent borders are invisible either way - skip the cache rebuild and draw entirely.
            float outlineAlpha = (Mod.Settings?.OverlayBorderAlpha ?? Setting.kDefaultOverlayBorderAlpha) / 100f;
            if (outlineAlpha <= 0f)
            {
                if (shouldSample)
                {
                    m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
                    Mod.log.Info("Overlay draw skipped, border alpha is 0");
                }
                return;
            }

            int groupVersion = m_GroupSystem.Version;
            if (groupVersion != m_ColorCacheVersion)
            {
                RebuildDistrictColorCache();
                m_ColorCacheVersion = groupVersion;
            }

            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;

            // +0.3 keeps the border a hair above the terrain it's drawn on, on top of the user-tunable offset.
            float heightOffset = (Mod.Settings?.OverlayBorderHeightOffset ?? Setting.kDefaultOverlayBorderHeightOffset) + 0.3f;
            float outlineWidth = Mod.Settings?.OverlayBorderWidth ?? Setting.kDefaultOverlayBorderWidth;

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle _);
            int districtCount = 0;
            int segmentCount = 0;
            foreach (KeyValuePair<Entity, Color> entry in m_DistrictColorCache)
            {
                // A cached district can still die (or lose its Node buffer) between
                // color-cache rebuilds, since district deletion doesn't bump
                // m_GroupSystem.Version
                Entity district = entry.Key;
                if (!EntityManager.Exists(district)
                    || EntityManager.HasComponent<Deleted>(district)
                    || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                {
                    continue;
                }
                districtCount++;

                Color color = entry.Value;
                color.a = outlineAlpha;

                // roundness=(1,1) bakes rounded end caps into each
                // segment's own quad, so adjacent segments' caps
                // overlap exactly at the shared node and cover the
                // corner notch without a separate draw call per node.
                DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                for (int j = 0; j < nodes.Length; j++)
                {
                    float3 a = nodes[j].m_Position + new float3(0f, heightOffset, 0f);
                    float3 b = nodes[(j + 1) % nodes.Length].m_Position + new float3(0f, heightOffset, 0f);
                    buffer.DrawLine(
                        color,
                        color,
                        0f,
                        (OverlayRenderSystem.StyleFlags)0,
                        new Line3.Segment(a, b),
                        outlineWidth,
                        new float2(1f, 1f)
                    );
                    segmentCount++;
                }
            }

            if (shouldSample)
            {
                stopwatch.Stop();
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
                Mod.log.Info($"Overlay draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} district_count:{districtCount} segment_count:{segmentCount}");
            }
        }

        // Districts can belong to more than one visible group - a district
        // is bordered in the color of whichever group claims it first, in
        // group-query iteration order. Only runs on a version/filter change,
        // not every frame (see m_ColorCacheVersion).
        private void RebuildDistrictColorCache()
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            m_DistrictColorCache.Clear();
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < groups.Length; i++)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                if (m_TypeFilter >= 0 && (int)data.m_Type != m_TypeFilter)
                {
                    continue;
                }

                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(groups[i], isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    if (!m_DistrictColorCache.ContainsKey(member.m_District))
                    {
                        m_DistrictColorCache[member.m_District] = data.m_Color;
                    }
                }
            }

            stopwatch.Stop();
            Mod.log.Info($"Overlay color cache rebuilt; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} group_count:{groups.Length} district_count:{m_DistrictColorCache.Count}");
        }
    }
}
