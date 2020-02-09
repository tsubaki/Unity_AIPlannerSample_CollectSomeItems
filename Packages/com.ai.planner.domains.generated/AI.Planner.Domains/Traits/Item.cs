using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Item : ITrait, IEquatable<Item>
    {

        public void SetField(string fieldName, object value)
        {
        }

        public object GetField(string fieldName)
        {
            throw new ArgumentException("No fields exist on trait Item.");
        }

        public bool Equals(Item other)
        {
            return true;
        }

        public override string ToString()
        {
            return $"Item";
        }
    }
}
