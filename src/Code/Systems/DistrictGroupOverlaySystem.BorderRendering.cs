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
            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;

            float outlineWidth = Mod.Settings?.OverlayBorderWidth ?? Setting.kDefaultOverlayBorderWidth;

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle _);
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            int districtCount = 0;
            int segmentCount = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                if (m_TypeFilter >= 0 && (int)data.m_Type != m_TypeFilter)
                {
                    continue;
                }

                Color color = data.m_Color;
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(groups[i], isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    Entity district = member.m_District;
                    if (!EntityManager.Exists(district)
                        || EntityManager.HasComponent<Deleted>(district)
                        || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                    {
                        continue;
                    }
                    districtCount++;

                    // roundness=(1,1) bakes rounded end caps into each
                    // segment's own quad, so adjacent segments' caps
                    // overlap exactly at the shared node and cover the
                    // corner notch without a separate draw call per node.
                    DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        float3 a = nodes[j].m_Position;
                        float3 b = nodes[(j + 1) % nodes.Length].m_Position;
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

            if (shouldSample)
            {
                stopwatch.Stop();
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
                Mod.log.Info($"Overlay draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} group_count:{groups.Length} district_count:{districtCount} segment_count:{segmentCount}");
            }
        }
    }
}
