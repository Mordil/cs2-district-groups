using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Rendering;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        private void DrawGroupOverlays(bool shouldSample)
        {
            // Fully transparent borders are invisible either way - skip drawing entirely.
            float outlineAlpha = (Mod.Settings?.OverlayBorderAlpha ?? Setting.kDefaultOverlayBorderAlpha) / 100f;
            if (outlineAlpha <= 0f)
            {
                if (shouldSample)
                {
                    Mod.log.Info("Overlay draw skipped, border alpha is 0");
                }
                return;
            }

            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;

            float heightOffset = OverlayHeightOffset;
            float outlineWidth = Mod.Settings?.OverlayBorderWidth ?? Setting.kDefaultOverlayBorderWidth;

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle _);
            int districtCount = 0;
            int segmentCount = 0;
            foreach (KeyValuePair<Entity, List<Color>> entry in m_DistrictGroupColors)
            {
                // Only single-group districts get a border; multi-group districts get a striped fill instead.
                if (entry.Value.Count > 1)
                {
                    continue;
                }

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

                Color color = entry.Value[0];
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
                Mod.log.Info($"Overlay draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} district_count:{districtCount} segment_count:{segmentCount}");
            }
        }
    }
}
