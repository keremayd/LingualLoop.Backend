using System.Runtime.Serialization;

namespace Common;

[DataContract]
public enum PriorityType
{
    [EnumMember] Emergency,
    [EnumMember] Alert,
    [EnumMember] Critical,
    [EnumMember] Error,
    [EnumMember] Warning,
    [EnumMember] Notice,
    [EnumMember] Informational,
    [EnumMember] Debug
};