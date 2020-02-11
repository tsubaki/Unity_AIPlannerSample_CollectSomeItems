using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Goal : ITrait, IEquatable<Goal>
    {
        public const string FieldGateCount = "GateCount";
        public System.Int32 GateCount;

        public void SetField(string fieldName, object value)
        {
            switch (fieldName)
            {
                case nameof(GateCount):
                    GateCount = (System.Int32)value;
                    break;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Goal.");
            }
        }

        public object GetField(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(GateCount):
                    return GateCount;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Goal.");
            }
        }

        public bool Equals(Goal other)
        {
            return GateCount == other.GateCount;
        }

        public override string ToString()
        {
            return $"Goal: {GateCount}";
        }
    }
}
