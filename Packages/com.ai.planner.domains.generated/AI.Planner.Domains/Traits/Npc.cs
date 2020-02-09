using System;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    [Serializable]
    public struct Npc : ITrait, IEquatable<Npc>
    {

        public void SetField(string fieldName, object value)
        {
        }

        public object GetField(string fieldName)
        {
            throw new ArgumentException("No fields exist on trait Npc.");
        }

        public bool Equals(Npc other)
        {
            return true;
        }

        public override string ToString()
        {
            return $"Npc";
        }
    }
}
