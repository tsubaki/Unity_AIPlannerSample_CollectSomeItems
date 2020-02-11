using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Baggage : ITrait, IEquatable<Baggage>
    {
        public const string FieldHasItem = "HasItem";
        public System.Boolean HasItem;

        public void SetField(string fieldName, object value)
        {
            switch (fieldName)
            {
                case nameof(HasItem):
                    HasItem = (System.Boolean)value;
                    break;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Baggage.");
            }
        }

        public object GetField(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(HasItem):
                    return HasItem;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Baggage.");
            }
        }

        public bool Equals(Baggage other)
        {
            return HasItem == other.HasItem;
        }

        public override string ToString()
        {
            return $"Baggage: {HasItem}";
        }
    }
}
