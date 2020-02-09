using Unity.AI.Planner;
using Unity.Collections;
using Unity.Entities;
using Unity.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Domains
{
    public struct GoalComplete
    {
        public bool IsTerminal(StateData stateData)
        {
            var NPCFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Npc>(),[1] = ComponentType.ReadWrite<Location>(),  };
            var GoalFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Goal>(),[1] = ComponentType.ReadWrite<Location>(),  };
            var NPCObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(NPCObjectIndices, NPCFilter);
            var GoalObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GoalObjectIndices, GoalFilter);
            var LocationBuffer = stateData.LocationBuffer;
            
            for (int i0 = 0; i0 < NPCObjectIndices.Length; i0++)
            {
                var NPCIndex = NPCObjectIndices[i0];
                var NPCObject = stateData.TraitBasedObjects[NPCIndex];
                
            
            for (int i1 = 0; i1 < GoalObjectIndices.Length; i1++)
            {
                var GoalIndex = GoalObjectIndices[i1];
                var GoalObject = stateData.TraitBasedObjects[GoalIndex];
                
                if (!(LocationBuffer[NPCObject.LocationIndex].Position == LocationBuffer[GoalObject.LocationIndex].Position))
                    continue;
                NPCFilter.Dispose();
                GoalFilter.Dispose();
                return true;
            }
            }
            NPCObjectIndices.Dispose();
            GoalObjectIndices.Dispose();
            NPCFilter.Dispose();
            GoalFilter.Dispose();

            return false;
        }

        public float TerminalReward(StateData stateData)
        {
            var reward = 100f;

            return reward;
        }
    }
}
