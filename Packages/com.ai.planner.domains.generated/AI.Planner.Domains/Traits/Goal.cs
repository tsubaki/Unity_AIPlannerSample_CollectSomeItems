using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Goal : ITrait, IEquatable<Goal>
    {

        public void SetField(string fieldName, object value)
        {
        }

        public object GetField(string fieldName)
        {
            throw new ArgumentException("No fields exist on trait Goal.");
        }

        public bool Equals(Goal other)
        {
            return true;
        }

        public override string ToString()
        {
            return $"Goal";
        }
    }
}
