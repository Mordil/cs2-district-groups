using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace multi_district_tool
{
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

    // A named, typed set of base districts. One entity per group; members live
    // in a DistrictGroupMember buffer on the same entity.
    public struct DistrictGroupData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public GroupServiceType m_Type;
        public FixedString64Bytes m_Name;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((byte)1);
            writer.Write((byte)m_Type);
            writer.Write(m_Name.ToString());
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte _);
            reader.Read(out byte type);
            m_Type = (GroupServiceType)type;
            reader.Read(out string name);
            m_Name = name;
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
    // v1 is exclusive — while assigned, the mod owns the building's buffer content.
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
