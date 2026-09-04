using System.Collections.Generic;
using Game.Areas;
using Game.Prefabs;
using Game.Rendering;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TextCore.LowLevel;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
        private void UpdateGroupLabels(bool shouldSample)
        {
            bool enabled = Mod.Settings?.OverlayEnableGroupLabels ?? Setting.kDefaultOverlayEnableGroupLabels;
            if (!enabled)
            {
                if (m_LabelsActive)
                {
                    DisableLabels();
                }
                return;
            }

            EnsureLabelRoot();

            if ((m_DirtyFlags & OverlayDirtyFlags.Labels) != 0)
            {
                RebuildLabelEntries();
                m_DirtyFlags &= ~OverlayDirtyFlags.Labels;
            }

            if (!m_LabelsActive)
            {
                m_LabelsActive = true;
                m_LabelRoot.SetActive(true);

                if (!m_CompositePassPriorityRefreshed)
                {
                    RefreshOverlayCompositePassPriority();
                }

                Mod.log.Info("Group overlay labels toggled; active:True");
            }

            DrawGroupLabels(shouldSample);

            if (shouldSample)
            {
                LogLabelMemorySample();
            }
        }

        private void DisableLabels()
        {
            m_LabelsActive = false;

            if (m_LabelRoot != null)
            {
                m_LabelRoot.SetActive(false);
            }

            Mod.log.Info("Group overlay labels toggled; active:False");
        }

        private void EnsureLabelRoot()
        {
            if (m_LabelRoot != null)
            {
                return;
            }

            m_LabelRoot = new GameObject("DistrictGroupsLabelRoot");

            
            // Since this is all constant and never moves, we set it once rather than every frame
            m_LabelRoot.transform.position = new Vector3(0f, kOverlayHeightOffset + kLabelHeightOffset, 0f);

            EnsureOverlayCompositePass();
        }

        // Hidden TextMeshPro used only to bake glyph meshes - never itself rendered (its MeshRenderer
        // stays disabled). Reused for every group: bake, copy the mesh out, move on to the next name.
        private void EnsureLabelBaker()
        {
            if (m_LabelBaker != null)
            {
                return;
            }

            m_LabelBakerObject = new GameObject("DistrictGroupsLabelBaker");
            m_LabelBaker = m_LabelBakerObject.AddComponent<TextMeshPro>();
            m_LabelBaker.fontSize = kLabelFontSize;
            m_LabelBaker.alignment = TextAlignmentOptions.Center;
            m_LabelBaker.enableWordWrapping = false;

            // TMP_Text requires a RectTransform even for this 3D component, and a freshly
            // added one defaults far too narrow - word-wrapping every single character onto its own line.
            RectTransform rectTransform = m_LabelBakerObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(2000f, 200f);
            }

            
            // Rebuild the font asset by crawling through resources as necessary to find matching fonts
            TMP_FontAsset resolvedFont = ResolveOverlayFont();
            if (resolvedFont != null)
            {
                m_LabelBaker.font = resolvedFont;
            }
            if (!m_LoggedLabelFontDiagnostics)
            {
                m_LoggedLabelFontDiagnostics = true;
                Mod.log.Info($"Group overlay label font resolved; font_name:{m_LabelBaker.font?.name ?? "<null>"} " +
                    $"glyph_count:{m_LabelBaker.font?.characterTable?.Count ?? 0} " +
                    $"fallback_count:{m_LabelBaker.font?.fallbackFontAssetTable?.Count ?? 0} " +
                    $"material_shader:{m_LabelBaker.font?.material?.shader?.name ?? "<null>"}");
            }

            MeshRenderer bakerRenderer = m_LabelBakerObject.GetComponent<MeshRenderer>();
            if (bakerRenderer != null)
            {
                bakerRenderer.enabled = false; // this object is never actually drawn - baking only
            }
        }

        // Clones the resolved font's own TMP SDF material. Acts as the shared template for every label's primary renderer,
        // and as the base every fallback-font submesh's own material is cloned
        //
        // Outline needs the shader's "OUTLINE_ON" keyword enabled on the material
        private void EnsureLabelMaterial()
        {
            if (m_LabelMaterial != null)
            {
                return;
            }

            EnsureLabelBaker();

            Material sourceMaterial = m_LabelBaker.font?.material;
            m_LabelMaterial = sourceMaterial != null
                ? new Material(sourceMaterial) { name = "DistrictGroupsLabelMaterial" }
                : null;

            if (m_LabelMaterial == null)
            {
                return;
            }

            m_LabelMaterial.SetFloat("_FaceDilate", 0f);
            m_LabelMaterial.SetColor("_FaceColor", Color.white);

            m_LabelMaterial.SetColor("_OutlineColor", Color.black);
            m_LabelMaterial.SetFloat("_OutlineWidth", 0.2f);
            m_LabelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);

            m_LabelMaterial.SetFloat("_ScaleRatioA", 1f);
            m_LabelMaterial.SetFloat("_ScaleRatioB", 1f);
            m_LabelMaterial.SetFloat("_ScaleRatioC", 1f);

            // TMP's SDF shader doesn't have a _SurfaceType property, so borrow it from a scratch material
            Material scratchMaterial = new Material(Shader.Find("HDRP/Unlit"));
            HDMaterial.SetRenderingPass(scratchMaterial, HDMaterial.RenderingPass.AfterPostProcess);
            m_LabelMaterial.renderQueue = scratchMaterial.renderQueue;
            Object.Destroy(scratchMaterial);

            Mod.log.Info($"Group overlay, label material ready; shader:{m_LabelMaterial.shader?.name ?? "<null>"}");
        }

        // Front-loads the label subsystem's one-time costs
        // 
        // Does NOT pre-bake the label entries themselves, as that requires district Geometry
        private void PrewarmLabelAssets()
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            EnsureLabelRoot();

            // Keep the root's active state in sync with m_LabelsActive
            m_LabelRoot.SetActive(false);
            EnsureLabelBaker();
            EnsureLabelMaterial();

            int nameCount = 0;
            if (m_LabelBaker.font != null)
            {
                // Warm every group's name regardless of the current type filter
                using NativeArray<Entity> groups = m_GroupSystem.GetGroups(Allocator.Temp);
                for (int i = 0; i < groups.Length; i++)
                {
                    DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                    m_LabelBaker.font.HasCharacters(data.m_Name.ToString(), out _, searchFallbacks: true, tryAddCharacter: true);
                    nameCount++;
                }
            }

            stopwatch.Stop();
            Mod.log.Info($"Prewarmed group label assets; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F1} name_count:{nameCount}");
        }

        // Rebuilds one label entry per snapshot group
        private void RebuildLabelEntries()
        {
            bool debugLogging = Mod.Settings?.EnableDebugLogging ?? false;
            System.Diagnostics.Stopwatch stopwatch = debugLogging ? System.Diagnostics.Stopwatch.StartNew() : null;

            m_LabelSeenGroupsScratch.Clear();
            foreach (KeyValuePair<Entity, GroupSnapshot> row in m_GroupSnapshots)
            {
                Entity group = row.Key;

                // A group deletion bumps the composition version, this guard is same-frame disappearance insurance
                if (!EntityManager.Exists(group) || !EntityManager.HasComponent<DistrictGroupData>(group))
                {
                    continue;
                }

                // The name is deliberately read live so renames show up from a Labels-only rebuild
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);

                m_LabelSeenGroupsScratch.Add(group);
                ApplyLabelEntry(group, data.m_Name.ToString(), row.Value.Center);
            }

            m_LabelStaleGroupsScratch.Clear();
            foreach (Entity existing in m_LabelEntries.Keys)
            {
                if (!m_LabelSeenGroupsScratch.Contains(existing))
                {
                    m_LabelStaleGroupsScratch.Add(existing);
                }
            }
            foreach (Entity entity in m_LabelStaleGroupsScratch)
            {
                DestroyLabelEntry(entity);
            }

            // New/moved/re-baked entries need a pose from DrawGroupLabels even if the camera is idle
            m_LabelTransformsDirty = true;

            if (debugLogging)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Overlay labels rebuilt; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} label_count:{m_LabelEntries.Count}");
            }
        }

        // Creates the label GameObject the first time a group is seen; otherwise updates existing instances
        private void ApplyLabelEntry(Entity group, string name, float3 center)
        {
            // The local position carries the raw center, the root's transform carries the shared height offset,
            // so the world position tracked for the per-frame distance-scale math re-adds it here.
            Vector3 localPosition = new Vector3(center.x, center.y, center.z);
            float3 worldPosition = center + new float3(0f, kOverlayHeightOffset + kLabelHeightOffset, 0f);

            if (m_LabelEntries.TryGetValue(group, out LabelEntry existing))
            {
                existing.Transform.localPosition = localPosition;
                existing.Position = worldPosition;
                if (existing.Name != name)
                {
                    BakeLabelMesh(group, name);
                }
                return;
            }

            EnsureLabelBaker();
            EnsureLabelMaterial();

            GameObject labelObject = new GameObject($"Label_{group.Index}");
            labelObject.transform.SetParent(m_LabelRoot.transform, worldPositionStays: false);
            labelObject.transform.localPosition = localPosition;

            m_LabelEntries[group] = new LabelEntry
            {
                Object = labelObject,
                Transform = labelObject.transform,
                SubMeshes = new List<LabelSubMesh>(),
                Name = null,
                Position = worldPosition,
            };
            BakeLabelMesh(group, name);
        }

        // Bakes name through the shared TextMeshPro baker and copies the result into this group's own per-submesh Meshes/Materials
        private void BakeLabelMesh(Entity group, string name)
        {
            LabelEntry entry = m_LabelEntries[group];

            // Diagnostic for non-Latin glyph coverage
            if (m_LabelBaker.font != null
                && !m_LabelBaker.font.HasCharacters(name, out uint[] missingCharacters, searchFallbacks: true, tryAddCharacter: true))
            {
                string missingHex = string.Join(",", System.Array.ConvertAll(missingCharacters, c => $"U+{c:X4}"));
                Mod.log.Warn($"Label font missing glyphs; group:{group} name:{name} missing_characters:{missingHex}");
            }

            m_LabelBaker.text = name;
            m_LabelBaker.ForceMeshUpdate();

            // TMP never destroys a TMP_SubMesh child once created
            foreach (TMP_SubMesh subMesh in m_LabelBakerObject.GetComponentsInChildren<TMP_SubMesh>())
            {
                Renderer subMeshRenderer = subMesh.GetComponent<Renderer>();
                if (subMeshRenderer != null)
                {
                    subMeshRenderer.enabled = false;
                }
            }

            TMP_TextInfo textInfo = m_LabelBaker.textInfo;
            List<LabelSubMesh> subMeshes = entry.SubMeshes;
            int activeCount = 0;
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
                if (meshInfo.vertexCount <= 0)
                {
                    continue; // this material slot exists (maybe from an earlier, different bake) but this name doesn't use it
                }

                LabelSubMesh labelSubMesh;
                if (activeCount < subMeshes.Count)
                {
                    labelSubMesh = subMeshes[activeCount];
                }
                else
                {
                    labelSubMesh = CreateLabelSubMesh(entry.Object, group, activeCount);
                    subMeshes.Add(labelSubMesh);
                }

                // Reuse the slot's Mesh object across bakes rather than destroying and re-allocating a native mesh per rename
                Mesh bakedMesh = labelSubMesh.Mesh;
                if (bakedMesh == null)
                {
                    bakedMesh = new Mesh { name = $"DistrictGroupsLabelMesh_{group.Index}_{activeCount}" };
                    labelSubMesh.Mesh = bakedMesh;
                    labelSubMesh.Filter.sharedMesh = bakedMesh;
                }

                FillMeshFromTMPMeshInfo(bakedMesh, meshInfo, out Vector2[] originalUv2);
                labelSubMesh.BakedUv2 = originalUv2;
                labelSubMesh.ScaledUv2 = labelSubMesh.ScaledUv2 != null && labelSubMesh.ScaledUv2.Length == originalUv2.Length
                    ? labelSubMesh.ScaledUv2
                    : new Vector2[originalUv2.Length];

                if (i == 0)
                {
                    labelSubMesh.Renderer.sharedMaterial = m_LabelMaterial;
                }
                else
                {
                    if (labelSubMesh.Material == null)
                    {
                        labelSubMesh.Material = new Material(m_LabelMaterial) { name = $"DistrictGroupsLabelMaterial_{group.Index}_{activeCount}" };
                    }
                    CopyAtlasParameters(meshInfo.material, labelSubMesh.Material);
                    labelSubMesh.Renderer.sharedMaterial = labelSubMesh.Material;
                }

                activeCount++;
            }

            // Fewer submeshes than before; drop the now-unused trailing ones rather than leaving stale hidden GameObjects/materials around
            for (int i = subMeshes.Count - 1; i >= activeCount; i--)
            {
                DestroyLabelSubMesh(subMeshes[i]);
                subMeshes.RemoveAt(i);
            }

            entry.Name = name;
            // The freshly baked uv2 is back at unscaled calibration - force DrawGroupLabels to
            // re-apply the current camera-distance scale on its next pass.
            entry.LastAppliedScale = 0f;
        }

        private static LabelSubMesh CreateLabelSubMesh(GameObject parent, Entity group, int index)
        {
            GameObject subMeshObject = new GameObject($"Label_{group.Index}_SubMesh{index}");
            subMeshObject.layer = kOverlayLabelLayer; // picked up by the CompositePass.cs DrawRenderersCustomPass, not the normal camera pass
            subMeshObject.transform.SetParent(parent.transform, worldPositionStays: false);
            subMeshObject.transform.localPosition = Vector3.zero;

            MeshFilter filter = subMeshObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = subMeshObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return new LabelSubMesh
            {
                Object = subMeshObject,
                Filter = filter,
                Renderer = renderer,
                Mesh = null,
                Material = null,
                BakedUv2 = null,
                ScaledUv2 = null,
            };
        }

        private static void FillMeshFromTMPMeshInfo(Mesh mesh, in TMP_MeshInfo meshInfo, out Vector2[] originalUv2)
        {
            int vertexCount = meshInfo.vertexCount;
            if (vertexCount <= 0)
            {
                mesh.Clear();
                originalUv2 = System.Array.Empty<Vector2>();
                return;
            }
            int indexCount = (vertexCount >> 2) * 6; // 4 vertices + 6 indices (2 triangles) per glyph quad
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs0 = new Vector2[vertexCount];
            Vector2[] uvs2 = new Vector2[vertexCount];
            Color32[] colors32 = new Color32[vertexCount];
            int[] triangles = new int[indexCount];
            System.Array.Copy(meshInfo.vertices, 0, vertices, 0, vertexCount);
            System.Array.Copy(meshInfo.uvs0, 0, uvs0, 0, vertexCount);
            System.Array.Copy(meshInfo.uvs2, 0, uvs2, 0, vertexCount);
            System.Array.Copy(meshInfo.colors32, 0, colors32, 0, vertexCount);
            System.Array.Copy(meshInfo.triangles, 0, triangles, 0, indexCount);

            mesh.Clear();
            if (vertexCount > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.vertices = vertices;
            mesh.uv = uvs0;
            mesh.uv2 = uvs2;
            mesh.colors32 = colors32;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            originalUv2 = uvs2;
        }

        // swaps in the fallback font's own atlas texture on our shared-template clone,
        // so a fallback-serviced submesh samples the right atlas instead of the primary font's.
        private static void CopyAtlasParameters(Material source, Material target)
        {
            if (source == null || target == null)
            {
                return;
            }
            target.mainTexture = source.mainTexture;
            CopyFloatIfPresent(source, target, "_GradientScale");
            CopyFloatIfPresent(source, target, "_TextureWidth");
            CopyFloatIfPresent(source, target, "_TextureHeight");
        }

        private static void CopyFloatIfPresent(Material source, Material target, string property)
        {
            if (source.HasProperty(property) && target.HasProperty(property))
            {
                target.SetFloat(property, source.GetFloat(property));
            }
        }

        private static void DestroyLabelSubMesh(LabelSubMesh subMesh)
        {
            if (subMesh.Mesh != null)
            {
                Object.Destroy(subMesh.Mesh);
            }
            if (subMesh.Material != null)
            {
                Object.Destroy(subMesh.Material);
            }
            if (subMesh.Object != null)
            {
                Object.Destroy(subMesh.Object);
            }
        }

        // Builds the primary font plus its fallback chain
        private TMP_FontAsset ResolveOverlayFont()
        {
            if (!m_PrefabSystem.TryGetSingletonPrefab(m_OverlayConfigQuery, out OverlayConfigurationPrefab config)
                || config.m_FontInfos == null || config.m_FontInfos.Length == 0)
            {
                Mod.log.Warn("Could not resolve OverlayConfigurationPrefab for label font, falling back to none;");
                return null;
            }

            TMP_FontAsset primary = CreateFont(config.m_FontInfos[0]);
            primary.fallbackFontAssetTable = new List<TMP_FontAsset>(config.m_FontInfos.Length - 1);
            for (int i = 1; i < config.m_FontInfos.Length; i++)
            {
                primary.fallbackFontAssetTable.Add(CreateFont(config.m_FontInfos[i]));
            }
            return primary;
        }

        private static TMP_FontAsset CreateFont(FontInfo info)
        {
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
                info.m_Font, info.m_SamplingPointSize, info.m_AtlasPadding,
                GlyphRenderMode.SDFAA_HINTED, info.m_AtlasWidth, info.m_AtlasHeight);
            font.material.SetFloat("_FaceDilate", 1f);
            return font;
        }

        // Full spherical billboard - each label's rotation matches the camera's exactly, so it stands
        // upright and faces the player head-on from any angle (yaw and pitch alike), unlike vanilla's
        // district names which just lie flat on the ground.
        private void DrawGroupLabels(bool shouldSample)
        {
            if (m_LabelEntries.Count == 0)
            {
                return;
            }

            if (!m_CameraUpdateSystem.TryGetLODParameters(out LODParameters lodParameters))
            {
                return;
            }
            float3 cameraPosition = new float3(
                lodParameters.cameraPosition.x, lodParameters.cameraPosition.y, lodParameters.cameraPosition.z);

            Camera activeCamera = m_CameraUpdateSystem.activeCamera;
            if (activeCamera == null)
            {
                return;
            }

            // Matching the camera's rotation outright
            Quaternion labelRotation = activeCamera.transform.rotation;

            bool rotationChanged = !labelRotation.Equals(m_LastLabelCameraRotation);
            bool cameraMoved = !cameraPosition.Equals(m_LastLabelCameraPosition);
            if (!m_LabelTransformsDirty && !rotationChanged && !cameraMoved)
            {
                if (shouldSample)
                {
                    Mod.log.Debug($"Overlay label draw sample; camera_idle:True label_count:{m_LabelEntries.Count}");
                }
                return;
            }

            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;
            float minScale = float.MaxValue;
            float maxScale = float.MinValue;

            bool applyRotation = rotationChanged || m_LabelTransformsDirty;
            foreach (LabelEntry entry in m_LabelEntries.Values)
            {
                if (applyRotation)
                {
                    entry.Transform.rotation = labelRotation;
                }

                float scale = AreaUtils.CalculateLabelScale(cameraPosition, entry.Position);

                // The transform scale and the uv2 SDF recalibration are gated together so they can never drift apart.
                if (math.abs(scale - entry.LastAppliedScale) > kLabelScaleRescaleEpsilon * entry.LastAppliedScale)
                {
                    entry.Transform.localScale = new Vector3(scale, scale, scale);
                    ApplyLabelSdfScale(entry, scale);
                    entry.LastAppliedScale = scale;
                }

                if (shouldSample)
                {
                    minScale = math.min(minScale, scale);
                    maxScale = math.max(maxScale, scale);
                }
            }

            m_LastLabelCameraPosition = cameraPosition;
            m_LastLabelCameraRotation = labelRotation;
            m_LabelTransformsDirty = false;

            if (shouldSample)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Overlay label draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} " +
                    $"label_count:{m_LabelEntries.Count} camera_x:{cameraPosition.x:F1} camera_y:{cameraPosition.y:F1} camera_z:{cameraPosition.z:F1} " +
                    $"scale_min:{minScale:F3} scale_max:{maxScale:F3}");
            }
        }

        // Replicates TMPro.TextMeshPro.UpdateSDFScale's own per-vertex uv2.y rewrite
        private static void ApplyLabelSdfScale(LabelEntry entry, float scale)
        {
            foreach (LabelSubMesh subMesh in entry.SubMeshes)
            {
                if (subMesh.Mesh == null || subMesh.BakedUv2 == null)
                {
                    continue;
                }

                Vector2[] bakedUv2 = subMesh.BakedUv2;
                Vector2[] scaledUv2 = subMesh.ScaledUv2;
                for (int i = 0; i < scaledUv2.Length; i++)
                {
                    scaledUv2[i] = new Vector2(bakedUv2[i].x, bakedUv2[i].y * scale);
                }
                subMesh.Mesh.uv2 = scaledUv2;
            }
        }

        // Standing memory footprint estimate for the label subsystem
        private void LogLabelMemorySample()
        {
            long meshBytes = 0;
            foreach (LabelEntry entry in m_LabelEntries.Values)
            {
                foreach (LabelSubMesh subMesh in entry.SubMeshes)
                {
                    if (subMesh.Mesh != null)
                    {
                        meshBytes += EstimateLabelMeshBytes(subMesh);
                    }
                }
            }

            long atlasBytes = 0;
            int atlasCount = 0;
            if (m_LabelBaker != null && m_LabelBaker.font != null)
            {
                atlasBytes += EstimateFontAtlasBytes(m_LabelBaker.font, ref atlasCount);
                if (m_LabelBaker.font.fallbackFontAssetTable != null)
                {
                    foreach (TMP_FontAsset fallback in m_LabelBaker.font.fallbackFontAssetTable)
                    {
                        atlasBytes += EstimateFontAtlasBytes(fallback, ref atlasCount);
                    }
                }
            }

            Mod.log.Debug($"Overlay label memory sample; label_count:{m_LabelEntries.Count} mesh_bytes:{meshBytes} " +
                $"atlas_count:{atlasCount} atlas_bytes:{atlasBytes} total_bytes:{meshBytes + atlasBytes}");
        }

        private static long EstimateLabelMeshBytes(LabelSubMesh subMesh)
        {
            long vertexBytes = (long)subMesh.Mesh.vertexCount * (12 + 8 + 8 + 4); // position, uv0, uv2, color32
            long indexBytes = subMesh.Mesh.GetIndexCount(0) * 2; // TMP glyph meshes use 16-bit indices
            long sdfScaleBytes = (subMesh.BakedUv2?.Length ?? 0) * 8L * 2; // BakedUv2 + ScaledUv2, Vector2 each
            return vertexBytes + indexBytes + sdfScaleBytes;
        }

        private static long EstimateFontAtlasBytes(TMP_FontAsset font, ref int atlasCount)
        {
            if (font == null || font.atlasTextures == null)
            {
                return 0;
            }
            long bytes = 0;
            int count = math.min(font.atlasTextureCount, font.atlasTextures.Length);
            for (int i = 0; i < count; i++)
            {
                Texture2D atlas = font.atlasTextures[i];
                if (atlas == null)
                {
                    continue;
                }
                atlasCount++;
                bytes += (long)atlas.width * atlas.height * EstimateBytesPerPixel(atlas.format);
            }
            return bytes;
        }

        private static int EstimateBytesPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    return 1;
                case TextureFormat.RG16:
                    return 2;
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                    return 4;
                default:
                    return 4; // unknown format - assume the worst case rather than under-report
            }
        }

        private void DestroyLabelEntry(Entity group)
        {
            if (m_LabelEntries.TryGetValue(group, out LabelEntry entry))
            {
                foreach (LabelSubMesh subMesh in entry.SubMeshes)
                {
                    DestroyLabelSubMesh(subMesh);
                }
                if (entry.Object != null)
                {
                    Object.Destroy(entry.Object);
                }
                m_LabelEntries.Remove(group);
            }
        }

        private void DestroyLabelEntries()
        {
            foreach (LabelEntry entry in m_LabelEntries.Values)
            {
                foreach (LabelSubMesh subMesh in entry.SubMeshes)
                {
                    DestroyLabelSubMesh(subMesh);
                }
                if (entry.Object != null)
                {
                    Object.Destroy(entry.Object);
                }
            }
            m_LabelEntries.Clear();
        }

        private void DestroyLabelRoot()
        {
            DestroyLabelEntries();
            if (m_LabelRoot != null)
            {
                Object.Destroy(m_LabelRoot);
                m_LabelRoot = null;
            }
            if (m_LabelBakerObject != null)
            {
                Object.Destroy(m_LabelBakerObject);
                m_LabelBakerObject = null;
                m_LabelBaker = null;
            }
            if (m_LabelMaterial != null)
            {
                Object.Destroy(m_LabelMaterial);
                m_LabelMaterial = null;
            }
        }
    }
}
