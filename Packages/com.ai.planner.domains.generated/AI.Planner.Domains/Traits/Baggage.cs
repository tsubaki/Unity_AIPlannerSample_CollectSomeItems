using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Baggage : ITrait, IEquatable<Baggage>
    {
        public const string FieldItemCount = "ItemCount";
        public System.Int32 ItemCount;

        public void SetField(string fieldName, object value)
        {
            switch (fieldName)
            {
                case nameof(ItemCount):
                    ItemCount = (System.Int32)value;
                    break;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Baggage.");
            }
        }

        public object GetField(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(ItemCount):
                    return ItemCount;
                default:
                    throw new ArgumentException($"Field \"{fieldName}\" does not exist on trait Baggage.");
            }
        }

        public bool Equals(Baggage other)
        {
            return ItemCount == other.ItemCount;
        }

        public override string ToString()
        {
            return $"Baggage: {ItemCount}";
        }
    }
}
