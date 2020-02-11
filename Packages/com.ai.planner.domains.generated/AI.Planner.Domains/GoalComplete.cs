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
            var GameStateFilter = new NativeArray<ComponentType>(1, Allocator.Temp){[0] = ComponentType.ReadWrite<GateSwitch>(),  };
            var NPCObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(NPCObjectIndices, NPCFilter);
            var GoalObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GoalObjectIndices, GoalFilter);
            var GameStateObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GameStateObjectIndices, GameStateFilter);
            var LocationBuffer = stateData.LocationBuffer;
            var GateSwitchBuffer = stateData.GateSwitchBuffer;
            
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
                
            
            for (int i2 = 0; i2 < GameStateObjectIndices.Length; i2++)
            {
                var GameStateIndex = GameStateObjectIndices[i2];
                var GameStateObject = stateData.TraitBasedObjects[GameStateIndex];
                
                
                if (!(GateSwitchBuffer[GameStateObject.GateSwitchIndex].OpenCount == 2))
                    continue;
                NPCFilter.Dispose();
                GoalFilter.Dispose();
                GameStateFilter.Dispose();
                return true;
            }
            }
            }
            NPCObjectIndices.Dispose();
            GoalObjectIndices.Dispose();
            GameStateObjectIndices.Dispose();
            NPCFilter.Dispose();
            GoalFilter.Dispose();
            GameStateFilter.Dispose();

            return false;
        }

        public float TerminalReward(StateData stateData)
        {
            var reward = 100f;

            return reward;
        }
    }
}
