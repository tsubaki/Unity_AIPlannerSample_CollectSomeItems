using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct WayPoint : ITrait, IEquatable<WayPoint>
    {

        public void SetField(string fieldName, object value)
        {
        }

        public object GetField(string fieldName)
        {
            throw new ArgumentException("No fields exist on trait WayPoint.");
        }

        public bool Equals(WayPoint other)
        {
            return true;
        }

        public override string ToString()
        {
            return $"WayPoint";
        }
    }
}
