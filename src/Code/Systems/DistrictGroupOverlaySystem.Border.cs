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
            float outlineAlpha = 1f - (Mod.Settings?.OverlayBorderTransparency ?? Setting.kDefaultOverlayBorderTransparency) / 100f;
            if (outlineAlpha <= 0f)
            {
                if (shouldSample)
                {
                    Mod.log.Debug("Overlay draw skipped, border is fully transparent");
                }
                return;
            }

            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;

            float outlineWidth = Mod.Settings?.OverlayBorderWidth ?? Setting.kDefaultOverlayBorderWidth;

            // Vanilla writers may still be filling this buffer
            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle dependencies);
            dependencies.Complete();
            int districtCount;
            int segmentCount;

            // While any non-default tool is active, area geometry may be changing under the cached snapshot
            // read nodes live so the border tracks the edit
            bool liveNodes = m_ToolSystem.activeTool != m_DefaultToolSystem;
            if (liveNodes)
            {
                DrawLiveNodeBorders(buffer, outlineAlpha, outlineWidth, out districtCount, out segmentCount);
            }
            else
            {
                DrawSnapshotBorders(buffer, outlineAlpha, outlineWidth, out districtCount, out segmentCount);
            }

            if (shouldSample)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Overlay draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} district_count:{districtCount} segment_count:{segmentCount} live_nodes:{liveNodes}");
            }
        }

        private void DrawLiveNodeBorders(OverlayRenderSystem.Buffer buffer, float outlineAlpha, float outlineWidth, out int districtCount, out int segmentCount)
        {
            districtCount = 0;
            segmentCount = 0;

            foreach (KeyValuePair<Entity, DistrictSnapshot> entry in m_DistrictSnapshots)
            {
                // Only single-group districts get a border; multi-group districts get a striped fill instead.
                if (entry.Value.Colors.Count > 1)
                {
                    continue;
                }

                // A cached district can still die (or lose its Node buffer) between snapshot rebuilds
                Entity district = entry.Key;
                if (!EntityManager.Exists(district)
                    || EntityManager.HasComponent<Deleted>(district)
                    || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                {
                    continue;
                }
                districtCount++;

                Color color = entry.Value.Colors[0];
                color.a = outlineAlpha;

                DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                for (int j = 0; j < nodes.Length; j++)
                {
                    float3 a = nodes[j].m_Position + new float3(0f, kOverlayHeightOffset, 0f);
                    float3 b = nodes[(j + 1) % nodes.Length].m_Position + new float3(0f, kOverlayHeightOffset, 0f);
                    buffer.DrawLine(
                        color,
                        color,
                        0f,
                        (OverlayRenderSystem.StyleFlags)0,
                        new Line3.Segment(a, b),
                        outlineWidth,
                        new float2(1f, 1f) // make the end caps overlap so it looks like 1 line
                    );
                    segmentCount++;
                }
            }
        }

        private void DrawSnapshotBorders(OverlayRenderSystem.Buffer buffer, float outlineAlpha, float outlineWidth, out int districtCount, out int segmentCount)
        {
            districtCount = 0;
            segmentCount = 0;

            Camera camera = m_CameraUpdateSystem.activeCamera;
            bool cull = camera != null; // no camera => skip culling, draw everything
            if (cull)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, m_BorderFrustumPlanes);
            }

            foreach (KeyValuePair<Entity, DistrictSnapshot> entry in m_DistrictSnapshots)
            {
                DistrictSnapshot row = entry.Value;

                // Only single-group districts get a border
                if (row.Colors.Count != 1)
                {
                    continue;
                }

                // Same-frame disappearance insurance
                Entity district = entry.Key;
                if (!EntityManager.Exists(district) || EntityManager.HasComponent<Deleted>(district))
                {
                    continue;
                }

                if (cull)
                {
                    Bounds bounds = row.BorderBounds;
                    bounds.Expand(outlineWidth * 2f); // segments extend half a width past the ring's AABB
                    if (!GeometryUtility.TestPlanesAABB(m_BorderFrustumPlanes, bounds))
                    {
                        continue;
                    }
                }
                districtCount++;

                Color color = row.Colors[0];
                color.a = outlineAlpha;

                // BorderPositions already carry the height offset
                float3[] positions = row.BorderPositions;
                int positionCount = positions.Length;
                for (int j = 0; j < positionCount; j++)
                {
                    float3 a = positions[j];
                    float3 b = positions[j + 1 == positionCount ? 0 : j + 1];
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
        }
    }
}
