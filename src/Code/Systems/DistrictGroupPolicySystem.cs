using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Common;
using Game.Policies;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    // District policies are ordinary policy prefabs, so one prefab query finds every one of them, including mods.
    //       
    // Policies belongs to the district, not to the group.
    //
    // A group-wide change just applies the policy state to all of the member districts.
    public partial class DistrictGroupPolicySystem : GameSystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        private PoliciesUISystem m_PoliciesUISystem;

        // Every policy prefab a district can carry, which is how the game itself tells district scope apart.
        private EntityQuery m_PolicyPrefabQuery;
        private EntityQuery m_PolicyUnlockedQuery;
        private EntityQuery m_UpdatedDistrictPolicyQuery;

        // The listable policies in display order, rebuilt only when the set or its unlock state moved.
        private readonly List<DistrictPolicy> m_Policies = new List<DistrictPolicy>();
        private bool m_PoliciesDirty = true;

        // The PolicyData order version the list was last built from; the dirty flag covers the first build.
        private int m_SeenPolicyPrefabVersion;

        // Identifies the answer every policy read currently gives, changing whenever that answer could have.
        public int Version { get; private set; }

        // The district policies the panel can list, in the order the game's own policy lists use.
        public IReadOnlyList<DistrictPolicy> Policies => m_Policies;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_PoliciesUISystem = World.GetOrCreateSystemManaged<PoliciesUISystem>();

            m_PolicyPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<PolicyData>() },
                Any = new[]
                {
                    ComponentType.ReadOnly<DistrictOptionData>(),
                    ComponentType.ReadOnly<DistrictModifierData>(),
                },
            });
            m_PolicyUnlockedQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
            m_UpdatedDistrictPolicyQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadOnly<Policy>(),
                ComponentType.ReadOnly<Updated>());

            GameManager.instance.localizationManager.onActiveDictionaryChanged += OnActiveDictionaryChanged;
        }

        protected override void OnDestroy()
        {
            GameManager.instance.localizationManager.onActiveDictionaryChanged -= OnActiveDictionaryChanged;
            base.OnDestroy();
        }

        // Prefabs and their unlock state both belong to the city that just loaded, so the list starts over.
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_PoliciesDirty = true;
            Version++;
        }

        // The list is sorted by each policy's display name, so a new active language reorders it.
        private void OnActiveDictionaryChanged()
        {
            m_PoliciesDirty = true;
        }

        protected override void OnUpdate()
        {
            bool policyUnlocked = PrefabUtils.HasUnlockedPrefab<PolicyData>(EntityManager, m_PolicyUnlockedQuery);
            bool districtPolicyChanged = !m_UpdatedDistrictPolicyQuery.IsEmptyIgnoreFilter;
            /*
                A policy prefab can be registered at any point while a city loads, a mod's own
                included, and registering one raises no event of its own.

                ECS keeps a per-component order version that moves whenever an archetype carrying that component gains or loses an entity,
                and PolicyData only ever sits on a prefab.
            */
            bool prefabsChanged = EntityManager.GetComponentOrderVersion<PolicyData>() != m_SeenPolicyPrefabVersion;
            bool listStale = m_PoliciesDirty || policyUnlocked || prefabsChanged;

            if (listStale)
            {
                m_PoliciesDirty = false;
                RebuildPolicies();
            }

            if (listStale || districtPolicyChanged)
            {
                Version++;
            }
        }

        // Whether a district has the policy switched on, and the value it carries when it has one.
        public DistrictPolicyState GetDistrictState(Entity district, Entity policy, float defaultValue)
        {
            if (!EntityManager.TryGetBuffer(district, isReadOnly: true, out DynamicBuffer<Policy> policies))
            {
                return new DistrictPolicyState { m_Active = false, m_Value = defaultValue };
            }

            return GetDistrictState(policies, policy, defaultValue);
        }

        // The same read against a district's policy buffer the caller is already holding.
        public static DistrictPolicyState GetDistrictState(DynamicBuffer<Policy> policies, Entity policy, float defaultValue)
        {
            DistrictPolicyState state = new DistrictPolicyState { m_Active = false, m_Value = defaultValue };

            // A district that has never been given a policy carries no buffer to read, which reads the same as nothing set.
            if (!policies.IsCreated)
            {
                return state;
            }

            foreach (Policy entry in policies)
            {
                if (entry.m_Policy != policy)
                {
                    continue;
                }

                state.m_Active = (entry.m_Flags & PolicyFlags.Active) != 0;
                state.m_Value = entry.m_Adjustment;
                break;
            }

            return state;
        }

        // The value a district falls back to when it has never carried the policy.
        public float GetDefaultValue(Entity policy)
        {
            return EntityManager.TryGetComponent(policy, out PolicySliderData slider) ? slider.m_Default : 0f;
        }

        // Switches the policy on or off across the group, leaving each district's own value alone.
        public void SetGroupPolicyActive(Entity group, Entity policy, bool active)
        {
            if (!EntityManager.TryGetBuffer(group, isReadOnly: true, out DynamicBuffer<DistrictGroupMember> members))
            {
                Mod.log.Warn($"Cannot set group policy, group has no members buffer; group:{group} policy:{policy}");
                return;
            }

            float defaultValue = GetDefaultValue(policy);
            foreach (DistrictGroupMember member in members)
            {
                DistrictPolicyState state = GetDistrictState(member.m_District, policy, defaultValue);
                m_PoliciesUISystem.SetPolicy(member.m_District, policy, active, state.m_Value);
            }

            Mod.log.Info($"Group policy toggled; group:{group} policy:{policy} active:{active} districts:{members.Length}");
        }

        // Gives the policy one shared value across every district of the group already carrying it.
        public void SetGroupPolicyValue(Entity group, Entity policy, float value)
        {
            if (!EntityManager.TryGetBuffer(group, isReadOnly: true, out DynamicBuffer<DistrictGroupMember> members))
            {
                Mod.log.Warn($"Cannot set group policy value, group has no members buffer; group:{group} policy:{policy}");
                return;
            }

            float defaultValue = GetDefaultValue(policy);
            int applied = 0;
            foreach (DistrictGroupMember member in members)
            {
                DistrictPolicyState state = GetDistrictState(member.m_District, policy, defaultValue);
                if (!state.m_Active)
                {
                    continue;
                }

                m_PoliciesUISystem.SetPolicy(member.m_District, policy, active: true, value);
                applied++;
            }

            Mod.log.Info($"Group policy value set; group:{group} policy:{policy} value:{value} districts:{applied}");
        }

        // Switches the policy on or off for one district, leaving the value it carries alone.
        public void SetDistrictPolicyActive(Entity district, Entity policy, bool active)
        {
            DistrictPolicyState state = GetDistrictState(district, policy, GetDefaultValue(policy));
            m_PoliciesUISystem.SetPolicy(district, policy, active, state.m_Value);
            Mod.log.Info($"District policy toggled; district:{district} policy:{policy} active:{active}");
        }

        // Gives one district's copy of the policy a new value.
        public void SetDistrictPolicyValue(Entity district, Entity policy, float value)
        {
            m_PoliciesUISystem.SetPolicy(district, policy, active: true, value);
            Mod.log.Info($"District policy value set; district:{district} policy:{policy} value:{value}");
        }

        private void RebuildPolicies()
        {
            m_Policies.Clear();
            m_SeenPolicyPrefabVersion = EntityManager.GetComponentOrderVersion<PolicyData>();

            using NativeArray<Entity> policies = m_PolicyPrefabQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity policy in policies)
            {
                if (TryDescribePolicy(policy, out DistrictPolicy described))
                {
                    m_Policies.Add(described);
                }
            }

            m_Policies.Sort();
            Mod.log.Info($"Rebuilt district policy list; listed:{m_Policies.Count} found:{policies.Length}");
        }

        private bool TryDescribePolicy(Entity policy, out DistrictPolicy described)
        {
            described = default;

            if (!m_PrefabSystem.TryGetPrefab(policy, out PolicyPrefab prefab))
            {
                Mod.log.Warn($"Skipping district policy with no prefab; policy:{policy}");
                return false;
            }

            if (prefab.m_Visibility == PolicyVisibility.HideFromPolicyList)
            {
                return false;
            }

            // A policy the city has not unlocked yet is left out, because there is nothing a group can do with it
            if (EntityManager.HasEnabledComponent<Locked>(policy))
            {
                return false;
            }

            bool hasSlider = EntityManager.TryGetComponent(policy, out PolicySliderData slider);
            int priority = EntityManager.TryGetComponent(policy, out UIObjectData uiObject) ? uiObject.m_Priority : 0;

            described = new DistrictPolicy(
                policy,
                prefab.name,
                LocalizedPolicyName(prefab.name),
                ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon,
                priority,
                hasSlider,
                slider);
            return true;
        }

        private static string LocalizedPolicyName(string id)
        {
            /*
                Policy display names never go through NameSystem - the game reads them straight out of the
                active locale dictionary - so the sort key has to be looked up the same way to match.
            */
            return GameManager.instance.localizationManager.activeDictionary
                .TryGetValue($"Policy.TITLE[{id}]", out string name)
                ? name
                : id;
        }
    }
}
