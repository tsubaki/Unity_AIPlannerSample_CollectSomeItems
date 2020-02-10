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
    struct MoveToSwitch : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_NPCIndex = 0;
        const int k_GateIndex = 1;
        const int k_MaxArguments = 2;

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        internal MoveToSwitch(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "NPC", StringComparison.OrdinalIgnoreCase))
                 return k_NPCIndex;
            if (string.Equals(parameterName, "Gate", StringComparison.OrdinalIgnoreCase))
                 return k_GateIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var NPCFilter = new NativeArray<ComponentType>(3, Allocator.Temp){[0] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[1] = ComponentType.ReadWrite<AI.Planner.Domains.Npc>(),[2] = ComponentType.ReadWrite<AI.Planner.Domains.Baggage>(),  };
            var GateFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[1] = ComponentType.ReadWrite<AI.Planner.Domains.Gate>(),  };
            var NPCObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(NPCObjectIndices, NPCFilter);
            var GateObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GateObjectIndices, GateFilter);
            var LocationBuffer = stateData.LocationBuffer;
            
            for (int i0 = 0; i0 < NPCObjectIndices.Length; i0++)
            {
                var NPCIndex = NPCObjectIndices[i0];
                var NPCObject = stateData.TraitBasedObjects[NPCIndex];
                
            
            for (int i1 = 0; i1 < GateObjectIndices.Length; i1++)
            {
                var GateIndex = GateObjectIndices[i1];
                var GateObject = stateData.TraitBasedObjects[GateIndex];
                
                if (!(LocationBuffer[GateObject.LocationIndex].Position != LocationBuffer[NPCObject.LocationIndex].Position))
                    continue;

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_NPCIndex] = NPCIndex,
                                                       [k_GateIndex] = GateIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            NPCObjectIndices.Dispose();
            GateObjectIndices.Dispose();
            NPCFilter.Dispose();
            GateFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalNPCObject = originalStateObjectBuffer[action[k_NPCIndex]];
            var originalGateObject = originalStateObjectBuffer[action[k_GateIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newLocationBuffer = newState.LocationBuffer;
            {
                    var @Location = newLocationBuffer[originalNPCObject.LocationIndex];
                    @Location.Position = newLocationBuffer[originalGateObject.LocationIndex].Position;
                    newLocationBuffer[originalNPCObject.LocationIndex] = @Location;
            }

            

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = -0.2f;
            {
                var param0 = originalState.GetTraitOnObjectAtIndex<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(action[0]);
                var param1 = originalState.GetTraitOnObjectAtIndex<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(action[1]);
                reward -= new Unity.AI.Planner.Navigation.LocationDistance().RewardModifier( param0, param1);
            }

            return reward;
        }

        public void Execute(int jobIndex)
        {
            m_StateDataContext.JobIndex = jobIndex; //todo check that all actions set the job index

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            var argumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            GenerateArgumentPermutations(stateData, argumentPermutations);

            var transitionInfo = new NativeArray<MoveToSwitchFixupReference>(argumentPermutations.Length, Allocator.Temp);
            for (var i = 0; i < argumentPermutations.Length; i++)
            {
                transitionInfo[i] = new MoveToSwitchFixupReference { TransitionInfo = ApplyEffects(argumentPermutations[i], stateEntityKey) };
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<MoveToSwitchFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(transitionInfo);

            transitionInfo.Dispose();
            argumentPermutations.Dispose();
        }

        
        public static T GetNPCTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_NPCIndex]);
        }
        
        public static T GetGateTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_GateIndex]);
        }
        
    }

    public struct MoveToSwitchFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


