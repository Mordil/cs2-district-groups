using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        private void UpdateFill()
        {
            EnsureFillRoot();
            ApplyFillHeightOffset();

            int saturationSetting = Mathf.Clamp(
                Mod.Settings?.OverlayFillSaturationPercent ?? Setting.kDefaultOverlayFillSaturationPercent,
                0,
                100);
            bool geometryDirty = (m_DirtyFlags & OverlayDirtyFlags.FillGeometry) != 0;
            bool saturationDirty = saturationSetting != m_FillBuiltSaturationPercent;
            if (geometryDirty)
            {
                RebuildFillObjects(saturationSetting);
                m_DirtyFlags &= ~OverlayDirtyFlags.FillGeometry;
                m_FillBuiltSaturationPercent = saturationSetting;
            }
            else if (saturationDirty)
            {
                RecolorFillObjects(saturationSetting);
                m_FillBuiltSaturationPercent = saturationSetting;
            }

            if (!m_FillActive)
            {
                m_FillActive = true;
                m_FillRoot.SetActive(true);
                Mod.log.Info("Group overlay fill toggled; active:True");
            }
        }

        private void DisableFill()
        {
            m_FillActive = false;
            if (m_FillRoot != null)
            {
                m_FillRoot.SetActive(false);
            }
            Mod.log.Info("Group overlay fill toggled; active:False");
        }

        private void EnsureFillRoot()
        {
            if (m_FillRoot != null)
            {
                return;
            }

            m_FillRoot = new GameObject("DistrictGroupsFillRoot");

            Shader shader = Shader.Find("HDRP/Unlit");
            m_FillMaterial = new Material(shader) { name = "DistrictGroupsFillMaterial" };

            // Pushes the fill into HDRP's after-post-process render queue,
            // so it composites in after desaturation Volume instead of being subject to it
            HDMaterial.SetRenderingPass(m_FillMaterial, HDMaterial.RenderingPass.AfterPostProcess);

            Mod.log.Info($"Group overlay, fill material ready; shader:{shader?.name ?? "<null>"}");
        }

        // Mesh vertices are baked with each district's raw node height, the user-tunable offset lives entirely on the root's transform instead,
        private void ApplyFillHeightOffset()
        {
            float heightOffset = OverlayHeightOffset;
            Vector3 position = m_FillRoot.transform.position;
            if (!Mathf.Approximately(position.y, heightOffset))
            {
                m_FillRoot.transform.position = new Vector3(position.x, heightOffset, position.z);
            }
        }

        private void RebuildFillObjects(int saturationSetting)
        {
            DestroyFillObjects();

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            float actualSaturationPercent = MapFillSaturationPercent(saturationSetting);
            float saturation = actualSaturationPercent / 100f;

            int totalVertices = 0;
            foreach (KeyValuePair<Entity, List<Color>> entry in m_DistrictGroupColors)
            {
                // Lighter (desaturated/vibrancy-scaled) variant of each raw group color
                var lightened = new List<Color>(entry.Value.Count);
                foreach (Color baseColor in entry.Value)
                {
                    lightened.Add(Lighten(baseColor, saturation, kFillVibrancy));
                }
                totalVertices += CreateFillObject(entry.Key, lightened);
            }

            stopwatch.Stop();
            Mod.log.Debug($"Group overlay, rebuilt fill meshes; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} fill_count:{m_FillEntries.Count} vertex_count:{totalVertices} saturation_setting:{saturationSetting} saturation_actual_percent:{actualSaturationPercent:F1}");
        }

        // Updates the colors applied to fill textures
        private void RecolorFillObjects(int saturationSetting)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            float actualSaturationPercent = MapFillSaturationPercent(saturationSetting);
            float saturation = actualSaturationPercent / 100f;

            int recoloredCount = 0;
            foreach (KeyValuePair<Entity, FillEntry> entry in m_FillEntries)
            {
                if (!m_DistrictGroupColors.TryGetValue(entry.Key, out List<Color> baseColors))
                {
                    continue;
                }

                var lightened = new List<Color>(baseColors.Count);
                foreach (Color baseColor in baseColors)
                {
                    lightened.Add(Lighten(baseColor, saturation, kFillVibrancy));
                }
                ApplyFillColors(entry.Value, lightened);
                recoloredCount++;
            }

            stopwatch.Stop();
            Mod.log.Debug($"Group overlay, recolored fill meshes; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} recolored_count:{recoloredCount} saturation_setting:{saturationSetting} saturation_actual_percent:{actualSaturationPercent:F1}");
        }

        // Applies a district's colors to an already-built mesh/renderer
        private void ApplyFillColors(FillEntry entry, List<Color> colors)
        {
            MaterialPropertyBlock colorBlock = new MaterialPropertyBlock();
            if (colors.Count > 1)
            {
                if (entry.Texture == null)
                {
                    Mod.log.Warn($"Group overlay, fill recolor expected a texture but found none; object:{entry.Object?.name ?? "<null>"} color_count:{colors.Count}");
                    return;
                }
                entry.Texture.SetPixels(colors.ToArray());
                entry.Texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                colorBlock.SetColor("_UnlitColor", Color.white);
                colorBlock.SetTexture("_UnlitColorMap", entry.Texture);
            }
            else
            {
                colorBlock.SetColor("_UnlitColor", colors[0]);
            }
            entry.Renderer.SetPropertyBlock(colorBlock);
        }

        // Linearly maps the user-facing 0-100% slider onto an actual [kMinFillSaturationPercent, 100] saturation percent
        private static float MapFillSaturationPercent(int displayPercent)
        {
            float t = Mathf.Clamp(displayPercent, 0, 100) / 100f;
            return Mathf.Lerp(kMinFillSaturationPercent, 100f, t);
        }

        // Desaturates and scales vibrancy towards white, so the fill reads as "a lighter" version of the group's color next to the border
        private static Color Lighten(Color color, float desaturation, float vibrancy)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            s *= desaturation;
            v = Mathf.Min(1f, v * vibrancy);
            Color lightened = Color.HSVToRGB(h, s, v);
            lightened.a = color.a;
            return lightened;
        }

        private int CreateFillObject(Entity district, List<Color> colors)
        {
            DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
            if (nodes.Length < 3)
            {
                return 0;
            }

            Vector3[] vertices = new Vector3[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                float3 pos = nodes[i].m_Position;
                vertices[i] = new Vector3(pos.x, pos.y, pos.z);
            }

            List<int> triangles = Triangulate(vertices);
            if (triangles.Count == 0)
            {
                Mod.log.Warn($"Group overlay, fill triangulation produced no triangles; district:{district} nodeCount:{nodes.Length}");
                return 0;
            }

            Mesh mesh = new Mesh { name = $"DistrictGroupsFillMesh_{district.Index}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            // A textured, UV-mapped fill is only needed when there's more than one group color to cycle through
            // a single-color district is cheaper as a flat _UnlitColor tint with no texture at all.
            Texture2D fillTexture = null;
            MaterialPropertyBlock colorBlock = new MaterialPropertyBlock();
            if (colors.Count > 1)
            {
                fillTexture = CreateFillTexture(colors);
                mesh.uv = ComputeStripeUVs(vertices);
                colorBlock.SetColor("_UnlitColor", Color.white);
                colorBlock.SetTexture("_UnlitColorMap", fillTexture);
            }
            else
            {
                colorBlock.SetColor("_UnlitColor", colors[0]);
            }

            mesh.RecalculateBounds();

            // worldPositionStays: false - the mesh's own vertices already carry each district's absolute world X/Z and raw (unoffset) Y,
            // so this object should sit at the root's local origin and let the root's transform.position supply the height offset uniformly
            GameObject fillObject = new GameObject($"Fill_{district.Index}");
            fillObject.transform.SetParent(m_FillRoot.transform, worldPositionStays: false);
            MeshFilter filter = fillObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = fillObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_FillMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            renderer.SetPropertyBlock(colorBlock);

            m_FillEntries[district] = new FillEntry
            {
                Object = fillObject,
                Mesh = mesh,
                Texture = fillTexture,
                Renderer = renderer,
            };
            return vertices.Length;
        }

        // One texel per group color, cycled via ComputeStripeUVs's repeated UVs - a multi-group district's fill
        // reads as a color-cycling stripe pattern
        private static Texture2D CreateFillTexture(List<Color> colors)
        {
            Texture2D texture = new Texture2D(colors.Count, 1, TextureFormat.RGBA32, mipChain: false)
            {
                name = "DistrictGroupsFillTexture",
                filterMode = FilterMode.Point, // hard stripe edges, no bilinear blending across boundaries
                wrapMode = TextureWrapMode.Repeat,
            };
            texture.SetPixels(colors.ToArray());
            // Kept CPU-readable (unlike a typical upload-and-forget texture) so a saturation-only
            // change can call SetPixels/Apply on this same texture again later instead of rebuilding it.
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        // Projects each vertex onto a fixed world-space diagonal so the stripe direction is consistent across every district
        private static Vector2[] ComputeStripeUVs(Vector3[] vertices)
        {
            float2 direction = math.normalize(new float2(1f, 1f));

            float min = float.MaxValue;
            float max = float.MinValue;
            float[] projections = new float[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                float projection = vertices[i].x * direction.x + vertices[i].z * direction.y;
                projections[i] = projection;
                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }

            float extent = Mathf.Max(max - min, 0.01f); // avoid divide-by-zero on a degenerate sliver

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                float t = (projections[i] - min) / extent;
                uvs[i] = new Vector2(t * kStripeRepeatCount, 0f);
            }
            return uvs;
        }

        // Ear-clipping triangulation for a simple polygon to keep within borders
        private static List<int> Triangulate(Vector3[] vertices)
        {
            List<int> indices = new List<int>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                indices.Add(i);
            }

            if (SignedArea(vertices, indices) < 0f)
            {
                indices.Reverse();
            }

            List<int> triangles = new List<int>();
            int guard = 0;
            int maxIterations = vertices.Length * vertices.Length + 8;
            while (indices.Count > 3 && guard++ < maxIterations)
            {
                bool clipped = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = indices[(i - 1 + indices.Count) % indices.Count];
                    int curr = indices[i];
                    int next = indices[(i + 1) % indices.Count];

                    if (!IsConvex(vertices[prev], vertices[curr], vertices[next]))
                    {
                        continue;
                    }
                    if (AnyPointInside(vertices, indices, prev, curr, next))
                    {
                        continue;
                    }

                    triangles.Add(prev);
                    triangles.Add(curr);
                    triangles.Add(next);
                    indices.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped)
                {
                    // loop forever; whatever triangles already exist still render, just an incomplete fill.
                    break;
                }
            }
            if (indices.Count == 3)
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
            }
            return triangles;
        }

        private static float SignedArea(Vector3[] vertices, List<int> indices)
        {
            float area = 0f;
            for (int i = 0; i < indices.Count; i++)
            {
                Vector3 a = vertices[indices[i]];
                Vector3 b = vertices[indices[(i + 1) % indices.Count]];
                area += (b.x - a.x) * (b.z + a.z);
            }
            return area;
        }

        private static bool IsConvex(Vector3 a, Vector3 b, Vector3 c)
        {
            float cross = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
            return cross < 0f;
        }

        private static bool AnyPointInside(Vector3[] vertices, List<int> indices, int prev, int curr, int next)
        {
            Vector3 a = vertices[prev];
            Vector3 b = vertices[curr];
            Vector3 c = vertices[next];
            foreach (int index in indices)
            {
                if (index == prev || index == curr || index == next)
                {
                    continue;
                }
                if (PointInTriangle(vertices[index], a, b, c))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = Cross2D(a, b, p);
            float d2 = Cross2D(b, c, p);
            float d3 = Cross2D(c, a, p);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Cross2D(Vector3 a, Vector3 b, Vector3 p)
        {
            return (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
        }

        private void DestroyFillObjects()
        {
            foreach (FillEntry entry in m_FillEntries.Values)
            {
                if (entry.Object != null)
                {
                    Object.Destroy(entry.Object);
                }
                // Destroying a GameObject doesn't destroy the assets its components merely reference
                if (entry.Mesh != null)
                {
                    Object.Destroy(entry.Mesh);
                }
                if (entry.Texture != null)
                {
                    Object.Destroy(entry.Texture);
                }
            }
            m_FillEntries.Clear();
        }

        private void DestroyFillRoot()
        {
            DestroyFillObjects();
            if (m_FillRoot != null)
            {
                Object.Destroy(m_FillRoot);
                m_FillRoot = null;
            }
            if (m_FillMaterial != null)
            {
                Object.Destroy(m_FillMaterial);
                m_FillMaterial = null;
            }
        }
    }
}
