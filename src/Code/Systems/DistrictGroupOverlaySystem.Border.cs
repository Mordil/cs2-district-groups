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
            int districtCount;
            int segmentCount;

            // While any non-default tool is active, area geometry may be changing under the cached snapshot
            // read nodes live so the border tracks the edit
            bool liveNodes = m_ToolSystem.activeTool != m_DefaultToolSystem;
            if (liveNodes)
            {
                /*
                    The live path reads node buffers off EntityManager and appends to the overlay buffer from
                    the main thread, so vanilla's own writers have to land before it starts.
                */
                dependencies.Complete();
                DrawLiveNodeBorders(buffer, outlineAlpha, outlineWidth, out districtCount, out segmentCount);
            }
            else
            {
                NativeList<BorderRing> visibleRings = CullVisibleBorderRings(outlineAlpha, outlineWidth, out districtCount, out segmentCount);

                if (visibleRings.Length == 0)
                {
                    visibleRings.Dispose();
                }
                else
                {
                    DrawBordersJob job = new DrawBordersJob
                    {
                        m_Positions = m_BorderPositions,
                        m_Rings = visibleRings,
                        m_OutlineWidth = outlineWidth,
                        m_OverlayBuffer = buffer,
                    };

                    /*
                        Chained after the buffer's existing writers because we append to the same lists, and
                        after the previous frame's draw so that one handle always covers every job still
                        reading m_BorderPositions. Registered as a writer so OverlayRenderSystem waits on the
                        draw instead of the main thread.
                    */
                    m_BorderJobHandle = job.Schedule(JobHandle.CombineDependencies(dependencies, m_BorderJobHandle));
                    visibleRings.Dispose(m_BorderJobHandle);
                    m_OverlayRenderSystem.AddBufferWriter(m_BorderJobHandle);
                }
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

        // Every visible district's ring, ready for the draw job.
        // Frustum culling and the entity checks stay here because both are managed calls,
        // and both are cheap next to the draw they gate.
        private NativeList<BorderRing> CullVisibleBorderRings(float outlineAlpha, float outlineWidth, out int districtCount, out int segmentCount)
        {
            districtCount = 0;
            segmentCount = 0;
            NativeList<BorderRing> rings = new NativeList<BorderRing>(m_DistrictSnapshots.Count, Allocator.TempJob);

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

                rings.Add(new BorderRing
                {
                    m_Start = row.BorderStart,
                    m_Count = row.BorderCount,
                    m_Color = color,
                });
                segmentCount += row.BorderCount;
            }

            return rings;
        }

        // One district's closed node ring, as a slice of m_BorderPositions.
        private struct BorderRing
        {
            public int m_Start;
            public int m_Count;
            public Color m_Color;
        }

        // The border draw itself.
        private struct DrawBordersJob : IJob
        {
            [ReadOnly] public NativeList<float3> m_Positions;
            [ReadOnly] public NativeList<BorderRing> m_Rings;
            public float m_OutlineWidth;
            public OverlayRenderSystem.Buffer m_OverlayBuffer;

            public void Execute()
            {
                for (int i = 0; i < m_Rings.Length; i++)
                {
                    BorderRing ring = m_Rings[i];

                    // Positions already carry the height offset
                    for (int j = 0; j < ring.m_Count; j++)
                    {
                        float3 a = m_Positions[ring.m_Start + j];
                        float3 b = m_Positions[ring.m_Start + (j + 1 == ring.m_Count ? 0 : j + 1)];
                        m_OverlayBuffer.DrawLine(
                            ring.m_Color,
                            ring.m_Color,
                            0f,
                            (OverlayRenderSystem.StyleFlags)0,
                            new Line3.Segment(a, b),
                            m_OutlineWidth,
                            new float2(1f, 1f) // make the end caps overlap so it looks like 1 line
                        );
                    }
                }
            }
        }
    }
}
