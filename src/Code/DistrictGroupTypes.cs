using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using System;
using Unity.Collections;
using Unity.Entities;
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
        Welfare = 11,
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
        // Resident households, excluding the tourists and commuters vanilla leaves out of its wealth average.
        public int m_HouseholdCount;

        // Folds another district's totals into these.
        public void Add(DistrictStats other)
        {
            m_Population += other.m_Population;
            m_HappinessSum += other.m_HappinessSum;
            m_LivingResidentCount += other.m_LivingResidentCount;
            m_WealthSum += other.m_WealthSum;
            m_HouseholdCount += other.m_HouseholdCount;
        }
    }

    // A named, typed set of base districts.
    public struct DistrictGroupData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public GroupServiceType m_Type;
        public FixedString64Bytes m_Name;
        public Color m_Color;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((byte)3);
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
                Versions before 3 stored GroupServiceType.Parks at byte 11 and GroupServiceType.Welfare
                at byte 12; Parks was removed and Welfare renumbered down to 11, so a save from an older
                version needs its raw byte remapped to keep reading the type it already had.
            */
            if (version < 3)
            {
                if (type == 11)
                {
                    type = (byte)GroupServiceType.Generic;
                }
                else if (type == 12)
                {
                    type = (byte)GroupServiceType.Welfare;
                }
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
