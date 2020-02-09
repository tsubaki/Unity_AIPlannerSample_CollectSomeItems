using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Burst;
using AI.Planner.Domains;

namespace AI.Planner.Actions.MyPlan
{
    [BurstCompile]
    struct MoveToGoal : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_NPCIndex = 0;
        const int k_GoalIndex = 1;
        const int k_MaxArguments = 2;

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        internal MoveToGoal(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "NPC", StringComparison.OrdinalIgnoreCase))
                 return k_NPCIndex;
            if (string.Equals(parameterName, "Goal", StringComparison.OrdinalIgnoreCase))
                 return k_GoalIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var NPCFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Npc>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),  };
            var GoalFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Goal>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),  };
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
                
                if (!(LocationBuffer[NPCObject.LocationIndex].Position != LocationBuffer[GoalObject.LocationIndex].Position))
                    continue;

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_NPCIndex] = NPCIndex,
                                                       [k_GoalIndex] = GoalIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            NPCObjectIndices.Dispose();
            GoalObjectIndices.Dispose();
            NPCFilter.Dispose();
            GoalFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalNPCObject = originalStateObjectBuffer[action[k_NPCIndex]];
            var originalGoalObject = originalStateObjectBuffer[action[k_GoalIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newLocationBuffer = newState.LocationBuffer;
            {
                    var @Location = newLocationBuffer[originalNPCObject.LocationIndex];
                    @Location.Position = newLocationBuffer[originalGoalObject.LocationIndex].Position;
                    newLocationBuffer[originalNPCObject.LocationIndex] = @Location;
            }

            

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = 0f;

            return reward;
        }

        public void Execute(int jobIndex)
        {
            m_StateDataContext.JobIndex = jobIndex; //todo check that all actions set the job index

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            var argumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            GenerateArgumentPermutations(stateData, argumentPermutations);

            var transitionInfo = new NativeArray<MoveToGoalFixupReference>(argumentPermutations.Length, Allocator.Temp);
            for (var i = 0; i < argumentPermutations.Length; i++)
            {
                transitionInfo[i] = new MoveToGoalFixupReference { TransitionInfo = ApplyEffects(argumentPermutations[i], stateEntityKey) };
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<MoveToGoalFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(transitionInfo);

            transitionInfo.Dispose();
            argumentPermutations.Dispose();
        }

        
        public static T GetNPCTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_NPCIndex]);
        }
        
        public static T GetGoalTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_GoalIndex]);
        }
        
    }

    public struct MoveToGoalFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


