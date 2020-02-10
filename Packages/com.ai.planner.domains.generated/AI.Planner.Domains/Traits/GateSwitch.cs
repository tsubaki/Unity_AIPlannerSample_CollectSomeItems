using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct GateSwitch : ITrait, IEquatable<GateSwitch>
    {
        public const string FieldOpenCount = "OpenCount";
        public System.Int32 OpenCount;

        public void SetField(string fieldName, object value)
        {
            switch (fieldName)
            {
                case nameof(OpenCount):
                    OpenCount = (System.Int32)value;
                    break;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait GateSwitch.");
            }
        }

        public object GetField(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(OpenCount):
                    return OpenCount;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait GateSwitch.");
            }
        }

        public bool Equals(GateSwitch other)
        {
            return OpenCount == other.OpenCount;
        }

        public override string ToString()
        {
            return $"GateSwitch: {OpenCount}";
        }
    }
}
