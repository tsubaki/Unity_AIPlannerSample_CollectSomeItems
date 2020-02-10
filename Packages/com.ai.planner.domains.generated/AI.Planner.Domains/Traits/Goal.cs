using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Goal : ITrait, IEquatable<Goal>
    {
        public const string FieldIsDone = "IsDone";
        public System.Boolean IsDone;

        public void SetField(string fieldName, object value)
        {
            switch (fieldName)
            {
                case nameof(IsDone):
                    IsDone = (System.Boolean)value;
                    break;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Goal.");
            }
        }

        public object GetField(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(IsDone):
                    return IsDone;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Goal.");
            }
        }

        public bool Equals(Goal other)
        {
            return IsDone == other.IsDone;
        }

        public override string ToString()
        {
            return $"Goal: {IsDone}";
        }
    }
}
