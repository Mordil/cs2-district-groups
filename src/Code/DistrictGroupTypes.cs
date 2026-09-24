using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DistrictGroups
{
    // Shared JSON-writing helpers for the mod's ECS types
    internal static class EntityJson
    {
        // Matches the JS-side Entity shape ({index, version}) explicitly, so the
        // wire format never depends on which writer extensions exist.
        internal static void WriteEntity(IJsonWriter writer, Entity entity)
        {
            writer.TypeBegin("Unity.Entities.Entity");
            writer.PropertyName("index");
            writer.Write(entity.Index);
            writer.PropertyName("version");
            writer.Write(entity.Version);
            writer.TypeEnd();
        }
    }

    public enum GroupServiceType : byte
    {
        Generic = 0,
        Police = 1,
        Fire = 2,
        Healthcare = 3,
        Deathcare = 4,
        Garbage = 5,
        EducationElementary = 6,
        EducationHighSchool = 7,
        EducationCollege = 8,
        EducationUniversity = 9,
        Post = 10,
    }

    // A district policy as the group panel lists it, with the prefab display data its row reads.
    public readonly struct DistrictPolicy : IComparable<DistrictPolicy>
    {
        // The policy prefab entity, which is what a policy write names as its subject.
        public readonly Entity m_Policy;
        // The prefab name, which is also the hash the game keys Policy.TITLE and Policy.DESCRIPTION on.
        public readonly string m_Id;
        // Resolved icon uri, already fallen back to the game's placeholder when the prefab carries none.
        public readonly string m_Icon;
        // Whether the policy carries an adjustable value on top of being on or off.
        public readonly bool m_HasSlider;
        // The value's range, default and step; meaningless unless m_HasSlider.
        public readonly PolicySliderData m_Slider;

        private readonly string m_LocalizedName;
        private readonly int m_Priority;

        // The value a district falls back to when it has never carried the policy.
        public float DefaultValue => m_HasSlider ? m_Slider.m_Default : 0f;

        public DistrictPolicy(
            Entity policy,
            string id,
            string localizedName,
            string icon,
            int priority,
            bool hasSlider,
            PolicySliderData slider)
        {
            m_Policy = policy;
            m_Id = id;
            m_LocalizedName = localizedName;
            m_Icon = icon;
            m_Priority = priority;
            m_HasSlider = hasSlider;
            m_Slider = slider;
        }

        /*
            The keys the game's own policy lists sort by, minus the milestone one: a policy still
            behind a milestone never reaches this list, so every entry shares the same milestone of
            none, and what is left is the order players already know from the district info panel.
        */
        public int CompareTo(DistrictPolicy other)
        {
            int byPriority = m_Priority.CompareTo(other.m_Priority);
            if (byPriority != 0)
            {
                return byPriority;
            }

            return string.Compare(m_LocalizedName, other.m_LocalizedName, StringComparison.Ordinal);
        }
    }

    // How one district has a policy set: whether it is switched on, and the value it carries.
    public struct DistrictPolicyState
    {
        public bool m_Active;
        public float m_Value;
    }

    // Resident totals for one district, or for a whole group once its districts are added together.
    // 
    // Kept as sums with their own divisors rather than as finished averages,
    // so adding two districts together yields the population-weighted average and not the average of averages.
    public struct DistrictStats
    {
        // Everyone living in the district's residential buildings.
        public int m_Population;
        // Summed Citizen.Happiness, divided by m_LivingResidentCount for the average.
        public int m_HappinessSum;
        // Living residents only - the dead still occupy a household but have no happiness to average.
        public int m_LivingResidentCount;
        // Summed household wealth, divided by m_HouseholdCount for the average.
        public long m_WealthSum;
        // Summed household income, divided by m_HouseholdCount for the average.
        public long m_IncomeSum;
        // Resident households, excluding the tourists and commuters vanilla leaves out of its wealth average.
        public int m_HouseholdCount;
        // Living residents of settled households, which is the set the settled averages are drawn from.
        public int m_SettledResidentCount;
        // Summed Game.Buildings.CrimeProducer.m_Crime, divided by m_CrimeProducerCount for the average.
        public float m_CrimeSum;
        // Crime-producing buildings the sum was drawn from.
        public int m_CrimeProducerCount;
        // Summed fire-hazard risk factor, divided by m_FireRiskBuildingCount for the average.
        public float m_FireRiskSum;
        // Flammable buildings the sum was drawn from.
        public int m_FireRiskBuildingCount;
        // Summed Citizen.m_Health over settled residents, divided by m_SettledResidentCount for the average.
        public int m_HealthSum;
        // Residents of this district currently occupying a hospital patient slot, wherever in the city that hospital is.
        public int m_ActivePatientCount;

        // Folds another district's totals into these.
        public void Add(DistrictStats other)
        {
            m_Population += other.m_Population;
            m_HappinessSum += other.m_HappinessSum;
            m_LivingResidentCount += other.m_LivingResidentCount;
            m_WealthSum += other.m_WealthSum;
            m_IncomeSum += other.m_IncomeSum;
            m_HouseholdCount += other.m_HouseholdCount;
            m_SettledResidentCount += other.m_SettledResidentCount;
            m_CrimeSum += other.m_CrimeSum;
            m_CrimeProducerCount += other.m_CrimeProducerCount;
            m_FireRiskSum += other.m_FireRiskSum;
            m_FireRiskBuildingCount += other.m_FireRiskBuildingCount;
            m_HealthSum += other.m_HealthSum;
            m_ActivePatientCount += other.m_ActivePatientCount;
        }
    }


    // Turns a district's or a group's swept sums into the figures its panels read out.
    public readonly struct DistrictStatsReader
    {
        private readonly bool m_HasWealthBands;
        private readonly CitizenHappinessParameterData m_WealthBands;
        private readonly float m_MaxCrimeAccumulation;

        // Captures the city-wide parameters one payload's figures are measured against.
        public DistrictStatsReader(
            bool hasWealthBands,
            CitizenHappinessParameterData wealthBands,
            float maxCrimeAccumulation)
        {
            m_HasWealthBands = hasWealthBands;
            m_WealthBands = wealthBands;
            m_MaxCrimeAccumulation = maxCrimeAccumulation;
        }

        // Which happiness band the average resident falls in, or kNoValue
        public int Happiness(DistrictStats stats) =>
            stats.m_LivingResidentCount == 0
                ? DistrictGroupsUISystem.kNoValue
                : (int)Game.Citizens.CitizenUtils.GetHappinessKey(
                    stats.m_HappinessSum / stats.m_LivingResidentCount);

        // Which wealth band the average household falls in, or kNoValue
        public int Wealth(DistrictStats stats)
        {
            if (stats.m_HouseholdCount == 0 || !m_HasWealthBands)
            {
                return DistrictGroupsUISystem.kNoValue;
            }
            int averageWealth = (int)(stats.m_WealthSum / stats.m_HouseholdCount);
            CitizenHappinessParameterData bands = m_WealthBands;
            return (int)Game.UI.InGame.CitizenUIUtils.GetHouseholdWealthKey(averageWealth, bands);
        }

        public int Income(DistrictStats stats) =>
            stats.m_HouseholdCount == 0 ? DistrictGroupsUISystem.kNoValue : (int)(stats.m_IncomeSum / stats.m_HouseholdCount);

        // Average crime accumulation as a whole percent of PoliceConfigurationData.m_MaxCrimeAccumulation, or kNoValue
        public int CrimeChance(DistrictStats stats)
        {
            if (stats.m_CrimeProducerCount == 0 || m_MaxCrimeAccumulation <= 0f)
            {
                return DistrictGroupsUISystem.kNoValue;
            }

            float averageCrime = stats.m_CrimeSum / stats.m_CrimeProducerCount;
            return (int)math.round(100f * math.saturate(averageCrime / m_MaxCrimeAccumulation));
        }

        // Average fire risk across the district's flammable buildings, on vanilla's own 0-100 fire-hazard scale, or kNoValue
        public int FireRisk(DistrictStats stats) =>
            stats.m_FireRiskBuildingCount == 0
                ? DistrictGroupsUISystem.kNoValue
                : (int)math.round(math.clamp(stats.m_FireRiskSum / stats.m_FireRiskBuildingCount, 0f, 100f));

        // Average health across the district's settled residents, on Citizen.m_Health's own 0-100 scale, or kNoValue
        public int Health(DistrictStats stats) =>
            stats.m_SettledResidentCount == 0
                ? DistrictGroupsUISystem.kNoValue
                : (int)math.round((float)stats.m_HealthSum / stats.m_SettledResidentCount);
    }

    // A named, typed set of base districts.
    public struct DistrictGroupData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public GroupServiceType m_Type;
        public FixedString64Bytes m_Name;
        public Color m_Color;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((byte)4);
            writer.Write((byte)m_Type);
            writer.Write(m_Name.ToString());
            writer.Write(m_Color.r);
            writer.Write(m_Color.g);
            writer.Write(m_Color.b);
            writer.Write(m_Color.a);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            reader.Read(out byte type);
            /*
                Versions before 4 stored GroupServiceType.Parks at byte 11 and GroupServiceType.Welfare
                at byte 12 (byte 11 from version 3 on, once Parks was removed); both types were dropped
                and folded into Generic, so a save from an older version needs either raw byte remapped
                to keep reading the type it already had.
            */
            if (version < 4 && (type == 11 || type == 12))
            {
                type = (byte)GroupServiceType.Generic;
            }
            m_Type = (GroupServiceType)type;
            reader.Read(out string name);
            m_Name = name;
            if (version >= 2)
            {
                reader.Read(out float r);
                reader.Read(out float g);
                reader.Read(out float b);
                reader.Read(out float a);
                m_Color = new Color(r, g, b, a);
            }
            else
            {
                m_Color = new Color(0f, 0f, 0f, -1f);
            }
        }
    }

    public struct DistrictGroupMember : IBufferElementData, ISerializable
    {
        public Entity m_District;

        public DistrictGroupMember(Entity district)
        {
            m_District = district;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_District);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_District);
        }
    }

    // On a service building: which group its ServiceDistrict buffer is managed by.
    // Enableable so toggling a building's assignment flips a bit instead of triggering an ECS archetype move.
    // Disabled == unassigned; queries over this type exclude disabled entities by default
    public struct DistrictGroupAssignment : IComponentData, IEnableableComponent, IQueryTypeParameter, ISerializable
    {
        public Entity m_Group;

        public DistrictGroupAssignment(Entity group)
        {
            m_Group = group;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Group);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Group);
        }
    }
}
