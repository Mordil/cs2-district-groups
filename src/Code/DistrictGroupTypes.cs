using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
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
        Parks = 11,
        Welfare = 12,
    }

    // A named, typed set of base districts.
    public struct DistrictGroupData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public GroupServiceType m_Type;
        public FixedString64Bytes m_Name;
        public Color m_Color;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((byte)2);
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
    public struct DistrictGroupAssignment : IComponentData, IQueryTypeParameter, ISerializable
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
