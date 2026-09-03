using System.Collections.Generic;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DistrictGroups
{
    // draws each group's member-district boundaries and regions using its own intrinsic color
    public partial class DistrictGroupOverlaySystem : GameSystemBase
    {
        // Wall-clock (not game-speed-affected) cadence for OnUpdate timing
        // samples. NegativeInfinity forces the first OnUpdate after the
        // overlay becomes visible to always sample, even if the user
        // dismisses the overlay again before kSampleIntervalSeconds elapses.
        private const float kSampleIntervalSeconds = 4f;
        private float m_LastSampleTime = float.NegativeInfinity;

        private static readonly PropertyInfo kRequireAreasProperty =
            typeof(ToolBaseSystem).GetProperty(nameof(ToolBaseSystem.requireAreas));

        private OverlayRenderSystem m_OverlayRenderSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private GameScreenUISystem m_GameScreenUISystem;
        private DistrictGroupSystem m_GroupSystem;
        private bool m_Visible;
        public bool Visible => m_Visible;

        // Used to detect when the vanilla area tool (which edits a district's boundaries) closes,
        // so the fill mesh can be forced to rebuild
        private ToolSystem m_ToolSystem;
        private AreaToolSystem m_AreaToolSystem;
        private bool m_WasAreaToolActive;
        public bool IsAreaToolActive => m_WasAreaToolActive;

        // Any non-default tool can mutate area geometry, so the snapshot is recaptured whenever one closes.
        private bool m_WasNonDefaultToolActive;

        // One row per district, captured once per Snapshot rebuild
        private sealed class DistrictSnapshot
        {
            // raw group colors, group-iteration order
            public List<Color> Colors;
            // node positions with kOverlayHeightOffset added to Y
            public float3[] BorderPositions;
            // AABB over BorderPositions, for frustum culling
            public Bounds BorderBounds;
            // raw node positions (fill root transform supplies the lift)
            public Vector3[] FillVertices;
            // flattened vanilla Game.Areas.Triangle indices; empty => ear-clip fallback     
            public int[] TriangleIndices;
            // content hash of node positions (count + folded math.hash)    
            public int PolygonHash;
            // whether the district carried a Geometry component at capture            
            public bool HasGeometry;
            // Geometry.m_SurfaceArea (0 if no Geometry component)
            public float SurfaceArea;
            // Geometry.m_CenterPosition (zero if no Geometry component)
            public float3 GeometryCenter;      
        }

        // Area-weighted member centroid of each visible group with >=1 Geometry-carrying member.
        private sealed class GroupSnapshot
        {
            public float3 Center;
        }

        private readonly Dictionary<Entity, DistrictSnapshot> m_DistrictSnapshots = new Dictionary<Entity, DistrictSnapshot>();
        private readonly Dictionary<Entity, GroupSnapshot> m_GroupSnapshots = new Dictionary<Entity, GroupSnapshot>();

        // Scratch for the cached border path's culling.
        private readonly Plane[] m_BorderFrustumPlanes = new Plane[6];

        // Lighten() results keyed by raw group color
        private readonly Dictionary<Color, Color> m_LightenedColorCache = new Dictionary<Color, Color>();
        private int m_LightenedCacheSaturationPercent = -1;

        // Scratch for RebuildFillObjects' stale-key sweep.
        private readonly List<Entity> m_FillStaleDistrictsScratch = new List<Entity>();

        // What overlay-derived state needs rebuilding; bits are set by DetectChanges and
        // the explicit invalidation sites, cleared by each consumer after it rebuilds.
        [System.Flags]
        private enum OverlayDirtyFlags : byte
        {
            None = 0,
            Snapshot = 1 << 0,         // m_DistrictSnapshots + m_GroupSnapshots
            FillGeometry = 1 << 1,     // fill meshes (keyed diff against the snapshot)
            Labels = 1 << 2,           // label text/positions (m_LabelEntries)
            All = Snapshot | FillGeometry | Labels,
        }

        private OverlayDirtyFlags m_DirtyFlags = OverlayDirtyFlags.All; // All = never built

        // Last GroupCompositionVersion already folded into m_DirtyFlags; advanced only
        // inside DetectChanges, so drift during inactive frames is caught on reactivation.
        private int m_LastSeenCompositionVersion = -1;

        // Last DistrictGroupSystem.Version already folded into m_DirtyFlags; renames bump Version but
        // not GroupCompositionVersion, so labels (which show the current name) need this coarser gate
        // in addition to m_LastSeenCompositionVersion above.
        private int m_LastSeenVersion = -1;

        // Master on/off for the border+fill overlay. In-session only, like m_TypeFilter below -
        // resets to the default each time the game starts.
        private bool m_ShowOverlay = true;
        public bool ShowOverlay => m_ShowOverlay;

        private bool m_AreasVisible;
        public bool AreasVisible => m_AreasVisible;

        // Defaults to Generic (0).
        private int m_TypeFilter = (int)GroupServiceType.Generic;
        public int TypeFilter => m_TypeFilter;

        // Full-screen desaturation Volume shown alongside the border overlay
        private Volume m_DesaturationVolume;
        private ColorAdjustments m_ColorAdjustments;
        private bool m_DesaturationActive;

        // Last saturation percent actually written to m_ColorAdjustments; int.MinValue = never written
        private int m_AppliedDesaturationPercent = int.MinValue;

        private const float kFillVibrancy = 0.75f;
        private const float kMinFillSaturationPercent = 35f;

        // How many times the color cycle repeats across a multi-group district's fill.
        private const int kStripeRepeatCount = 4;

        // One entry per district with a fill mesh; keyed by district so a saturation-only change can
        // recolor an existing entry in place instead of destroying and recreating it.
        private struct FillEntry
        {
            public GameObject Object;
            public Mesh Mesh;
            public Texture2D Texture; // null for single-color districts (flat _UnlitColor tint, no texture)
            public MeshRenderer Renderer; // cached so recoloring doesn't need GetComponent
            public int PolygonHash; // DistrictSnapshot.PolygonHash the mesh was built from
            public List<Color> BuiltColors; // raw (pre-Lighten) group colors at build time
        }

        private GameObject m_FillRoot;
        private Material m_FillMaterial;
        private readonly Dictionary<Entity, FillEntry> m_FillEntries = new Dictionary<Entity, FillEntry>();

        // Last (clamped) saturation percent the fill meshes were actually built with
        // a Settings change has to be caught to trigger a rebuild
        private int m_FillBuiltSaturationPercent = -1;

        // Whether the fill material was last configured for transparency; a Settings change has to be
        // caught to reconfigure the material's blend state (set once at creation, otherwise unchanged).
        private bool m_FillBuiltTransparent;
        private bool m_FillActive;

        // Nothing in HDRP's own render path draws TMP's SDF shader at the AfterPostProcess queue
        // HDMaterial.SetRenderingPass routes the label material into (the native after-post-process
        // object pass only draws ForwardOnly-tagged passes, which TMP's shader lacks) - this mod's own
        // Custom Pass at the AfterPostProcess injection point is what actually issues the labels' draw
        // call. DrawRenderersCustomPass finds renderers to draw by LayerMask (+ shader-tag filtering
        // broad enough to include TMP's SDF shader - confirmed via ildasm dump of
        // Unity.RenderPipelines.HighDefinition.Runtime.dll: forwardShaderTags includes
        // HDShaderPassNames.s_SRPDefaultUnlitName and s_EmptyName, not just HDRP-authored passes).
        // Draw order against the vanilla border and the opaque fill is decided by CustomPassVolume
        // priority against the game's own "Outlines Pass" volume at the same injection point - see
        // EnsureOverlayCompositePass in DistrictGroupOverlaySystem.CompositePass.cs.
        //
        // Deliberately label-only, NOT also used for the fill mesh: HDRP already has its own native,
        // always-on mechanism that draws AfterPostProcessOpaque/Transparent-queued renderers every frame
        // - that's what was already drawing the fill correctly (just without any depth test) before any
        // of this custom-pass machinery existed. Registering the fill's renderer on a
        // DrawRenderersCustomPass too drew it a SECOND time on top of that - confirmed by trying it: the
        // fill broke (progressively, worse after camera movement - consistent with two independently
        // depth-tested draws of the same geometry) and transparency stopped working (consistent with two
        // stacked alpha-blends of the same quad converging toward opaque). TMP's shader apparently isn't
        // picked up by whatever narrower shader-tag filter that same native mechanism uses (it never drew
        // labels at all, hence labels needing this pass in the first place), so no such conflict exists
        // for labels specifically.
        private const int kOverlayLabelLayer = 30;

        private GameObject m_OverlayPassVolumeObject;
        private CustomPassVolume m_OverlayPassVolume;
        private DrawRenderersCustomPass m_LabelCustomPass;

        // Small additional lift above the border/fill so the label never shares a height with them; tweak freely.
        private const float kLabelHeightOffset = 40f;

        // Matches vanilla district-name glyph generation (fontSize = 200f) - the baked mesh is scaled
        // down per-frame via AreaUtils.CalculateLabelScale instead of being generated at a smaller size.
        private const float kLabelFontSize = 400f;

        // One child GameObject per distinct font/atlas TMP actually used to render a name - a name
        // mixing e.g. Latin and CJK glyphs needs one of these per script, since each font asset owns
        // its own atlas texture and a single mesh/material can only sample one. Mirrors how TMP's own
        // TMP_SubMesh children (and OverlayRenderSystem.GetTextRenderItems' per-meshInfo items) work.
        private sealed class LabelSubMesh
        {
            public GameObject Object;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Mesh Mesh; // owned snapshot, not shared with the baker/its TMP_SubMesh children
            public Material Material; // null for submesh 0, which just reuses the shared m_LabelMaterial

            // Pristine uv2 captured at bake time (implicitly at scale 1, since the baker GameObject
            // itself never scales) and a same-length scratch buffer reused across rescales - see
            // ApplyLabelSdfScale for why these exist and why BakedUv2 must never be mutated in place.
            public Vector2[] BakedUv2;
            public Vector2[] ScaledUv2;
        }

        // One entry per visible group's name label, keyed by the group entity.
        private sealed class LabelEntry
        {
            public GameObject Object;
            public Transform Transform; // cached so the per-frame path never pays a .transform lookup
            public List<LabelSubMesh> SubMeshes;
            public string Name;

            // The label's world position (group center plus the root's height offset), tracked here
            // so DrawGroupLabels' distance math never reads Transform.position back from native code.
            public float3 Position;

            // The scale DrawGroupLabels last actually applied (transform + uv2 SDF recalibration,
            // always together so they can't drift apart). 0 means "uv2 is at baked, unscaled
            // calibration" - guaranteed below any real scale (CalculateLabelScale floors at 0.01),
            // so a fresh or re-baked entry always gets scaled on its next draw.
            public float LastAppliedScale;
        }

        // Camera pose DrawGroupLabels last applied to the label transforms.
        private float3 m_LastLabelCameraPosition;
        private Quaternion m_LastLabelCameraRotation;

        private bool m_LabelTransformsDirty = true;

        // Below this relative scale change, neither the transform scale nor the uv2 rewrite is worth re-applying
        private const float kLabelScaleRescaleEpsilon = 0.002f;

        // Scratch collections reused across RebuildLabelEntries calls.
        private readonly HashSet<Entity> m_LabelSeenGroupsScratch = new HashSet<Entity>();
        private readonly List<Entity> m_LabelStaleGroupsScratch = new List<Entity>();

        // Whether the composite pass's undercut-everyone priority scan has run since the last city load
        private bool m_CompositePassPriorityRefreshed;

        // Supplies the current camera position each frame for AreaUtils.CalculateLabelScale, the same
        // public helper vanilla district-name rendering uses to stay legible at any zoom level.
        private CameraUpdateSystem m_CameraUpdateSystem;

        // Resolves OverlayConfigurationPrefab (the same singleton settings prefab
        // OverlayRenderSystem itself reads) so label font creation can match its m_FontInfos exactly.
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_OverlayConfigQuery;

        private GameObject m_LabelRoot;
        private readonly Dictionary<Entity, LabelEntry> m_LabelEntries = new Dictionary<Entity, LabelEntry>();
        private bool m_LabelsActive;

        // Hidden, never-rendered TextMeshPro used purely to bake glyph meshes (layout/kerning/wrapping)
        // for every label - the actual per-group GameObjects render a baked copy of its mesh through
        // m_LabelMaterial (cloned from the baker font's own TMP SDF material) instead of through the
        // baker's live TMP_Text component.
        private GameObject m_LabelBakerObject;
        private TextMeshPro m_LabelBaker;
        private Material m_LabelMaterial;

        // Logged once (not per-label) the first time the label font/material is resolved.
        private bool m_LoggedLabelFontDiagnostics;

        private bool IsOverlayActive =>
            m_Visible && m_ShowOverlay && !m_GameScreenUISystem.isMenuActive &&
            m_GameScreenUISystem.activeScreen != GameScreenUISystem.GameScreen.FreeCamera;

        // Keeps the border/fill a hair above the terrain it's drawn on.
        private const float kOverlayHeightOffset = 0.3f;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_GameScreenUISystem = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_OverlayConfigQuery = GetEntityQuery(ComponentType.ReadOnly<OverlayConfigurationData>());
            ApplyAreasVisibility();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_AreaToolSystem = World.GetOrCreateSystemManaged<AreaToolSystem>();
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Combine(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
        }

        protected override void OnDestroy()
        {
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Remove(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
            DestroyDesaturationVolume();
            DestroyFillRoot();
            DestroyLabelRoot();
            DestroyOverlayCompositePass();
            base.OnDestroy();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // We need to reset the overlay entirely in between cities because otherwise we'd display the last city's
            // cached overlay
            m_DirtyFlags = OverlayDirtyFlags.All;

            // Snapshot rows are keyed by the previous city's entities - drop them outright.
            m_DistrictSnapshots.Clear();
            m_GroupSnapshots.Clear();

            // Label entries are keyed by the previous city's group entities - drop them outright
            // rather than letting their GameObjects linger (hidden) until the first rebuild sweep.
            DestroyLabelEntries();

            // the system doesn't get refreshed in between save-game loads, so we need to make sure we're in a clean enough state
            if (m_Visible)
            {
                m_Visible = false;
                ApplyAreasVisibility();
                Mod.log.Info("Forcing group overlay closed on load");
            }
            if (m_DesaturationActive)
            {
                DisableDesaturation();
            }
            if (m_FillActive)
            {
                DisableFill();
            }
            if (m_LabelsActive)
            {
                DisableLabels();
            }

            // Front-load the label subsystem's one-time costs here instead of paying a mid-gameplay hitch
            // on the first overlay open. Gated so saves that can never show labels
            // (no groups, or the feature disabled) never pay the atlas memory for it.
            bool labelsEnabled = Mod.Settings?.OverlayEnableGroupLabels ?? Setting.kDefaultOverlayEnableGroupLabels;
            if (mode == GameMode.Game && labelsEnabled && m_GroupSystem.HasGroups)
            {
                PrewarmLabelAssets();
            }

            // Cleared after the prewarm, which may have just created the composite pass, so the
            // first label activation this load re-runs the priority undercut and still sorts after
            // any AfterPostProcess volume registered between now and the overlay actually opening.
            m_CompositePassPriorityRefreshed = false;
        }

        protected override void OnUpdate()
        {
            // we don't want to flood the logs with rendering breadcrumbs, so we sample instead
            bool debugLogging = Mod.Settings?.EnableDebugLogging ?? false;
            bool shouldSample = debugLogging &&
                UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;
            if (shouldSample)
            {
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
            }

            bool active = IsOverlayActive && m_GroupSystem.HasGroups;
            if (active)
            {
                DetectChanges();
                EnsureOverlaySnapshot();
                UpdateDesaturation();
                UpdateFill(shouldSample);
                DrawGroupOverlays(shouldSample);
                UpdateGroupLabels(shouldSample);
            }
            else
            {
                if (m_DesaturationActive)
                {
                    DisableDesaturation();
                }
                if (m_FillActive)
                {
                    DisableFill();
                }
                if (m_LabelsActive)
                {
                    DisableLabels();
                }
            }
        }

        // Folds the upstream composition counter into dirty bits, consumers then only check/clear their own bit.
        private void DetectChanges()
        {
            int compositionVersion = m_GroupSystem.GroupCompositionVersion;
            if (compositionVersion != m_LastSeenCompositionVersion)
            {
                m_LastSeenCompositionVersion = compositionVersion;
                m_DirtyFlags |= OverlayDirtyFlags.All;
            }

            // Coarser than the composition version above - catches renames, which labels need to
            // reflect but which don't otherwise touch colors/fill/geometry.
            int version = m_GroupSystem.Version;
            if (version != m_LastSeenVersion)
            {
                m_LastSeenVersion = version;
                m_DirtyFlags |= OverlayDirtyFlags.Labels;
            }
        }

        // The UI panel drives visibility
        public void SetVisible(bool visible)
        {
            if (m_Visible == visible)
            {
                return;
            }
            bool wasActive = IsOverlayActive;
            m_Visible = visible;
            OnOverlayActiveChanged(wasActive);
            Mod.log.Info($"Group overlay toggled; visible:{m_Visible}");
            ApplyAreasVisibility();
        }

        // The panel's own "Show group overlay" checkbox.
        public void SetShowOverlay(bool show)
        {
            if (m_ShowOverlay == show)
            {
                return;
            }
            bool wasActive = IsOverlayActive;
            m_ShowOverlay = show;
            OnOverlayActiveChanged(wasActive);
            Mod.log.Info($"Show group overlay toggled; show:{m_ShowOverlay}");
        }

        private void OnOverlayActiveChanged(bool wasActive)
        {
            bool isActive = IsOverlayActive;
            if (isActive == wasActive)
            {
                return;
            }
            if (isActive)
            {
                m_LastSampleTime = float.NegativeInfinity;
            }
        }

        public void SetAreasVisible(bool visible)
        {
            if (m_AreasVisible == visible)
            {
                return;
            }
            m_AreasVisible = visible;
            Mod.log.Info($"District areas checkbox toggled; visible:{m_AreasVisible}");
            ApplyAreasVisibility();
        }

        private void ApplyAreasVisibility()
        {
            bool shouldShow = m_Visible && m_AreasVisible;
            kRequireAreasProperty?.SetValue(m_DefaultToolSystem,
                shouldShow ? AreaTypeMask.Districts : AreaTypeMask.None);
        }

        public void SetTypeFilter(int type)
        {
            if (m_TypeFilter == type)
            {
                Mod.log.Info($"Not changing overlay filter, same type; type:{type}");
                return;
            }

            Mod.log.Info($"Setting overlay filter; type:{type}");
            m_TypeFilter = type;

            // signal that the color cache and fill mesh both need to be rebuilt
            m_DirtyFlags |= OverlayDirtyFlags.All;
        }

        // Rebuilds the shared snapshot every overlay subsystem consumes
        private void EnsureOverlaySnapshot()
        {
            if ((m_DirtyFlags & OverlayDirtyFlags.Snapshot) == 0)
            {
                return;
            }

            bool debugLogging = Mod.Settings?.EnableDebugLogging ?? false;
            System.Diagnostics.Stopwatch stopwatch = debugLogging ? System.Diagnostics.Stopwatch.StartNew() : null;

            m_DistrictSnapshots.Clear();
            m_GroupSnapshots.Clear();

            using NativeArray<Entity> groups = m_GroupSystem.GetGroups(Allocator.Temp);
            for (int i = 0; i < groups.Length; i++)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                if (m_TypeFilter >= 0 && (int)data.m_Type != m_TypeFilter)
                {
                    continue;
                }

                float3 weightedSum = float3.zero;
                float totalArea = 0f;

                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(groups[i], isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    Entity district = member.m_District;
                    if (!EntityManager.Exists(district) || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                    {
                        continue;
                    }
                    if (!m_DistrictSnapshots.TryGetValue(district, out DistrictSnapshot snapshot))
                    {
                        snapshot = CaptureDistrictSnapshot(district);
                        m_DistrictSnapshots[district] = snapshot;
                    }
                    snapshot.Colors.Add(data.m_Color);

                    // Area-weighted centroid; a Geometry-less district still gets colors but contributes 0.
                    if (snapshot.HasGeometry)
                    {
                        float area = math.max(snapshot.SurfaceArea, 0.01f); // guards a degenerate/zero-area district
                        weightedSum += snapshot.GeometryCenter * area;
                        totalArea += area;
                    }
                }

                if (totalArea > 0f)
                {
                    m_GroupSnapshots[groups[i]] = new GroupSnapshot { Center = weightedSum / totalArea };
                }
            }

            m_DirtyFlags &= ~OverlayDirtyFlags.Snapshot;

            if (debugLogging)
            {
                stopwatch.Stop();
                Mod.log.Debug($"Overlay snapshot rebuilt; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} " +
                    $"group_count:{groups.Length} district_count:{m_DistrictSnapshots.Count} label_group_count:{m_GroupSnapshots.Count}");
            }
        }

        // Copies everything the overlay needs from one district's ECS data
        private DistrictSnapshot CaptureDistrictSnapshot(Entity district)
        {
            DistrictSnapshot snapshot = new DistrictSnapshot { Colors = new List<Color>() };

            DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
            int nodeCount = nodes.Length;
            snapshot.BorderPositions = new float3[nodeCount];
            snapshot.FillVertices = new Vector3[nodeCount];

            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            uint hash = (uint)nodeCount;
            for (int i = 0; i < nodeCount; i++)
            {
                float3 position = nodes[i].m_Position;
                float3 lifted = position + new float3(0f, kOverlayHeightOffset, 0f);
                snapshot.BorderPositions[i] = lifted;
                snapshot.FillVertices[i] = new Vector3(position.x, position.y, position.z);
                min = math.min(min, lifted);
                max = math.max(max, lifted);
                hash = math.hash(new uint2(hash, math.hash(position)));
            }
            snapshot.PolygonHash = (int)hash;
            if (nodeCount > 0)
            {
                Bounds bounds = new Bounds();
                bounds.SetMinMax(new Vector3(min.x, min.y, min.z), new Vector3(max.x, max.y, max.z));
                snapshot.BorderBounds = bounds;
            }

            snapshot.TriangleIndices = ReadVanillaTriangles(district, snapshot.FillVertices);

            if (EntityManager.HasComponent<Geometry>(district))
            {
                Geometry geometry = EntityManager.GetComponentData<Geometry>(district);
                snapshot.HasGeometry = true;
                snapshot.SurfaceArea = geometry.m_SurfaceArea;
                snapshot.GeometryCenter = geometry.m_CenterPosition;
            }

            return snapshot;
        }

        // Flattens the district's Game.Areas.Triangle buffer into index triples matching the fill's own winding convention.
        private int[] ReadVanillaTriangles(Entity district, Vector3[] vertices)
        {
            if (!EntityManager.HasBuffer<Game.Areas.Triangle>(district))
            {
                return System.Array.Empty<int>();
            }

            DynamicBuffer<Game.Areas.Triangle> triangles = EntityManager.GetBuffer<Game.Areas.Triangle>(district, isReadOnly: true);
            if (triangles.Length == 0)
            {
                return System.Array.Empty<int>();
            }

            int[] indices = new int[triangles.Length * 3];
            for (int i = 0; i < triangles.Length; i++)
            {
                int3 triangle = triangles[i].m_Indices;
                if (triangle.x < 0 || triangle.x >= vertices.Length
                    || triangle.y < 0 || triangle.y >= vertices.Length
                    || triangle.z < 0 || triangle.z >= vertices.Length)
                {
                    return System.Array.Empty<int>();
                }
                indices[i * 3] = triangle.x;
                indices[i * 3 + 1] = triangle.y;
                indices[i * 3 + 2] = triangle.z;
            }

            // The game triangulates with uniform winding, but it may be the reverse of the fill's
            // convention (clockwise in the XZ plane, matching Triangulate/IsConvex). Check the
            // first non-degenerate triangle and swap two indices per triangle if reversed.
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = vertices[indices[i]];
                Vector3 b = vertices[indices[i + 1]];
                Vector3 c = vertices[indices[i + 2]];
                float cross = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                if (math.abs(cross) < 1e-3f)
                {
                    continue;
                }
                if (cross > 0f)
                {
                    for (int j = 0; j < indices.Length; j += 3)
                    {
                        int swap = indices[j + 1];
                        indices[j + 1] = indices[j + 2];
                        indices[j + 2] = swap;
                    }
                }
                break;
            }

            return indices;
        }

        // Tears down every runtime asset the overlay owns and forces the panel closed.
        public void RemoveAllData()
        {
            Mod.log.Info("Removing all group overlay state from the world");

            if (m_Visible)
            {
                m_Visible = false;
                ApplyAreasVisibility();
            }

            if (m_DesaturationVolume != null)
            {
                DestroyDesaturationVolume();
            }
            m_DesaturationActive = false;

            DestroyFillRoot();
            m_FillActive = false;

            DestroyLabelRoot();
            m_LabelsActive = false;

            DestroyOverlayCompositePass();

            m_DistrictSnapshots.Clear();
            m_GroupSnapshots.Clear();
            m_DirtyFlags = OverlayDirtyFlags.All;

            Mod.log.Info("Finished removing all group overlay state from the world");
        }

        // Handle that the district area tool was closed
        private void OnActiveToolChanged(ToolBaseSystem tool)
        {
            bool isAreaToolActive = tool == m_AreaToolSystem;
            if (m_WasAreaToolActive && !isAreaToolActive)
            {
                Mod.log.Info("Area tool closed, forcing fill and label rebuild");
                m_DirtyFlags |= OverlayDirtyFlags.FillGeometry | OverlayDirtyFlags.Labels;
            }
            m_WasAreaToolActive = isAreaToolActive;

            // Any transition away from a non-default tool recaptures the whole snapshot: area-tool
            // edits change polygons, terrain tools re-sample area node heights, and the cached
            // border would otherwise keep drawing the stale capture. User-paced, and the fill
            // diff no-ops on rows whose PolygonHash is unchanged.
            if (m_WasNonDefaultToolActive)
            {
                m_DirtyFlags |= OverlayDirtyFlags.All;
            }
            m_WasNonDefaultToolActive = tool != null && tool != m_DefaultToolSystem;
        }
    }
}
